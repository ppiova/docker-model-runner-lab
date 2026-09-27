using System.Net;
using System.Net.Http.Json;

namespace Chat.Tests;

public class ApiCancellationTests
{
    [Fact]
    public async Task Aborting_http_request_cancels_the_upstream_model_call()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var model = new TestModel(async token =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                stopped.TrySetResult();
                throw;
            }
            throw new InvalidOperationException("Expected cancellation");
        });
        await using var app = new ApiFactory(model.CreateClient());
        using var client = app.CreateClient();
        using var cancel = new CancellationTokenSource();
        var request = client.PostAsJsonAsync("/chat", new { prompt = "Hello" }, cancel.Token);
        await started.Task.WaitAsync(TestModel.Timeout);
        cancel.Cancel();

        await stopped.Task.WaitAsync(TestModel.Timeout);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await request.WaitAsync(TestModel.Timeout));
    }

    [Fact]
    public async Task Upstream_failure_still_returns_bad_gateway()
    {
        using var model = new TestModel(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        { Content = new StringContent("{\"error\":{\"message\":\"Model failed\",\"type\":\"server_error\"}}") }));
        await using var app = new ApiFactory(model.CreateClient());
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/chat", new { prompt = "Hello" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

}
