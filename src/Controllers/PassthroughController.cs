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
            _aidboxGraphqlUrl = configuration["AidboxGraphqlUrl"] ?? throw new Exception("AidboxGraphqlUrl not configured");
            _queriesPath = configuration["QueriesPath"] ?? Path.Combine(AppContext.BaseDirectory, "Graphql", "Queries");
        }

        // For testability (internal so DI will not use it)
        internal PassthroughController(IHttpClientFactory httpClientFactory, string aidboxGraphqlUrl, string queriesPath)
        {
            _httpClientFactory = httpClientFactory;
            _aidboxGraphqlUrl = aidboxGraphqlUrl;
            _queriesPath = queriesPath;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonObject? body)
        {

            string? queryFilePath = null;
            string queryText;
            JsonObject variables = new JsonObject();

            // Extract variables if present
            if (body != null && body.TryGetPropertyValue("variables", out var variablesNode) && variablesNode is JsonObject vObj)
                variables = vObj;

            // If no variables, use default query
            if (variables.Count == 0)
            {
                queryFilePath = Directory.GetFiles(_queriesPath, "*.graphql")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("GetAllConsents", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(queryFilePath))
                    return NotFound($"Query file 'GetAllConsents.graphql' not found.");
                queryText = await System.IO.File.ReadAllTextAsync(queryFilePath);
            }
            else
            {
                // Determine query based on resource and variables
                string queryName = "GetAllConsents";
                if (body != null && body.TryGetPropertyValue("Resource", out var resourceNode) && resourceNode is not null)
                {
                    var resource = resourceNode.ToString().ToLowerInvariant();
                    if (resource == "consent" && variables.ContainsKey("id"))
                        queryName = "GetConsentDetailsQuery";
                    else if (resource == "documentreference" && variables.ContainsKey("subject") && variables.ContainsKey("related"))
                        queryName = "GetDocumentReferenceUrlQuery";
                    else if (resource == "provenance" && variables.ContainsKey("id"))
                        queryName = "GetConsentHistoryQuery";
                    else
                        queryName = "GetAllConsents";
                }

                queryFilePath = Directory.GetFiles(_queriesPath, "*.graphql")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(queryName, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(queryFilePath))
                    return NotFound($"Query file '{queryName}.graphql' not found.");
                queryText = await System.IO.File.ReadAllTextAsync(queryFilePath);
            }

            // Interpolate variables into query text if placeholders exist (e.g., {id}, {subject}, {related}, etc.)
            foreach (var kvp in variables)
            {
                var pattern = "{" + kvp.Key + "}";
                queryText = queryText.Replace(pattern, kvp.Value?.ToString() ?? "");
            }

            var client = _httpClientFactory.CreateClient();
            var payload = new { query = queryText, variables = variables };
            var response = await client.PostAsJsonAsync(_aidboxGraphqlUrl, payload);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }
    }
}
