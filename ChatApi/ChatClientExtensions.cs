using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using OllamaSharp;

public static class ChatClientExtensions
{
    public static IHostApplicationBuilder AddChatClient(this IHostApplicationBuilder builder, string connectionName)
    {
        var cs = builder.Configuration.GetConnectionString(connectionName);

        if (!ChatClientConnectionInfo.TryParse(cs, out var connectionInfo))
        {
            throw new InvalidOperationException($"Invalid connection string: {cs}");
        }

        var httpKey = connectionName + "_http";

        builder.Services.AddHttpClient(httpKey, c =>
        {
            c.BaseAddress = connectionInfo.Endpoint;
        });

        builder.Services.AddChatClient(sp =>
        {
            if (connectionInfo.AccessKey is null && builder.Environment.IsDevelopment())
            {
                // Create a client for the Ollama API using the http client factory
                var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpKey);

                return new OllamaApiClient(client, connectionInfo.SelectedModel);
            }
            else
            {
                // Azure open ai in when not in development
                var key = connectionInfo.AccessKey ?? throw new InvalidOperationException("An access key is required for the azure ai inference sdk.");

                return new ChatCompletionsClient(connectionInfo.Endpoint, new AzureKeyCredential(key)).AsChatClient(connectionInfo.SelectedModel);
            }
        })
        .UseOpenTelemetry()
        .UseLogging();

        // Add OpenTelemetry tracing for the Microsoft.Extensions.AI activity source
        builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource("Experimental.Microsoft.Extensions.AI"));

        return builder;
    }
}
