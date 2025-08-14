using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrep.Models;


namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;
        
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmODA2OThjZi0wYmFmLTRlYjktYjU3Zi03Zjk1MzRlZDY4M2EiLCJpYXQiOiIwOC8xMy8yMDI1IDE3OjQ4OjA3IiwiVXNlcklkIjoiNzg1NzY5ZTUtZjg5Mi00ZTBkLWQyYWItMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJycmFkZXZAZXhhbXByZXAuY29tIiwiVXNlck5hbWUiOiJycmFkZXYxMjMiLCJleHAiOjE3NTUxMjg4ODcsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.EDepkesa5E6J6cxbZiBXN-b5NivwmeS4ClLQRxZTHrg";

        private const string LoginEmail = "rradev@examprep.com";
        private const string LoginPassword = "rradev123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }


        //All tests here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfAllIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);

            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditExistingIdea_ShouldReturnSuccess()
        {
           var editRequest = new IdeaDTO
           {
               Title = "Edited Idea",
               Description = "This is an updated test idea description.",
               Url = ""
           };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);         
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithMissingFields_ShouldReturnBadRequest()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "", //Missing title
                Description = "", // Missing description
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Non-existing Idea",
                Description = "This idea does not exist.",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
             
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }








        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}