using Amazon.S3;
using Amazon.S3.Model;

namespace Restaurant.Core.Services;

public class S3FileService(IAmazonS3 _s3)
{
    private const string BucketName = "run20-team2-project-education-artifacts-dev";
    
    public async Task<string> GetFileContentAsync(string key, CancellationToken ct = default)
    {
        // key example: "restaurant-menu/menu.pdf"
        var request = new GetObjectRequest
        {
            BucketName = BucketName,
            Key        = key
        };

        using var response = await _s3.GetObjectAsync(request, ct);
        using var reader   = new StreamReader(response.ResponseStream);

        return await reader.ReadToEndAsync(ct);
    }
    
    public async Task<Stream> GetFileStreamAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(
            new GetObjectRequest { BucketName = BucketName, Key = key }, ct);

        return response.ResponseStream;
    }

}