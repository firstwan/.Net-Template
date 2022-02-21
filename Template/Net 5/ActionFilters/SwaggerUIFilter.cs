using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Collections.Generic;

namespace GDBAPI.ActionFilters
{
    public class SwaggerUIFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;
            operation.Deprecated |= apiDescription.IsDeprecated();

            var allAttributed = context.ApiDescription.CustomAttributes();
            var noAuthRequired = !allAttributed.Any(attr => attr.GetType() == typeof(AuthorizeAttribute)) || allAttributed.Any(attr => attr.GetType() == typeof(AllowAnonymousAttribute));


            if (!noAuthRequired)
            {
                var authorizeAttribute = allAttributed.FirstOrDefault(x => x.GetType() == typeof(AuthorizeAttribute)) as AuthorizeAttribute;

                // Add Authorization header into each endpoint, 
                // we must define the security scheme with AddSecurityDefinition before we able to choose which scheme we want to use on AddSecurityRequirement
                var jwtBearerSecurityRequirement = new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "jwt_bearer"
                    }
                };

                operation.Security.Add(new OpenApiSecurityRequirement()
                {
                    {
                        jwtBearerSecurityRequirement, new List<string>()
                    }
                });
            }
        }
    }
}
