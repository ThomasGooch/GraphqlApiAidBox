using Xunit;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
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
    }
}
