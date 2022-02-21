using FluentValidation.AspNetCore;
using GDBAPI.ActionFilters;
using GDBAPI.Domain.Constants;
using GDBAPI.Domain.Contexts;
using GDBAPI.DtoModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDBAPI.Extensions
{
    public static class ServicesExtension
    {
        public static IServiceCollection AddCustomMVC(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // Register global action filter
                options.Filters.Add(typeof(ValidationActionFilter));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Disable the default automatic 400 BadRequest Response
                // Microsoft Doc: https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-5.0#automatic-http-400-responses
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(options =>
            {
                // Hide the null value data param when response to client
                options.JsonSerializerOptions.IgnoreNullValues = true;

                // Configure to show enum as string
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            })
            .AddFluentValidation(options =>
            {
                // Fluent Validation brief example: https://www.carlrippon.com/fluentvalidation-in-an-asp-net-core-web-api/
                options.RegisterValidatorsFromAssemblyContaining<Startup>();
                // Indicate only run Fluent validation
                options.DisableDataAnnotationsValidation = true;
            });

            return services;
        }

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            // API versioning
            services.AddApiVersioning(options =>
            {
                // Reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });
            services.AddVersionedApiExplorer(options =>
            {
                // VersionedApiExplorer are for discovering and exposing metadata of your application
                // it won't affect how client communication with this application
                // https://github.com/microsoft/aspnet-api-versioning/wiki/Version-Format#custom-api-version-format-strings
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";
                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Authentication
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // To retrive the claim information
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        // The default ClockSkew time is 5 min, this mean JWT will add extra 5 min on the token expiry time when checking the expiry date
                        // set it with TimeSpan.Zero to remove the extra 5 min
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]))
                    };


                    // Custom response for Failed authentication
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = MediaTypeNames.Application.Json;
                            await context.Response.WriteAsync(new ErrorResponseDto<string>()
                            {
                                Code = ErrorCodeConstants.UNAUTHORIZED_ERROR,
                                Message = "Unauthorized",
                            }.ToString());
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddCustomAutoMapper(this IServiceCollection services)
        {
            // Auto Mapper
            services.AddAutoMapper(typeof(Startup));

            return services;
        }

        public static IServiceCollection AddCustomDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Entity Framework configuration
            services.AddDbContext<AppDBContext>(options =>
            {
                var connectionString = configuration["Database:ConnectionStringSecretName"];

                options.UseMySql(connectionString,
                    ServerVersion.AutoDetect(connectionString)
                    );
            });

            return services;
        }

        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            // Generate Swagger Json Document
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            // Configure Swagger to support Newtonsoft Json
            //services.AddSwaggerGenNewtonsoftSupport();

            services.AddSwaggerGen(options =>
            {
                // Add a custom operation filter on Swagger for better document overview
                options.OperationFilter<SwaggerUIFilter>();

                // IncludeXmlComments enable feature of showing summary on Swagger
                var xmlFileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                options.IncludeXmlComments(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), xmlFileName));

                // Define what kind of authentication method your API is using (can have one or more)
                options.AddSecurityDefinition("jwt_bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                    Description = "Please enter into field the word 'Bearer' following by space and JWT",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header
                });
            });

            return services;
        }
    }
}
