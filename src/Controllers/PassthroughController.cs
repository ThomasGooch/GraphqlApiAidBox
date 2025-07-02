using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GraphqlApiAidBox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PassthroughController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _aidboxGraphqlUrl;
        private readonly string _queriesPath;
        private readonly ILogger<PassthroughController> _logger;

        public PassthroughController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PassthroughController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _aidboxGraphqlUrl = configuration["AidboxGraphqlUrl"] ?? "";
            _queriesPath = configuration["QueriesPath"] ?? Path.Combine(AppContext.BaseDirectory, "Graphql", "Queries");
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonObject? body)
        {
            try
            {
                _logger.LogInformation("Processing GraphQL passthrough request");
                
                if (body == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest(new { error = "Request body is required" });
                }

                string? queryFilePath = null;
                string? queryText = null;
                JsonObject variables = new JsonObject();

                // Extract variables if present
                if (body.TryGetPropertyValue("variables", out var variablesNode) && variablesNode is JsonObject vObj)
                    variables = vObj;

                // Extract id if present as direct property
                if (body.TryGetPropertyValue("id", out var idNode) && !string.IsNullOrWhiteSpace(idNode?.ToString()))
                {
                    variables["id"] = JsonValue.Create(idNode.ToString());
                }

                // Extract subject if present as direct property (for document reference)
                if (body.TryGetPropertyValue("subject", out var subjectNode) && !string.IsNullOrWhiteSpace(subjectNode?.ToString()))
                {
                    variables["subject"] = JsonValue.Create(subjectNode.ToString());
                }

                // Extract related if present as direct property (for document reference)
                if (body.TryGetPropertyValue("related", out var relatedNode) && !string.IsNullOrWhiteSpace(relatedNode?.ToString()))
                {
                    variables["related"] = JsonValue.Create(relatedNode.ToString());
                }

                // Check if query is provided directly in the body
                if (body.TryGetPropertyValue("query", out var queryNode) && !string.IsNullOrWhiteSpace(queryNode?.ToString()))
                {
                    queryText = queryNode.ToString();
                    _logger.LogInformation("Using direct query from request body");
                }
                else
                {
                    // Determine query name based on resource and variables
                    string queryName = "GetAllConsentsQuery";
                    
                    // Check both "Resource" and "resource" (case insensitive)
                    var resourceProperty = body.FirstOrDefault(kvp => 
                        string.Equals(kvp.Key, "resource", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kvp.Key, "Resource", StringComparison.OrdinalIgnoreCase));
                    
                    if (resourceProperty.Value is not null)
                    {
                        var resource = resourceProperty.Value.ToString().ToLowerInvariant();
                        
                        if (resource == "consent" && HasVariable(variables, "id"))
                        {
                            queryName = "GetConsentDetailsQuery";
                            _logger.LogInformation("Using consent details query for ID: {ConsentId}", GetVariableValue(variables, "id"));
                        }
                        else if (resource == "documentreference" && HasVariable(variables, "subject") && HasVariable(variables, "related"))
                        {
                            queryName = "GetDocumentReferenceUrlQuery";
                            _logger.LogInformation("Using document reference query");
                        }
                        else if (resource == "provenance")
                        {
                            if (!IsValidHistoryRequest(variables))
                            {
                                _logger.LogWarning("Invalid provenance request: missing or invalid ID parameter");
                                return BadRequest(new { 
                                    error = "Invalid provenance request", 
                                    message = "Valid ID parameter is required for provenance/history queries" 
                                });
                            }
                            
                            queryName = "GetConsentHistoryQuery";
                            _logger.LogInformation("Using consent history query for ID: {ConsentId}", GetVariableValue(variables, "id"));
                        }
                        else if (resource == "consent")
                        {
                            queryName = "GetAllConsentsQuery";
                            _logger.LogInformation("Using default consent query: {QueryName}", queryName);
                        }
                        else
                        {
                            _logger.LogError("Unknown resource type: {Resource}", resource);
                            return NotFound(new { error = $"Resource type '{resource}' not supported" });
                        }
                    }

                    queryFilePath = Directory.GetFiles(_queriesPath, "*.graphql")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(queryName, StringComparison.OrdinalIgnoreCase));
                    
                    if (string.IsNullOrEmpty(queryFilePath))
                    {
                        _logger.LogError("Query file not found: {QueryName}", queryName);
                        return NotFound(new { error = $"Query file '{queryName}.graphql' not found" });
                    }

                    queryText = (await System.IO.File.ReadAllTextAsync(queryFilePath)).Trim();
                    _logger.LogInformation("Loaded query from file: {QueryFile}", Path.GetFileName(queryFilePath));
                }

                // Interpolate variables into query text if placeholders exist
                if (!string.IsNullOrEmpty(queryText))
                {
                    var originalQuery = queryText;
                    foreach (var kvp in variables)
                    {
                        // Make placeholder matching case-insensitive
                        var pattern = "{" + kvp.Key + "}";
                        var regex = new Regex(Regex.Escape(pattern), RegexOptions.IgnoreCase);
                        queryText = regex.Replace(queryText, kvp.Value?.ToString() ?? "");
                    }
                    
                    if (originalQuery != queryText)
                    {
                        _logger.LogInformation("Substituted {VariableCount} variables in query", variables.Count);
                    }
                }

                // Check if AidboxGraphqlUrl is configured
                if (string.IsNullOrEmpty(_aidboxGraphqlUrl))
                {
                    // Return a mock response for testing when no external URL is configured
                    _logger.LogInformation("No external GraphQL URL configured, returning mock response");
                    var mockResponse = new
                    {
                        data = new { message = "Mock response: Query processed successfully" },
                        processedQuery = queryText,
                        processedVariables = variables
                    };
                    return Ok(mockResponse);
                }

                var client = _httpClientFactory.CreateClient();
                var payload = new { query = queryText, variables = variables };
                
                _logger.LogInformation("Sending request to Aidbox GraphQL endpoint");
                var response = await client.PostAsJsonAsync(_aidboxGraphqlUrl, payload);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Aidbox GraphQL request failed with status {StatusCode}: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    return StatusCode((int)response.StatusCode, new { 
                        error = "GraphQL request failed", 
                        details = response.ReasonPhrase 
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("GraphQL request completed successfully");
                
                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing GraphQL request");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        private static bool IsValidHistoryRequest(JsonObject variables)
        {
            // Validate ID exists and is meaningful for history queries
            if (!HasVariable(variables, "id"))
                return false;
            
            var idValue = GetVariableValue(variables, "id");
            if (string.IsNullOrWhiteSpace(idValue) || idValue.Length < 2)
                return false;
            
            return true;
        }

        private static string? GetVariableValue(JsonObject variables, string key)
        {
            return variables.FirstOrDefault(kvp => 
                string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                .Value?.ToString();
        }

        private static bool HasVariable(JsonObject variables, string key)
        {
            return variables.Any(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && 
                                       kvp.Value != null && 
                                       !string.IsNullOrWhiteSpace(kvp.Value.ToString()));
        }
    }
}
