using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface IFileService
{
    Task<Stream> GetFileStreamAsync(string key, CancellationToken ct = default);
    Task<Result> UploadFileAsync(string key, Stream stream, string contentType);
    string GetFileUrl(string key);
}
