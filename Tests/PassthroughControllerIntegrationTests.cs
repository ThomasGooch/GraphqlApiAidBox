using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GraphqlApiAidBox.Tests
{
    public class PassthroughControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public PassthroughControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Post_DirectQuery_ReturnsSuccess()
        {
            // Arrange - Use correct JSON format per coding instructions
            var requestBody = new
            {
                query = "query { ConsentList { id } }",
                variables = new { }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/passthrough", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Post_ConsentResourceWithId_ReturnsSuccess()
        {
            // Arrange - Use correct resource-based JSON format
            var requestBody = new
            {
                resource = "consent",
                id = "/Consent/test-123"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/passthrough", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Post_InvalidProvenanceQuery_ReturnsBadRequest()
        {
            // Arrange - Use correct JSON format for resource-based queries
            var requestBody = new
            {
                resource = "provenance"
                // Missing required 'id' field for provenance queries
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/passthrough", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid provenance request", responseContent);
        }

        [Fact]
        public async Task Post_NullBody_ReturnsBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/passthrough", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
