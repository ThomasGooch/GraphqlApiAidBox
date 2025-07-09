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
    public class PatientSearchTests
    {
        private PassthroughController CreateController(string tempQueriesPath)
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler()));
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PassthroughController>>();
            var controller = new PassthroughController(httpClientFactory, Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>(), logger);
            
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
        public async Task Post_WithPatientResourceAndGivenName_ReturnsPatientSearchQuery()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["resource"] = "patient",
                ["given"] = "John"
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Verify the response contains processed query
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetPatientsByName", responseJson);
            Assert.Contains("John", responseJson);
        }

        [Fact]
        public async Task Post_WithPatientResourceAndFamilyName_ReturnsPatientSearchQuery()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["resource"] = "patient",
                ["family"] = "Doe"
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetPatientsByName", responseJson);
            Assert.Contains("Doe", responseJson);
        }

        [Fact]
        public async Task Post_WithPatientResourceAndBothNames_ReturnsPatientSearchQuery()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["resource"] = "patient",
                ["given"] = "John",
                ["family"] = "Doe"
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetPatientsByName", responseJson);
            Assert.Contains("John", responseJson);
            Assert.Contains("Doe", responseJson);
        }

        [Fact]
        public async Task Post_WithPatientResourceButNoNames_ReturnsBadRequest()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["resource"] = "patient"
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            Assert.NotNull(errorResponse);
            
            var errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            Assert.Contains("Invalid patient search request", errorJson);
        }

        [Fact]
        public async Task Post_WithPatientResourceAndEmptyGiven_ReturnsBadRequest()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["resource"] = "patient",
                ["given"] = ""
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            Assert.NotNull(errorResponse);
            
            var errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            Assert.Contains("Invalid patient search request", errorJson);
        }

        [Fact]
        public async Task Post_WithPatientResourceCaseInsensitive_ReturnsPatientSearchQuery()
        {
            // Arrange
            var fileContent = "query GetPatientsByName { PatientList(given: \"{given}\", family: \"{family}\") { id name { given family } } }";
            var tempQueriesPath = TestHelpers.CreateTempQueriesDirWithFile("GetPatientsByNameQuery.graphql", fileContent);
            var controller = CreateController(tempQueriesPath);
            
            var requestBody = new JsonObject
            {
                ["Resource"] = "Patient", // Test case insensitive resource
                ["Given"] = "Jane",       // Test case insensitive parameter names
                ["Family"] = "Smith"
            };

            // Act
            var result = await controller.Post(requestBody);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("GetPatientsByName", responseJson);
            Assert.Contains("Jane", responseJson);
            Assert.Contains("Smith", responseJson);
        }
    }
}
