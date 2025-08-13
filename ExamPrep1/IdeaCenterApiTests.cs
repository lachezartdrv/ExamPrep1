using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrep1.Models;


namespace ExamPrep1
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhNDE5MjVhYi1jNzhlLTRhM2ItOTA2NC1kYWI5ZTQzNzg2NDAiLCJpYXQiOiIwOC8xMy8yMDI1IDA5OjEzOjA1IiwiVXNlcklkIjoiNDlhYTAyZGEtMGRiOS00Y2VmLWQyODQtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJleG1wcnBAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6ImV4bXBycCIsImV4cCI6MTc1NTA5Nzk4NSwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.phlzYdATnF6P56ElNUqtuZajXK-1FWDxmVZETdzNmkU";

        private const string LoginEmail = "exmprp@example.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options); //this. infron of client
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new
            {
                email,
                password
            });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token form the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        

        // All tests here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test",
                Description = "Test description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
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
                Title = "Edited",
                Description = "Edited description",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = "",
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Edited non-existing",
                Description = "updated non-existing",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddJsonBody(editRequest);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
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

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }




        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
}