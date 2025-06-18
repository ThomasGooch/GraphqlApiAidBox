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

        public PassthroughController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _aidboxGraphqlUrl = configuration["AidboxGraphqlUrl"] ?? throw new Exception("AidboxGraphqlUrl not configured");
        }

        public class PassthroughRequest
        {
            public required string Query { get; set; }
            public required JsonObject Variables { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PassthroughRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            string interpolatedQuery = InterpolateQuery(request.Query, request.Variables);
            var payload = new { query = interpolatedQuery, variables = new { } };
            var response = await client.PostAsJsonAsync(_aidboxGraphqlUrl, payload);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }

        // Helper to replace {{var}} in query with values from variables
        private string InterpolateQuery(string query, JsonObject variables)
        {
            if (variables == null) return query;
            foreach (var kvp in variables)
            {
                var pattern = "{{" + kvp.Key + "}}";
                query = query.Replace(pattern, kvp.Value?.ToString() ?? "");
            }
            return query;
        }
    }
}
