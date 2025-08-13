// File: Filters/SwaggerFilters.cs
// Custom Swagger filters for enhanced API documentation

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace GasFireMonitoringServer.Filters
{
    /// <summary>
    /// Provides default values for Swagger operations
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // Set operation ID for better client generation
            if (string.IsNullOrWhiteSpace(operation.OperationId))
            {
                operation.OperationId = apiDescription.TryGetMethodInfo(out var methodInfo)
                    ? methodInfo.Name
                    : null;
            }

            // Add deprecation information
            operation.Deprecated |= apiDescription.IsDeprecated();

            // Improve parameter descriptions
            if (operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters)
                {
                    var description = apiDescription.ParameterDescriptions
                        .First(p => p.Name == parameter.Name);

                    parameter.Description ??= description.ModelMetadata?.Description;

                    if (parameter.Schema.Default == null &&
                        description.DefaultValue != null)
                    {
                        parameter.Schema.Default = new OpenApiString(
                            description.DefaultValue.ToString());
                    }

                    parameter.Required |= description.IsRequired;
                }
            }

            // Ensure all responses have descriptions
            foreach (var response in operation.Responses)
            {
                if (string.IsNullOrWhiteSpace(response.Value.Description))
                {
                    response.Value.Description = response.Key switch
                    {
                        "200" => "Success - Request completed successfully",
                        "201" => "Created - Resource created successfully",
                        "204" => "No Content - Request completed with no content to return",
                        "400" => "Bad Request - Invalid input parameters",
                        "401" => "Unauthorized - Authentication required or failed",
                        "403" => "Forbidden - Insufficient permissions",
                        "404" => "Not Found - Resource not found",
                        "409" => "Conflict - Resource conflict occurred",
                        "500" => "Internal Server Error - Unexpected error occurred",
                        _ => "Response"
                    };
                }
            }
        }
    }

    /// <summary>
    /// Adds response examples to Swagger documentation
    /// </summary>
    public class SwaggerResponseExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add common response examples
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];

                // Add media type if not present
                if (!response.Content.ContainsKey("application/json"))
                {
                    response.Content.Add("application/json", new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object"
                        }
                    });
                }

                var mediaType = response.Content["application/json"];

                // Add example based on operation
                if (context.MethodInfo.Name.Contains("GetAll") ||
                    context.MethodInfo.Name.Contains("List"))
                {
                    mediaType.Examples = new Dictionary<string, OpenApiExample>
                    {
                        ["Success"] = new OpenApiExample
                        {
                            Summary = "Successful response with multiple items",
                            Value = new OpenApiObject
                            {
                                ["success"] = new OpenApiBoolean(true),
                                ["message"] = new OpenApiString("Data retrieved successfully"),
                                ["data"] = new OpenApiArray
                                {
                                    new OpenApiObject
                                    {
                                        ["id"] = new OpenApiInteger(1),
                                        ["name"] = new OpenApiString("Example Item")
                                    }
                                },
                                ["count"] = new OpenApiInteger(1),
                                ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O"))
                            }
                        }
                    };
                }
            }

            // Add error response examples
            if (operation.Responses.ContainsKey("400"))
            {
                AddErrorExample(operation.Responses["400"], "Validation failed",
                    "Invalid input parameters provided");
            }

            if (operation.Responses.ContainsKey("401"))
            {
                AddErrorExample(operation.Responses["401"], "Authentication required",
                    "Please provide a valid JWT token");
            }

            if (operation.Responses.ContainsKey("404"))
            {
                AddErrorExample(operation.Responses["404"], "Resource not found",
                    "The requested resource does not exist");
            }

            if (operation.Responses.ContainsKey("500"))
            {
                AddErrorExample(operation.Responses["500"], "Internal error",
                    "An unexpected error occurred while processing the request");
            }
        }

        private void AddErrorExample(OpenApiResponse response, string summary, string message)
        {
            if (!response.Content.ContainsKey("application/json"))
            {
                response.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = "object" }
                });
            }

            response.Content["application/json"].Examples = new Dictionary<string, OpenApiExample>
            {
                ["Error"] = new OpenApiExample
                {
                    Summary = summary,
                    Value = new OpenApiObject
                    {
                        ["success"] = new OpenApiBoolean(false),
                        ["message"] = new OpenApiString(message),
                        ["data"] = new OpenApiNull(),
                        ["count"] = new OpenApiInteger(0),
                        ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O"))
                    }
                }
            };
        }
    }

    /// <summary>
    /// Document filter for additional Swagger customization
    /// </summary>
    public class SwaggerDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Add server information
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer
                {
                    Url = "https://localhost:5001",
                    Description = "Development Server (HTTPS)"
                },
                new OpenApiServer
                {
                    Url = "http://localhost:5208",
                    Description = "Development Server (HTTP)"
                },
                new OpenApiServer
                {
                    Url = "https://api.gasfiremonitoring.com",
                    Description = "Production Server"
                },
                new OpenApiServer
                {
                    Url = "https://staging-api.gasfiremonitoring.com",
                    Description = "Staging Server"
                }
            };

            // Add additional tags with descriptions
            swaggerDoc.Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = "🔐 Authentication",
                    Description = "User authentication and authorization endpoints. Obtain JWT tokens for API access."
                },
                new OpenApiTag
                {
                    Name = "📊 Sensors & Monitoring",
                    Description = "Real-time sensor data from gas and fire detectors. Monitor sensor status, values, and connectivity."
                },
                new OpenApiTag
                {
                    Name = "🚨 Alarms & Alerts",
                    Description = "Alarm history and active alerts. Track Level 1 (Warning) and Level 2 (Critical) alarms."
                },
                new OpenApiTag
                {
                    Name = "🏭 Sites & Locations",
                    Description = "Industrial site information and status. View site health, sensor counts, and geographic data."
                },
                new OpenApiTag
                {
                    Name = "🗺️ Layout Management",
                    Description = "SVG layout management for site visualization. Upload layouts and position sensors."
                },
                new OpenApiTag
                {
                    Name = "⚙️ Configuration",
                    Description = "System configuration management. Manage sites, sensors, and system settings."
                }
            };

            // Add external documentation
            swaggerDoc.ExternalDocs = new OpenApiExternalDocs
            {
                Description = "Gas Fire Monitoring System Documentation",
                Url = new Uri("https://docs.gasfiremonitoring.com")
            };

            // Order paths alphabetically
            var paths = swaggerDoc.Paths.OrderBy(p => p.Key).ToList();
            swaggerDoc.Paths.Clear();
            foreach (var path in paths)
            {
                swaggerDoc.Paths.Add(path.Key, path.Value);
            }

            // Add common schemas
            AddCommonSchemas(swaggerDoc);
        }

        private void AddCommonSchemas(OpenApiDocument swaggerDoc)
        {
            // Add common error response schema
            swaggerDoc.Components.Schemas.Add("ErrorResponse", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["success"] = new OpenApiSchema
                    {
                        Type = "boolean",
                        Description = "Indicates if the operation was successful",
                        Example = new OpenApiBoolean(false)
                    },
                    ["message"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Error message describing what went wrong",
                        Example = new OpenApiString("An error occurred")
                    },
                    ["data"] = new OpenApiSchema
                    {
                        Type = "object",
                        Nullable = true,
                        Description = "Response data (null for errors)"
                    },
                    ["count"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Description = "Number of items (0 for errors)",
                        Example = new OpenApiInteger(0)
                    },
                    ["timestamp"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "date-time",
                        Description = "UTC timestamp of the response",
                        Example = new OpenApiString(DateTime.UtcNow.ToString("O"))
                    }
                },
                Required = new HashSet<string> { "success", "message", "timestamp" }
            });

            // Add pagination schema
            swaggerDoc.Components.Schemas.Add("PaginationInfo", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["pageNumber"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Description = "Current page number",
                        Example = new OpenApiInteger(1)
                    },
                    ["pageSize"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Description = "Number of items per page",
                        Example = new OpenApiInteger(50)
                    },
                    ["totalPages"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Description = "Total number of pages",
                        Example = new OpenApiInteger(10)
                    },
                    ["totalItems"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Description = "Total number of items",
                        Example = new OpenApiInteger(500)
                    }
                }
            });

            // Add sensor status enum
            swaggerDoc.Components.Schemas.Add("SensorStatus", new OpenApiSchema
            {
                Type = "integer",
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiInteger(0),
                    new OpenApiInteger(1),
                    new OpenApiInteger(2),
                    new OpenApiInteger(3),
                    new OpenApiInteger(4),
                    new OpenApiInteger(5),
                    new OpenApiInteger(6)
                },
                Description = @"Sensor status codes:
                    * `0` - Normal Operation
                    * `1` - Alarm Level 1 (Warning)
                    * `2` - Alarm Level 2 (Critical)
                    * `3` - Detector Error
                    * `4` - Detector Disabled
                    * `5` - Line Open Fault
                    * `6` - Line Short Fault"
            });

            // Add detector type enum
            swaggerDoc.Components.Schemas.Add("DetectorType", new OpenApiSchema
            {
                Type = "integer",
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiInteger(1),
                    new OpenApiInteger(2),
                    new OpenApiInteger(3),
                    new OpenApiInteger(4)
                },
                Description = @"Detector type codes:
                    * `1` - Gas Detector (H2S, CH4, etc.)
                    * `2` - Flame Detector (UV/IR)
                    * `3` - Smoke Detector
                    * `4` - Heat Detector"
            });

            // Add user role enum
            swaggerDoc.Components.Schemas.Add("UserRole", new OpenApiSchema
            {
                Type = "string",
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("CEO"),
                    new OpenApiString("Regional"),
                    new OpenApiString("Operator")
                },
                Description = @"User roles:
                    * `CEO` - Full system access
                    * `Regional` - County-level access
                    * `Operator` - Site-level access"
            });
        }
    }

    /// <summary>
    /// Extension methods for API description
    /// </summary>
    public static class ApiDescriptionExtensions
    {
        public static bool IsDeprecated(this ApiDescription apiDescription)
        {
            var attributes = apiDescription.ActionDescriptor.EndpointMetadata
                .OfType<ObsoleteAttribute>();
            return attributes.Any();
        }
    }
}