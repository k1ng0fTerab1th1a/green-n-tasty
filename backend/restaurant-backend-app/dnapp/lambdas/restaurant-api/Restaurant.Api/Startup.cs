using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Services;
using Restaurant.Infrastructure.Repositories;
using Restaurant.Infrastructure.Services;
using System;

namespace Restaurant.Api
{
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
                        ValidateAudience = false
                    };
                });

            services.AddExceptionHandler<Middlewares.GlobalExceptionHandler>();
            services.AddProblemDetails();

            // --- РЕЄСТРАЦІЇ DEPENDENCY INJECTION ---
            services.AddDynamoDb();
            services.AddSingleton<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();

            services.AddScoped<ICognitoService, CognitoService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddAuthorization();
            services.AddControllers();

            if (_env.IsDevelopment())
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen();
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
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}