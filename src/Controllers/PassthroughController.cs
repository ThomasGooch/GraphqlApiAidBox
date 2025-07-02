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


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonObject? body)
        {
            string? queryFilePath = null;
            string? queryText = null;
            JsonObject variables = new JsonObject();

            // Extract variables if present
            if (body != null && body.TryGetPropertyValue("variables", out var variablesNode) && variablesNode is JsonObject vObj)
                variables = vObj;

            // Determine query name
            string queryName = "GetAllConsentsQuery";
            if (variables.Count > 0 && body != null && body.TryGetPropertyValue("Resource", out var resourceNode) && resourceNode is not null)
            {
                var resource = resourceNode.ToString().ToLowerInvariant();
                if (resource == "consent" && variables.ContainsKey("id"))
                    queryName = "GetConsentDetailsQuery";
                else if (resource == "documentreference" && variables.ContainsKey("subject") && variables.ContainsKey("related"))
                    queryName = "GetDocumentReferenceUrlQuery";
                else if (resource == "provenance" && variables.ContainsKey("id"))
                    queryName = "GetConsentHistoryQuery";
            }

            queryFilePath = Directory.GetFiles(_queriesPath, "*.graphql")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(queryName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(queryFilePath))
                return NotFound($"Query file '{queryName}.graphql' not found.");

            queryText = (await System.IO.File.ReadAllTextAsync(queryFilePath)).Trim();

            // Interpolate variables into query text if placeholders exist (e.g., {id}, {subject}, {related}, etc.)
            if (!string.IsNullOrEmpty(queryText))
            {
                foreach (var kvp in variables)
                {
                    var pattern = "{" + kvp.Key + "}";
                    queryText = queryText.Replace(pattern, kvp.Value?.ToString() ?? "");
                }
            }

            var client = _httpClientFactory.CreateClient();
            var payload = new { query = queryText, variables = variables };
            var response = await client.PostAsJsonAsync(_aidboxGraphqlUrl, payload);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }
    }
}
