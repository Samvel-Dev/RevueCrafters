using NUnit.Framework;
using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace RRevueCrafters
{
    [TestFixture]
    public class Revue_FullSuite_Tests
    {
        private const string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net"; // без финален '/'
        private const string AccessToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzNmRmZTljMC0xMTAzLTQ4M2EtYTdmNC02MjRhOTZmNGZhNzciLCJpYXQiOiIwOC8yMi8yMDI1IDA4OjE4OjM5IiwiVXNlcklkIjoiYWE1ZWE2YjYtNWRlZS00Y2JiLTEzM2UtMDhkZGRlMWQ4YTY0IiwiRW1haWwiOiJtYXJpYS5nZW9yZ2lldmFAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6Im1hcmlhX2dlb3JnaWV2YSIsImV4cCI6MTc1NTg3MjMxOSwiaXNzIjoiUmV2dWVNYWtlcl9BcHBfU29mdFVuaSIsImF1ZCI6IlJldnVlTWFrZXJfV2ViQVBJX1NvZnRVbmkifQ.xxq6EH94CXhJApHxNeOH7j8QY_yPk7fCLmv_EZ1c0JA";

        private RestClient client;
        private static string createdTitle = string.Empty;
        private static string createdRevueId = string.Empty;

        [OneTimeSetUp]
        public void Setup() => client = new RestClient(BaseUrl);

        // 1) CREATE -> 200 + "Successfully created!"
        [Test, Order(1)]
        public void CreateRevue_ShouldReturn200_AndSuccessMessage()
        {
            createdTitle = $"NUnit-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var body = new { title = createdTitle, url = "", description = "Created via NUnit" };

            var req = new RestRequest("api/Revue/Create", Method.Post)
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddJsonBody(body);

            var res = client.Execute(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {res.Content}");
            Assert.That(GetMessage(res.Content) ?? res.Content ?? "",
                        Does.Contain("Successfully created!"),
                        $"Body: {res.Content}");
        }

        // 2) GET ALL -> 200, взимаме id на току-що създадената по уникално title
        [Test, Order(2)]
        public void GetAllRevues_ShouldReturn200_AndStoreCreatedId()
        {
            var req = new RestRequest("api/Revue/All", Method.Get)
                .AddHeader("Authorization", $"Bearer {AccessToken}");

            var res = client.Execute(req);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {res.Content}");

            using var doc = JsonDocument.Parse(res.Content ?? "[]");
            Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));

            var match = doc.RootElement.EnumerateArray()
                .FirstOrDefault(e =>
                    (TryGetString(e, "title") ?? TryGetString(e, "Title"))
                    ?.Equals(createdTitle, StringComparison.Ordinal) == true);

            createdRevueId = TryGetString(match, "revueId")
                          ?? TryGetString(match, "RevueId")
                          ?? TryGetString(match, "id");

            Assert.That(createdRevueId, Is.Not.Null.And.Not.Empty, "Не намерих revueId от /All.");
        }

        // 3) EDIT -> 200 + "Edited successfully"
        [Test, Order(3)]
        public void EditRevue_ShouldReturn200_AndEditedSuccessfully()
        {
            Assume.That(!string.IsNullOrEmpty(createdRevueId), "Няма записан revueId от предния тест.");

            var req = new RestRequest("api/Revue/Edit", Method.Put)
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddHeader("Content-Type", "application/json-patch+json") // според Swagger
                .AddQueryParameter("revueId", createdRevueId)
                .AddJsonBody(new { title = createdTitle + " - edited", url = "", description = "Edited via NUnit" });

            var res = client.Execute(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {res.Content}");
            Assert.That(GetMessage(res.Content) ?? res.Content ?? "",
                        Does.Contain("Edited successfully"),
                        $"Body: {res.Content}");
        }

        // 4) DELETE -> 200 + "The revue is deleted!"
        [Test, Order(4)]
        public void DeleteRevue_ShouldReturn200_AndDeletedMessage()
        {
            Assume.That(!string.IsNullOrEmpty(createdRevueId), "Няма записан revueId от предния тест.");

            var req = new RestRequest("api/Revue/Delete", Method.Delete)
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddQueryParameter("revueId", createdRevueId);

            var res = client.Execute(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {res.Content}");
            Assert.That(GetMessage(res.Content) ?? res.Content ?? "",
                        Does.Contain("The revue is deleted!"),
                        $"Body: {res.Content}");
        }

        // 5) CREATE без задължителни полета -> 400 BadRequest
        [Test, Order(5)]
        public void CreateRevue_WithoutRequiredFields_ShouldReturn400()
        {
            var body = new { title = (string)null, url = "", description = (string)null };

            var req = new RestRequest("api/Revue/Create", Method.Post)
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddJsonBody(body);

            var res = client.Execute(req);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Body: {res.Content}");
        }

        // 6) EDIT несъществуваща -> 400 + "There is no such revue!"
        [Test, Order(6)]
        public void EditNonExistingRevue_ShouldReturnClientError_AndNoSuchRevue()
        {
            var nonExistingId = Guid.NewGuid().ToString(); // валиден, но несъществуващ GUID

            var req = new RestRequest("api/Revue/Edit", Method.Put);
            req.AddHeader("Authorization", $"Bearer {AccessToken}");
            req.AddHeader("Content-Type", "application/json-patch+json"); // по Swagger
            req.AddQueryParameter("revueId", nonExistingId);             // точното име на query параметъра
            req.AddJsonBody(new { title = "x", url = "", description = "x" }); // lowercase ключове

            var res = client.Execute(req);

            // Някои инстанции връщат 400, други 404 – приемаме всеки 4xx
            Assert.That(res.StatusCode,
                Is.EqualTo(HttpStatusCode.BadRequest).Or.EqualTo(HttpStatusCode.NotFound),
                $"Expected 400 or 404. Got {(int)res.StatusCode} {res.StatusCode}. Body: {res.Content}");

            // Съобщение: "There is no such revue!" (или подобно); правим case-insensitive проверка
            string msg = null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(res.Content ?? "");
                if (doc.RootElement.TryGetProperty("message", out var m)) msg = m.GetString();
                else if (doc.RootElement.TryGetProperty("msg", out var mm)) msg = mm.GetString();
            }
            catch { /* not JSON */ }

            var text = msg ?? res.Content ?? string.Empty;
            Assert.That(text, Does.Contain("There is no such revue").IgnoreCase
                             .Or.Contain("not found").IgnoreCase,
                        $"Body: {res.Content}");
        }

        // 7) DELETE несъществуваща -> 400 + "There is no such revue!"
        [Test, Order(7)]
        public void DeleteNonExistingRevue_ShouldReturn400_AndNoSuchRevue()
        {
            var nonExistingId = Guid.NewGuid().ToString();

            var req = new RestRequest("api/Revue/Delete", Method.Delete)
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddQueryParameter("revueId", nonExistingId);

            var res = client.Execute(req);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Body: {res.Content}");
            Assert.That(GetMessage(res.Content) ?? res.Content ?? "",
                        Does.Contain("There is no such revue!"),
                        $"Body: {res.Content}");
        }

        // --------------- helpers ---------------
        private static string TryGetString(JsonElement e, string name)
            => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
               ? p.GetString() : null;

        private static string GetMessage(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content ?? "");
                if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
                if (doc.RootElement.TryGetProperty("msg", out var mm)) return mm.GetString();
            }
            catch { /* not JSON */ }
            return null;
        }
    }
}