using Xunit;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using GraphqlApiAidBox.Controllers;

namespace GraphqlApiAidBox.Tests
{
    public class PassthroughControllerTests
    {
        [Fact]
        public async Task Returns_NotFound_When_QueryFile_Missing()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            // Use a temp directory for queries path, but do not create any .graphql files
            var tempQueriesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempQueriesPath);
            var controller = new PassthroughController(httpClientFactory, aidboxUrl, tempQueriesPath);

            // Use a resource that does not exist
            var body = new JsonObject { ["Resource"] = "DoesNotExist", ["variables"] = new JsonObject { ["id"] = "123" } };

            // Act
            var result = await controller.Post(body);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString()?.ToLowerInvariant());
        }

        [Fact]
        public async Task Returns_ConsentDetails_When_Consent_With_Id()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query { ConsentList(_id: {id}) { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentDetailsQuery.graphql", fileContent);
            var controller = new PassthroughController(httpClientFactory, aidboxUrl, tempQueriesPath);

            var body = new JsonObject { ["Resource"] = "consent", ["variables"] = new JsonObject { ["id"] = "abc" } };
            var result = await controller.Post(body);
            // Should attempt to call Aidbox, but since httpClient is mocked, just check for ContentResult
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_DocumentReferenceUrl_When_DocumentReference_With_Subject_And_Related()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query { DocumentReferenceList(subject: {subject}, related: {related}) { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetDocumentReferenceUrlQuery.graphql", fileContent);
            var controller = new PassthroughController(httpClientFactory, aidboxUrl, tempQueriesPath);

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
            var fileContent = "query { ProvenanceList(_id: {id}) { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetConsentHistoryQuery.graphql", fileContent);
            var controller = new PassthroughController(httpClientFactory, aidboxUrl, tempQueriesPath);

            var body = new JsonObject { ["Resource"] = "provenance", ["variables"] = new JsonObject { ["id"] = "prov1" } };
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task Returns_AllConsents_When_No_Variables()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var aidboxUrl = "http://fake-url";
            var fileContent = "query { ConsentList { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsents.graphql", fileContent);
            var controller = new PassthroughController(httpClientFactory, aidboxUrl, tempQueriesPath);

            var body = new JsonObject();
            var result = await controller.Post(body);
            Assert.IsType<ContentResult>(result);
        }
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
