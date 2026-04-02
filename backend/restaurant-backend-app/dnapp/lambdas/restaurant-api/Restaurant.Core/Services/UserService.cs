using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class UserService : IUserService
{
    private readonly ICognitoService _cognitoService;
    private readonly IUserRepository _userRepository;
    private readonly IFileService _fileService;

    private const long MaxAvatarSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UserService(ICognitoService cognitoService, IUserRepository userRepository, IFileService fileService)
    {
        _cognitoService = cognitoService;
        _userRepository = userRepository;
        _fileService = fileService;
    }

    public async Task<Result> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        var cognitoResult = await _cognitoService.UpdateUserEmailAsync(userId, newEmail, ct);
        if (cognitoResult.IsFailed)
            return cognitoResult;

        await _userRepository.UpdateEmailAsync(userId, newEmail, ct);

        return Result.Ok();
    }

    public async Task<Result> UpdateUserNameAsync(
        string userId,
        string firstName,
        string lastName,
        CancellationToken ct)
    {
        try
        {
            await _userRepository.UpdateUserNameAsync(userId, firstName, lastName, ct);
            return Result.Ok();
        }
        catch (Exception)
        {
            return UserErrors.UpdateNotSuccessful;
        }
    }

    public async Task<Result> ChangePasswordAsync(
        string accessToken,
        string currentPassword,
        string newPassword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Result.Fail(AuthErrors.AccessTokenRequired);

        var result = await _cognitoService.ChangePasswordAsync(
            accessToken,
            currentPassword,
            newPassword,
            ct);

        if (result.IsFailed)
            return result;

        return Result.Ok();
    }

    public async Task<Result<string>> UpdateAvatarAsync(string userId, FileUploadDto file, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return UserErrors.UserNotFound;

        var key = $"uploads/avatars/{userId}/profile";

        if (!AllowedAvatarContentTypes.Contains(file.ContentType))
        {
            return FileErrors.InvalidFileType;
        }

        Stream content = file.Content;
        MemoryStream? bufferedContent = null;

        if (!content.CanSeek)
        {
            bufferedContent = new MemoryStream();
            await content.CopyToAsync(bufferedContent, ct);
            bufferedContent.Position = 0;
            content = bufferedContent;
        }

        try
        {
            if (!IsImage(content))
            {
                return FileErrors.InvalidFileType;
            }

            if (file.Size > MaxAvatarSize)
            {
                return FileErrors.FileTooBig;
            }

            var uploadResult = await _fileService.UploadFileAsync(key, content, file.ContentType);
            if (uploadResult.IsFailed)
                return uploadResult;

            var url = _fileService.GetFileUrl(key);

            await _userRepository.UpdateAvatarUrlAsync(userId, url, ct);

            return url;
        }
        finally
        {
            bufferedContent?.Dispose();
        }
    }

    public async Task<Result<User>> GetMeAsync(string userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return UserErrors.UserNotFound;

        return user;
    }

    private static bool IsImage(Stream stream)
    {
        if (!stream.CanRead)
            return false;

        var header = new byte[12];
        var totalRead = 0;

        while (totalRead < header.Length)
        {
            var read = stream.Read(header, totalRead, header.Length - totalRead);
            if (read == 0)
                break;

            totalRead += read;
        }

        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        if (totalRead < 4)
            return false;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return true;

        // WEBP: RIFF....WEBP
        if (totalRead >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            return true;

        return false;
    }
}
