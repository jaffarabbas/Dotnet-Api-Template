using ApiTemplate.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ApiTemplate.Swagger
{
    /// <summary>
    /// Adds X-Resource-Id header parameter to Swagger operations that require permission checking
    /// </summary>
    public class ResourceIdOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the endpoint has SkipJwtValidation or SkipPermissionCheck attribute
            var hasSkipJwt = context.MethodInfo.GetCustomAttributes(true)
                .Any(x => x.GetType() == typeof(SkipJwtValidationAttribute)) ||
                context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Any(x => x.GetType() == typeof(SkipJwtValidationAttribute)) == true;

            var hasSkipPermission = context.MethodInfo.GetCustomAttributes(true)
                .Any(x => x.GetType() == typeof(SkipPermissionCheckAttribute)) ||
                context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Any(x => x.GetType() == typeof(SkipPermissionCheckAttribute)) == true;

            // If endpoint has skip attributes, don't add the header parameter
            if (hasSkipJwt || hasSkipPermission)
            {
                return;
            }

            // Add permission-related headers as parameters
            operation.Parameters ??= new List<OpenApiParameter>();

            // Add X-Resource-Id header
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Resource-Id",
                In = ParameterLocation.Header,
                Required = true,
                Description = "The ID of the resource being accessed (required for permission checking)",
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                }
            });

            // Add X-Action-Type-Id header (option 1 - preferred)
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Action-Type-Id",
                In = ParameterLocation.Header,
                Required = false,
                Description = "The action type ID (e.g., 1 for Read, 2 for Create, etc.). Use this OR X-Action-Type.",
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                }
            });

            // Add X-Action-Type header (option 2 - alternative)
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Action-Type",
                In = ParameterLocation.Header,
                Required = false,
                Description = "The action type name (e.g., 'Read', 'Create', 'Update', 'Delete'). Use this OR X-Action-Type-Id.",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}
