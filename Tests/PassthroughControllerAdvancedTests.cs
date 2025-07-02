using Xunit;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System;
using GraphqlApiAidBox.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Threading;

namespace GraphqlApiAidBox.Tests
{
    public class PassthroughControllerAdvancedTests
    {
        [Theory]
        [InlineData("id", "consent-123")]
        [InlineData("ID", "consent-456")]
        [InlineData("Id", "consent-789")]
        [InlineData("iD", "consent-abc")]
        public async Task Post_DirectQuery_CaseInsensitiveVariableSubstitution(string variableKey, string variableValue)
        {
            // Arrange
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["query"] = "query { ConsentList(_id: \"{" + variableKey + "}\") { id } }",
                ["variables"] = new JsonObject { [variableKey] = variableValue }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Contains(variableValue, capturedRequest);
            Assert.DoesNotContain("{" + variableKey + "}", capturedRequest);
        }

        [Fact]
        public async Task Post_ProvenanceQuery_ValidId_Success()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", 
                "query GetConsentHistory { ProvenanceList(target: \"{id}\") { id recorded } }");
            config["QueriesPath"].Returns(tempQueriesPath);
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "provenance",
                ["variables"] = new JsonObject { ["id"] = "valid-consent-id" }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("x")] // Too short
        [InlineData("   \t   ")] // Whitespace only
        public async Task Post_ProvenanceQuery_InvalidId_ReturnsBadRequest(string invalidId)
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "provenance",
                ["variables"] = new JsonObject { ["id"] = invalidId }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Post_ProvenanceQuery_MissingIdVariable_ReturnsBadRequest()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "provenance",
                ["variables"] = new JsonObject { ["other"] = "value" }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Post_ProvenanceQuery_NullIdVariable_ReturnsBadRequest()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "provenance",
                ["variables"] = new JsonObject { ["id"] = null }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task Post_AidboxServerError_ReturnsCorrectStatusCode(HttpStatusCode errorStatusCode)
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var errorHandler = new ErrorHttpMessageHandler(errorStatusCode);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(errorHandler));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["query"] = "query { ConsentList { id } }",
                ["variables"] = new JsonObject()
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)errorStatusCode, statusResult.StatusCode);
            Assert.Contains("GraphQL request failed", statusResult.Value?.ToString());
        }

        [Theory]
        [InlineData("consent", "CONSENT", "Consent")]
        [InlineData("provenance", "PROVENANCE", "Provenance")]
        [InlineData("documentreference", "DOCUMENTREFERENCE", "DocumentReference")]
        public async Task Post_ResourceQuery_CaseInsensitiveResourceNames(params string[] resourceVariations)
        {
            // Test each variation of the resource name
            foreach (var resource in resourceVariations)
            {
                // Arrange
                var httpClientFactory = Substitute.For<IHttpClientFactory>();
                httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
                
                var logger = Substitute.For<ILogger<PassthroughController>>();
                var config = Substitute.For<IConfiguration>();
                config["AidboxGraphqlUrl"].Returns("http://test-url");
                
                // Create appropriate query files based on resource type
                string queryFileName = resource.ToLowerInvariant() switch
                {
                    "consent" => "GetAllConsentsQuery.graphql",
                    "provenance" => "GetConsentHistoryQuery.graphql",
                    "documentreference" => "GetDocumentReferenceUrlQuery.graphql",
                    _ => "GetAllConsentsQuery.graphql"
                };
                
                var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile(queryFileName, 
                    "query { ConsentList { id } }");
                config["QueriesPath"].Returns(tempQueriesPath);
                
                var controller = new PassthroughController(httpClientFactory, config, logger);

                var requestBody = new JsonObject
                {
                    ["Resource"] = resource,
                    ["variables"] = new JsonObject()
                };

                // For provenance, add valid ID to pass validation
                if (resource.ToLowerInvariant() == "provenance")
                {
                    requestBody["variables"] = new JsonObject { ["id"] = "test-id-123" };
                }
                // For documentreference, add required subject and related variables
                else if (resource.ToLowerInvariant() == "documentreference")
                {
                    requestBody["variables"] = new JsonObject 
                    { 
                        ["subject"] = "patient-123",
                        ["related"] = "consent-456"
                    };
                }

                // Act
                var result = await controller.Post(requestBody);

                // Assert
                Assert.IsType<ContentResult>(result);
            }
        }

        [Fact]
        public async Task Post_MultipleVariableSubstitution_WorksCorrectly()
        {
            // Arrange
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["query"] = "query { ConsentList(_id: \"{id}\", status: \"{status}\") { id } }",
                ["variables"] = new JsonObject 
                { 
                    ["id"] = "consent-123",
                    ["status"] = "active"
                }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Contains("consent-123", capturedRequest);
            Assert.Contains("active", capturedRequest);
            Assert.DoesNotContain("{id}", capturedRequest);
            Assert.DoesNotContain("{status}", capturedRequest);
        }

        [Fact]
        public async Task Post_NullBody_ReturnsBadRequest()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            // Act
            var result = await controller.Post(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Request body is required", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Post_EmptyBody_UsesDefaultQuery()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", 
                "query GetAllConsents { ConsentList { id } }");
            config["QueriesPath"].Returns(tempQueriesPath);
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject();

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
        }

        [Fact]
        public async Task Post_NonexistentQueryFile_ReturnsNotFound()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            config["QueriesPath"].Returns(tempQueriesPath);
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "NonexistentResource",
                ["variables"] = new JsonObject { ["id"] = "test-123" }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not supported", notFoundResult.Value?.ToString()?.ToLowerInvariant());
        }

        [Fact]
        public async Task Post_HttpClientException_ReturnsInternalServerError()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var faultyHandler = new ExceptionHttpMessageHandler();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(faultyHandler));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns("http://test-url");
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["query"] = "query { ConsentList { id } }",
                ["variables"] = new JsonObject()
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Contains("Internal server error", statusResult.Value?.ToString());
        }

        [Theory]
        [InlineData("special-@#$%^&*()")]
        [InlineData("unicode-ñáéíóú-测试-🎉")]
        [InlineData("very-long-id-with-many-characters-that-should-still-work-properly-123456789")]
        [InlineData("numbers-123-and-symbols-!@#")]
        public async Task Post_SpecialCharactersInId_WorksCorrectly(string specialId)
        {
            // Arrange
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var config = Substitute.For<IConfiguration>();
            config["AidboxGraphqlUrl"].Returns(""); // Empty to trigger mock response
            
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", 
                "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }");
            config["QueriesPath"].Returns(tempQueriesPath);
            
            var controller = new PassthroughController(httpClientFactory, config, logger);

            var requestBody = new JsonObject
            {
                ["Resource"] = "consent",
                ["variables"] = new JsonObject { ["id"] = specialId }
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseContent = okResult.Value?.ToString();
            Assert.Contains(specialId, responseContent); // Check if ID appears in the substituted query
        }

        // Helper classes for testing
        public class FakeHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{}}", Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        public class CapturingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Action<string> _captureAction;

            public CapturingHttpMessageHandler(Action<string> captureAction)
            {
                _captureAction = captureAction;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _captureAction(content);
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{}}", Encoding.UTF8, "application/json")
                };
                return response;
            }
        }

        public class ErrorHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;

            public ErrorHttpMessageHandler(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    ReasonPhrase = _statusCode.ToString()
                };
                return Task.FromResult(response);
            }
        }

        public class ExceptionHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("Simulated network error");
            }
        }
    }
}
