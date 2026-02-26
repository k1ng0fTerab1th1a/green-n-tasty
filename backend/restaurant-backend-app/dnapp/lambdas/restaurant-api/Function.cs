using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RestaurantLambdaFunction;

public class Function : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Restaurant.Api.Startup>();
    }

    protected override void Init(IHostBuilder builder)
    {
    }
}
