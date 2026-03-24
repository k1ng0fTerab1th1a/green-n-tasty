using Amazon.S3;
using Amazon.S3.Model;

namespace Restaurant.Infrastructure.Services;

public class S3FileService(IAmazonS3 _s3)
{
    private const string BucketName = "run20-team2-project-education-artifacts-dev";

    public async Task<Stream> GetFileStreamAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(
            new GetObjectRequest { BucketName = BucketName, Key = key }, ct);

        return response.ResponseStream;
    }
}
