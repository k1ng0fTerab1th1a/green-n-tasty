using Amazon.S3;
using Amazon.S3.Model;
using FluentResults;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Infrastructure.Services;

public class S3FileService(IAmazonS3 _s3) : IFileService
{
    private const string BucketName = "run20-tm2-frontend-bucket";

    public async Task<Stream> GetFileStreamAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(
            new GetObjectRequest { BucketName = BucketName, Key = key }, ct);

        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<Result> UploadFileAsync(string key, Stream stream, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3.PutObjectAsync(request);
            return Result.Ok();
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception("Failed to upload to file system");
        }
    }

    public string GetFileUrl(string key)
    {
        return $"https://{BucketName}.s3.amazonaws.com/{key}";
    }
}
