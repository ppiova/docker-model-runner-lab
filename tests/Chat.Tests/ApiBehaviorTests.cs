using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Chat.Tests;

public class ApiBehaviorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"prompt\":null}")]
    [InlineData("{\"prompt\":\"\"}")]
    [InlineData("{\"prompt\":\"  \\n\\t\"}")]
    [InlineData("{\"prompt\":123}")]
    [InlineData("{")]
    public async Task Invalid_input_returns_400_without_contacting_model(string body)
    {
        using var model = new TestModel(_ => throw new InvalidOperationException("Must not call model"));
        await using var app = new ApiFactory(model.CreateClient());
        using var client = app.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/chat", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(model.Requests);
    }

    [Theory]
    [InlineData("Respuesta válida 🐳")]
    [InlineData("")]
    public async Task Successful_request_forwards_prompt_and_returns_model_reply(string reply)
    {
        using var model = new TestModel(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = "test", @object = "chat.completion", created = 0, model = "test-model",
                choices = new[] { new { index = 0, message = new { role = "assistant", content = reply }, finish_reason = "stop" } }
            })
        }));
        await using var app = new ApiFactory(model.CreateClient());
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/chat", new { prompt = "¿Qué es Docker?" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(reply, body.GetProperty("reply").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("model").GetString()));
        Assert.Equal(new[] { "¿Qué es Docker?" }, TestModel.Contents(Assert.Single(model.Requests)));
    }

    [Fact]
    public async Task Connection_failure_returns_problem_details()
    {
        using var model = new TestModel(_ => throw new HttpRequestException("Connection refused"));
        await using var app = new ApiFactory(model.CreateClient());
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/chat", new { prompt = "Hello" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(502, problem.GetProperty("status").GetInt32());
        Assert.Equal("Model request failed", problem.GetProperty("title").GetString());
        Assert.Single(model.Requests);
    }
}
