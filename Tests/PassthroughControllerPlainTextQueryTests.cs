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

namespace GraphqlApiAidBox.Tests
{
    public class PassthroughControllerPlainTextQueryTests
    {
        [Fact]
        public async Task Returns_AllConsents_When_No_Variables()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetAllConsents { ConsentList { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject();
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_ConsentDetails_When_Consent_With_Id()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "consent", ["variables"] = new JsonObject { ["id"] = "abc" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_DocumentReferenceUrl_When_DocumentReference_With_Subject_And_Related()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetDocumentReferenceUrl($subject: string, $related: string) { DocumentReferenceList(subject: \"{subject}\", related: \"{related}\") { content { attachment { url } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetDocumentReferenceUrlQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "documentreference", ["variables"] = new JsonObject { ["subject"] = "pat1", ["related"] = "cons1" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_ConsentHistory_When_Provenance_With_Id()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query { ProvenanceList(target: \"{id}\") { id recorded meta { lastUpdated } activity { coding { display } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = "prov1" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_NotFound_When_QueryFile_Missing()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "DoesNotExist", ["variables"] = new JsonObject { ["id"] = "123" } };
            var result = await controller.Post(body);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not supported", notFoundResult.Value?.ToString()?.ToLowerInvariant());
        }

        [Fact]
        public async Task ID_Substitution_Works_With_Case_Insensitive_Variables()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with uppercase ID variable
            var body = new JsonObject { ["Resource"] = "consent", ["variables"] = new JsonObject { ["ID"] = "test-id-123" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Direct_Query_With_Variables_Works()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with direct query
            var body = new JsonObject 
            { 
                ["query"] = "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }",
                ["variables"] = new JsonObject { ["id"] = "direct-test-123" } 
            };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Verify_ID_Is_Properly_Substituted_In_GraphQL_Query()
        {
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentDetails { ConsentList(_id: \"{id}\") { id status } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with specific ID
            var testId = "consent-123-abc";
            var body = new JsonObject { ["Resource"] = "consent", ["variables"] = new JsonObject { ["id"] = testId } };
            var result = await controller.Post(body);
            
            Assert.IsType<ContentResult>(result);
            // Check that the ID was properly substituted
            Assert.Contains(testId, capturedRequest);
            Assert.DoesNotContain("{id}", capturedRequest); // Ensure placeholder was replaced
        }

        [Fact]
        public async Task Returns_ProvenanceHistory_When_Provenance_With_Id()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentHistory { ProvenanceList(target: \"{id}\") { id recorded activity { coding { display } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = "consent-123" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Provenance_Query_Case_Insensitive_Resource_And_Variable()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentHistory { ProvenanceList(target: \"{id}\") { id recorded } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with uppercase resource and variable names
            var body = new JsonObject { ["Resource"] = "PROVENANCE", ["variables"] = new JsonObject { ["ID"] = "consent-456" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Provenance_Without_Id_Returns_BadRequest()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var allConsentsContent = "query GetAllConsents { ConsentList { id status } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", allConsentsContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Provenance resource but no ID variable should now return BadRequest due to enhanced validation
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["someOtherField"] = "value" } };
            var result = await controller.Post(body);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Verify_Provenance_ID_Is_Properly_Substituted_In_GraphQL_Query()
        {
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentHistory { ProvenanceList(target: \"{id}\") { id recorded activity { coding { display } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", fileContent);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with specific ID
            var testId = "consent-history-123";
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = testId } };
            var result = await controller.Post(body);
            
            Assert.IsType<ContentResult>(result);
            Assert.Contains(testId, capturedRequest); // Check if the ID appears anywhere in the request
            Assert.DoesNotContain("{id}", capturedRequest); // Ensure placeholder was replaced
        }

        [Fact]
        public async Task Enhanced_Provenance_Query_Contains_Basic_Fields()
        {
            var capturedRequest = "";
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var httpMessageHandler = new CapturingHttpMessageHandler(req => capturedRequest = req);
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));
            
            var aidboxUrl = "http://fake-url";
            // Use the actual query file
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", 
                System.IO.File.ReadAllText("/Users/harish.mukkapati/Documents/GitHub/GraphqlApiAidBox/src/Graphql/Queries/GetConsentHistoryQuery.graphql"));
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            var testId = "comprehensive-test-123";
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = testId } };
            var result = await controller.Post(body);
            
            Assert.IsType<ContentResult>(result);
            
            // Verify the enhanced query has proper structure
            Assert.Contains("GetConsentHistory", capturedRequest); // Named query
            Assert.Contains(testId, capturedRequest); // ID substitution worked
            Assert.DoesNotContain("{id}", capturedRequest); // Placeholder replaced
            
            // Verify basic field selection that exists in current query
            Assert.Contains("ProvenanceList", capturedRequest); // Main query
            Assert.Contains("recorded", capturedRequest); // Basic field
            Assert.Contains("activity", capturedRequest); // Activity information
            Assert.Contains("coding", capturedRequest); // Coding details
            Assert.Contains("display", capturedRequest); // Display field
            Assert.Contains("meta", capturedRequest); // Meta information
            Assert.Contains("lastUpdated", capturedRequest); // Last updated field
        }

        // New Enhanced Tests for Controller Improvements

        [Fact]
        public async Task Returns_BadRequest_When_Provenance_Request_Has_Invalid_ID()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with empty ID - should return BadRequest
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = "" } };
            var result = await controller.Post(body);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Returns_BadRequest_When_Provenance_Request_Has_Very_Short_ID()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with single character ID - should return BadRequest
            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = "x" } };
            var result = await controller.Post(body);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid provenance request", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task Returns_BadRequest_When_Request_Body_Is_Null()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var logger = Substitute.For<ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            
            // Test with null body - should return BadRequest
            var result = await controller.Post(null);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Request body is required", badRequestResult.Value?.ToString());
        }

        // Fake handler to return a dummy response for HttpClient in tests
        public class FakeHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{}}", System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        // Custom handler to capture the request content
        public class CapturingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Action<string> _captureAction;

            public CapturingHttpMessageHandler(Action<string> captureAction)
            {
                _captureAction = captureAction;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _captureAction(content);
                }

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{}}", System.Text.Encoding.UTF8, "application/json")
                };
                return response;
            }
        }
    }
}
