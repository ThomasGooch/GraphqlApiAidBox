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
    public class DocumentReferenceTests
    {
        private PassthroughController CreateController(string tempQueriesPath)
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
            
            // Set private fields using reflection
            typeof(PassthroughController)
                .GetField("_queriesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, tempQueriesPath);
            typeof(PassthroughController)
                .GetField("_aidboxGraphqlUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, "");
            
            return controller;
        }

        [Fact]
        public async Task DocumentReference_WithValidParameters_ReturnsProcessedQuery()
        {
            // Arrange
            var fileContent = "query GetDocumentReferenceUrl { DocumentReferenceList(subject: \"{subject}\", related: \"{related}\") { content { attachment { url } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetDocumentReferenceUrlQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "documentreference",
                ["variables"] = new JsonObject
                {
                    ["subject"] = "Patient/test-123",
                    ["related"] = "Consent/consent-456"
                }
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            // Verify response contains processed query
            Assert.NotNull(response);
            
            // The response should contain the processed GraphQL query with substituted variables
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetDocumentReferenceUrl", responseJson);
            Assert.Contains("Patient/test-123", responseJson);
            Assert.Contains("Consent/consent-456", responseJson);
        }

        [Fact]
        public async Task DocumentReference_CaseInsensitive_ReturnsProcessedQuery()
        {
            // Arrange
            var fileContent = "query GetDocumentReferenceUrl { DocumentReferenceList(subject: \"{subject}\", related: \"{related}\") { content { attachment { url } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetDocumentReferenceUrlQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "DOCUMENTREFERENCE",
                ["variables"] = new JsonObject
                {
                    ["SUBJECT"] = "Patient/case-test",
                    ["RELATED"] = "Consent/case-consent"
                }
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetDocumentReferenceUrl", responseJson);
            Assert.Contains("Patient/case-test", responseJson);
            Assert.Contains("Consent/case-consent", responseJson);
        }

        [Fact]
        public async Task DocumentReference_MissingSubject_FallsBackToDefault()
        {
            // Arrange
            var fileContent = "query GetAllConsents { ConsentList { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "documentreference",
                ["variables"] = new JsonObject
                {
                    ["related"] = "Consent/consent-456"
                }
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Should fall back to default query (GetAllConsents) when required parameters are missing
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetAllConsents", responseJson);
        }

        [Fact]
        public async Task DocumentReference_MissingRelated_FallsBackToDefault()
        {
            // Arrange
            var fileContent = "query GetAllConsents { ConsentList { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "documentreference",
                ["variables"] = new JsonObject
                {
                    ["subject"] = "Patient/test-123"
                }
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Should fall back to default query when required parameters are missing
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetAllConsents", responseJson);
        }

        [Fact]
        public async Task DocumentReference_EmptyVariables_FallsBackToDefault()
        {
            // Arrange
            var fileContent = "query GetAllConsents { ConsentList { id } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetAllConsentsQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "documentreference",
                ["variables"] = new JsonObject()
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Should fall back to default query when no variables provided
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetAllConsents", responseJson);
        }

        [Fact]
        public async Task DocumentReference_SpecialCharactersInVariables_HandlesCorrectly()
        {
            // Arrange
            var fileContent = "query GetDocumentReferenceUrl { DocumentReferenceList(subject: \"{subject}\", related: \"{related}\") { content { attachment { url } } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetDocumentReferenceUrlQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var body = new JsonObject
            {
                ["Resource"] = "documentreference",
                ["variables"] = new JsonObject
                {
                    ["subject"] = "Patient/test-with-special-chars-123",
                    ["related"] = "Consent/consent-with-dashes_and_underscores"
                }
            };

            // Act
            var result = await controller.Post(body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetDocumentReferenceUrl", responseJson);
            Assert.Contains("Patient/test-with-special-chars-123", responseJson);
            Assert.Contains("Consent/consent-with-dashes_and_underscores", responseJson);
        }
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            // Return a fake response for testing
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\": {\"message\": \"Mock response: Query processed successfully\"}}")
            };
            return Task.FromResult(response);
        }
    }
}
