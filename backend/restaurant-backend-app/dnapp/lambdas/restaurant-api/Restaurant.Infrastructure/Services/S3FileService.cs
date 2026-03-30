using Amazon.S3;
using Amazon.S3.Model;

namespace Restaurant.Infrastructure.Services;

public class S3FileService(IAmazonS3 _s3)
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
}
