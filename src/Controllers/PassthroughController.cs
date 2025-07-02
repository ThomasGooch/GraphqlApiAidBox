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

            // If AidboxGraphqlUrl is not configured (for testing), return mock response
            if (string.IsNullOrEmpty(_aidboxGraphqlUrl))
            {
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
            var response = await client.PostAsJsonAsync(_aidboxGraphqlUrl, payload);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
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
