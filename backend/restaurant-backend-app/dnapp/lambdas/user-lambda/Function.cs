using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;

namespace UserLambda;

public class Function : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<UserLambda.Api.Startup>();
    }

    protected override void Init(IHostBuilder builder)
    {
    }
}