using Microsoft.Extensions.AI;
using OllamaSharp;

public static class ChatClientExtensions
{
    public static IHostApplicationBuilder AddChatClient(this IHostApplicationBuilder builder, string connectionName)
    {
        ChatClientBuilder chatClientBuilder;

        if (builder.Environment.IsDevelopment())
        {
            var cs = builder.Configuration.GetConnectionString(connectionName);

            if (!ChatClientConnectionInfo.TryParse(cs, out var connectionInfo))
            {
                throw new InvalidOperationException($"Invalid connection string: {cs}");
            }

            var httpKey = $"{connectionName}_http";

            builder.Services.AddHttpClient(httpKey, c =>
            {
                c.BaseAddress = connectionInfo.Endpoint;
            });

            chatClientBuilder = builder.Services.AddChatClient(sp =>
            {
                // Create a client for the Ollama API using the http client factory
                var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpKey);

                return new OllamaApiClient(client, connectionInfo.SelectedModel);
            });
        }
        else
        {
            chatClientBuilder = builder.AddAzureOpenAIClient(connectionName).AddChatClient("gpt-4o");
        }

        // Add OpenTelemetry tracing for the ChatClient activity source
        chatClientBuilder.UseOpenTelemetry().UseLogging();

        builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource("Experimental.Microsoft.Extensions.AI"));

        return builder;
    }
}
