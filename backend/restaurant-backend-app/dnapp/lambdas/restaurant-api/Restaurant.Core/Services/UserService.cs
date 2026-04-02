using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
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
    private readonly IEmailService _emailService;

    private const long MaxAvatarSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UserService(ICognitoService cognitoService, IUserRepository userRepository,
        IFileService fileService, IEmailService emailService)
    {
        _cognitoService = cognitoService;
        _userRepository = userRepository;
        _fileService = fileService;
        _emailService = emailService;
    }

    public async Task<Result> UpdateEmailAsync(string newEmail, string accessToken, CancellationToken ct = default)
    {
        var cognitoResult = await _cognitoService.UpdateUserEmailAsync(accessToken, newEmail, ct);
        if (cognitoResult.IsFailed)
            return cognitoResult;

        return Result.Ok();
    }

    public async Task<Result> VerifyEmailChangeAsync(string userId, string newEmail, string accessToken, string code, CancellationToken ct = default)
    {
        var cognitoResult = await _cognitoService.VerifyEmailChangeAsync(accessToken, code, ct);
        if (cognitoResult.IsFailed)
            return cognitoResult;

        await _userRepository.UpdateEmailAsync(userId, newEmail, ct);

        return Result.Ok();
    }

    public async Task<Result> UpdateUserNameAsync(string userId, string firstName, string lastName, CancellationToken
            ct)
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

    public async Task<Result> CreateOtpAsync(string email, CancellationToken ct)
    {
        var normalizedEmailResult = ValidateAndNormalizeEmail(email);
        if (normalizedEmailResult.IsFailed)
            return Result.Fail(normalizedEmailResult.Errors);

        var normalizedEmail = normalizedEmailResult.Value;

        if (!await _userRepository.IfUserExistsByEmail(normalizedEmail, ct))
            return UserErrors.UserNotFound;

        var otp = GenerateOtp(normalizedEmail);
        await _userRepository.CreateOtp(otp, ct);

        await _emailService.SendEmail($"Your One-Time-Password is: {otp.Otp}. It's duration is 10 minutes",
            "Password Recovery", normalizedEmail, ct);

        return Result.Ok();
    }

    public async Task<Result> VerifyOtp(string email, string otp, CancellationToken ct)
    {
        var normalizedEmailResult = ValidateAndNormalizeEmail(email);
        if (normalizedEmailResult.IsFailed)
            return Result.Fail(normalizedEmailResult.Errors);

        var normalizedEmail = normalizedEmailResult.Value;

        if (!await _userRepository.IfUserExistsByEmail(normalizedEmail, ct))
            return UserErrors.UserNotFound;

        var otpObj = await _userRepository.GetOtpByEmailAsync(normalizedEmail, ct);

        if (otpObj == null)
            return UserErrors.OtpNotFound;

        if (otpObj.Used)
            return UserErrors.OtpAlreadyUsed;

        if (IsOtpExpired(otpObj.ExpiresAt))
            return UserErrors.OtpExpired;

        if (otp == otpObj.Otp)
            return Result.Ok();

        return UserErrors.OtpIsWrong;
    }

    public async Task<Result> RecoverPassword(string email, string otp, string password, CancellationToken ct)
    {
        var normalizedEmailResult = ValidateAndNormalizeEmail(email);
        if (normalizedEmailResult.IsFailed)
            return Result.Fail(normalizedEmailResult.Errors);

        var normalizedEmail = normalizedEmailResult.Value;

        var res = await VerifyOtp(normalizedEmail, otp, ct);
        if (res.IsFailed)
            return res;

        res = await _cognitoService.UpdatePasswordAsync(normalizedEmail, password, ct);
        if (res.IsFailed)
            return res;

        var otpObj = await _userRepository.GetOtpByEmailAsync(normalizedEmail, ct);
        if (otpObj is null)
            return UserErrors.OtpNotFound;

        otpObj.Used = true;
        await _userRepository.CreateOtp(otpObj, ct);

        return Result.Ok();
    }


    private static UserOtp GenerateOtp(string email)
    {
        var now = DateTime.UtcNow;

        return new UserOtp
        {
            Email = email,
            Used = false,
            Otp = GenerateSecureOtp(6),
            ExpiresAt = new DateTimeOffset(now.AddMinutes(10)).ToUnixTimeSeconds(),
            CreatedAt = now.ToString("o") // ISO 8601 format
        };
    }

    private static Result<string> ValidateAndNormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail<string>(UserErrors.EmptyEmail);

        var trimmedEmail = email.Trim();

        try
        {
            _ = new MailAddress(trimmedEmail);
        }
        catch (FormatException)
        {
            return Result.Fail<string>(UserErrors.InvalidEmail);
        }

        return Result.Ok(trimmedEmail.ToLowerInvariant());
    }
    
    private static string GenerateSecureOtp(int length)
    {
        char[] alphanumeric = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        var result = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();
        var uintBuffer = new byte[4];

        while (result.Length < length)
        {
            rng.GetBytes(uintBuffer);
            var value = BitConverter.ToUInt32(uintBuffer, 0);
            
            result.Append(alphanumeric[value % alphanumeric.Length]);
        }

        return result.ToString();
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
    
    private static bool IsOtpExpired(long expiresAt)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
        return now >= expiresAt;
    }
}
