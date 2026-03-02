using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Services;
using Restaurant.Infrastructure.Repositories;
using Restaurant.Infrastructure.Services;
using Restaurant.Infrastructure.Utils;

namespace Restaurant.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
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
                        ValidateAudience = false
                    };
                });

            services.AddExceptionHandler<Middlewares.GlobalExceptionHandler>();
            services.AddProblemDetails();

            // --- ВАШІ РЕЄСТРАЦІЇ DEPENDENCY INJECTION ---
            services.AddDynamoDb();
            services.AddScoped<ICognitoService, CognitoService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IRawMappingService, RawMappingService>();
            services.AddScoped<DynamoDbSeeder>();

            services.AddAuthorization();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DynamoDbSeeder seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                //seeder.SeedAsync().Wait();
            }

            app.UseExceptionHandler();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}