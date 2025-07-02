using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace GraphqlApiAidBox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PassthroughController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _aidboxGraphqlUrl;

        private readonly string _queriesPath;

        public PassthroughController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _aidboxGraphqlUrl = configuration["AidboxGraphqlUrl"] ?? "";
            _queriesPath = configuration["QueriesPath"] ?? Path.Combine(AppContext.BaseDirectory, "Graphql", "Queries");
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonObject? body)
        {
            string? queryFilePath = null;
            string? queryText = null;
            JsonObject variables = new JsonObject();

            // Extract variables if present
            if (body != null && body.TryGetPropertyValue("variables", out var variablesNode) && variablesNode is JsonObject vObj)
                variables = vObj;

            // Check if query is provided directly in the body
            if (body != null && body.TryGetPropertyValue("query", out var queryNode) && !string.IsNullOrWhiteSpace(queryNode?.ToString()))
            {
                queryText = queryNode.ToString();
            }
            else
            {
                // Determine query name based on Resource and variables
                string queryName = "GetAllConsentsQuery";
                if (body != null && body.TryGetPropertyValue("Resource", out var resourceNode) && resourceNode is not null)
                {
                    var resource = resourceNode.ToString().ToLowerInvariant();
                    if (resource == "consent" && HasVariable(variables, "id"))
                        queryName = "GetConsentDetailsQuery";
                    else if (resource == "documentreference" && HasVariable(variables, "subject") && HasVariable(variables, "related"))
                        queryName = "GetDocumentReferenceUrlQuery";
                    else if (resource == "provenance" && HasVariable(variables, "id"))
                        queryName = "GetConsentHistoryQuery";
                }

                queryFilePath = Directory.GetFiles(_queriesPath, "*.graphql")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(queryName, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(queryFilePath))
                    return NotFound($"Query file '{queryName}.graphql' not found.");

                queryText = (await System.IO.File.ReadAllTextAsync(queryFilePath)).Trim();
            }

            // Interpolate variables into query text if placeholders exist (e.g., {id}, {subject}, {related}, etc.)
            if (!string.IsNullOrEmpty(queryText))
            {
                foreach (var kvp in variables)
                {
                    // Make placeholder matching case-insensitive
                    var pattern = "{" + kvp.Key + "}";
                    var regex = new Regex(Regex.Escape(pattern), RegexOptions.IgnoreCase);
                    queryText = regex.Replace(queryText, kvp.Value?.ToString() ?? "");
                }
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

        private static bool HasVariable(JsonObject variables, string key)
        {
            return variables.Any(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && 
                                       kvp.Value != null && 
                                       !string.IsNullOrWhiteSpace(kvp.Value.ToString()));
        }
    }
}
