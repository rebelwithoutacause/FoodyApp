using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace FoodySystem
{
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [SetUp]
        public void Setup()
        {
            string token = GetJwtToken("ted123", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("Failed to retrieve JWT token.");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [TearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        [Order(1)]
        [Test]
        public void CreateFood_ShouldReturnCreated()
        {
            var foodInfo = new
            {
                Name = "New Food",
                Description = "Delicious Food Item",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(foodInfo);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                $"Expected Created, but got {response.StatusCode}. Response: {response.Content}");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;

            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty.");
        }

        [Order(2)]
        [Test]

        public void EditFoodTitle_ShouldReturnOK()
        {
            var changes = new[]
            {
                 new { path = "/name", op = "replace", value = "Updated food name" }
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddHeader("Content-Type", "application/json-patch+json");
            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected OK, but got {response.StatusCode}. Response: {response.Content}");
        }

        [Order(3)]
        [Test]

        public void GetAllFoods_ShouldReturnAllItems() 
        {
            var request = new RestRequest("/api/Food/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(foods, Is.Not.Empty);
  
        }

        [Order(4)]
        [Test]

        public void DeleteFood_ShouldReturnOk() 
        {

            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        }
    }   
}
