var builder = DistributedApplication.CreateBuilder(args);

// This is the AI model our application will use
var model = builder.AddAIModel("llm")
                   .RunAsOllama("phi4", c =>
                   {
                       // Enable to enable GPU support (if your machine has a GPU)
                       c.WithGPUSupport();
                       c.WithLifetime(ContainerLifetime.Persistent);
                   })
                   .PublishAsAzureOpenAI("gpt-4o", "2024-05-13");

// We use Cosmos DB for our conversation history
var conversations = builder.AddAzureCosmosDB("cosmos")
                           .RunAsPreviewEmulator(e => e.WithDataExplorer().WithDataVolume())
                           .AddCosmosDatabase("db")
                           .AddContainer("conversations", "/id");

var chatapi = builder.AddProject<Projects.ChatApi>("chatapi")
                     .WithReference(model)
                     .WaitFor(model)
                     .WithReference(conversations)
                     .WaitFor(conversations)
                     .PublishAsAzureContainerApp((infra, app) =>
                      {
                          app.Configuration.Ingress.AllowInsecure = true;
                      });

builder.AddDockerfile("chatui", "../chatui")
       .WithHttpEndpoint(env: "PORT")
       .WithReverseProxy(chatapi.GetEndpoint("http"))
       .WithExternalHttpEndpoints()
       .WithOtlpExporter();

builder.Build().Run();
