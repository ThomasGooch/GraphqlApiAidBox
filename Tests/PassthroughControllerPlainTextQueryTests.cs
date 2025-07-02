using Xunit;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System;
using GraphqlApiAidBox.Controllers;

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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, aidboxUrl);
            var body = new JsonObject { ["Resource"] = "DoesNotExist", ["variables"] = new JsonObject { ["id"] = "123" } };
            var result = await controller.Post(body);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString()?.ToLowerInvariant());
        }

        [Fact]
        public async Task ID_Substitution_Works_With_Case_Insensitive_Variables()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", fileContent);
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
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
