var builder = DistributedApplication.CreateBuilder(args);

// This is the AI model our application will use
var model = builder.AddAIModel("llm");

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
       .WithHttpEndpoint(targetPort: 80, env: "PORT")
       .WithCaddyUpstream(chatapi.GetEndpoint("http"))
       .WithExternalHttpEndpoints()
       .WithOtlpExporter();

builder.Build().Run();
