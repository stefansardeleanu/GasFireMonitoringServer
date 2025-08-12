// File: Infrastructure/SwaggerDefaultValues.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GasFireMonitoringServer.Infrastructure
{
    /// <summary>
    /// Swagger operation filter to add default values and examples
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add parameter examples
            AddParameterExamples(operation, context);

            // Set default response descriptions
            SetDefaultResponseDescriptions(operation);
        }

        private static void AddParameterExamples(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];

            // Add examples for common parameters
            foreach (var parameter in operation.Parameters)
            {
                switch (parameter.Name.ToLower())
                {
                    case "siteid":
                        parameter.Description = "Site identifier (1-10, 12)";
                        break;
                    case "id":
                        if (controllerName?.ToLower() == "sensor")
                        {
                            parameter.Description = "Sensor unique identifier";
                        }
                        break;
                    case "county":
                        parameter.Description = "County name (Prahova or Gorj)";
                        break;
                }
            }
        }

        private static void SetDefaultResponseDescriptions(OpenApiOperation operation)
        {
            // Set default descriptions if not provided
            foreach (var response in operation.Responses)
            {
                if (string.IsNullOrEmpty(response.Value.Description))
                {
                    response.Value.Description = response.Key switch
                    {
                        "200" => "Success - Operation completed successfully",
                        "201" => "Created - Resource created successfully",
                        "204" => "No Content - Operation completed, no data to return",
                        "400" => "Bad Request - Invalid input or validation errors",
                        "401" => "Unauthorized - Authentication required",
                        "403" => "Forbidden - Access denied",
                        "404" => "Not Found - Resource not found",
                        "409" => "Conflict - Resource already exists or conflict",
                        "500" => "Internal Server Error - Server error occurred",
                        _ => "Response"
                    };
                }
            }
        }
    }
}