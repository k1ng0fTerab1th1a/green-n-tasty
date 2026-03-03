<<<<<<< backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs

=======
using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
>>>>>>> backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Services;
using Restaurant.Infrastructure.Repositories;
<<<<<<< backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs

using Restaurant.Infrastructure.Utils;
=======
using Restaurant.Infrastructure.Repository;
using Restaurant.Infrastructure.Services;
using System;
>>>>>>> backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs

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
<<<<<<< backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IRawMappingService, RawMappingService>();
=======
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWaiterListRepository, WaiterListRepository>();
>>>>>>> backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Api/Startup.cs

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
}