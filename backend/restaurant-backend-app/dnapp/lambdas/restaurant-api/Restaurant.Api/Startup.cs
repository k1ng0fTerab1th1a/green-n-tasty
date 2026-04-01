using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Services;
using Restaurant.Core.SharedModels;
using Restaurant.Infrastructure.Repositories;
using Restaurant.Infrastructure.Services;
using Restaurant.Reports.Application;
using Restaurant.Reports.Infrastructure;

namespace Restaurant.Api;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var region = Environment.GetEnvironmentVariable("COGNITO_REGION") ?? _configuration["Cognito:Region"];
        var userPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? _configuration["Cognito:UserPoolId"];
        var cognitoIssuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = cognitoIssuer;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = cognitoIssuer,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    RoleClaimType = "custom:role"
                };
            });

        services.AddExceptionHandler<Middlewares.GlobalExceptionHandler>();
        services.AddProblemDetails();

        // --- РЕЄСТРАЦІЇ DEPENDENCY INJECTION ---
        services.AddDynamoDb();
        services.AddSingleton<IAmazonCognitoIdentityProvider>(sp =>
        {
            var config = new AmazonCognitoIdentityProviderConfig
            {
                MaxErrorRetry = 0
            };

            return new AmazonCognitoIdentityProviderClient(config);
        });
        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
        services.AddScoped<IEventPublisher, SqsEventPublisher>();

        services.AddScoped<ICognitoService, CognitoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWaiterListRepository, WaiterListRepository>();
        services.AddScoped<IDishRepository, DishRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<ITableDayRepository, TableDayRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IWaiterScheduleRepository, WaiterScheduleRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReportsRepository, ReportsRepository>();

        services.Configure<ClientSettings>(
            _configuration.GetSection("ClientSettings"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var bucketRegion = RegionEndpoint.GetBySystemName(
                Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2"
            );

            return new AmazonS3Client(bucketRegion);
        });

        services.AddScoped<IFileService, S3FileService>();
        
        services.AddAuthorization();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        services.AddControllers();

        if (_env.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        if (_env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseRouting();

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}