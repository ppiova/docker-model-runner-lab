extern alias ComposeApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;

namespace Chat.Tests;

internal sealed class ApiFactory(ChatClient chat) : WebApplicationFactory<ComposeApi::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.RemoveAll<ChatClient>();
        services.AddSingleton(chat);
    });
}
