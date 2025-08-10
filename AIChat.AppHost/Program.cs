var builder = DistributedApplication.CreateBuilder(args);

// Publish this as a Docker Compose application
builder.AddDockerComposeEnvironment("env")
       .WithDashboard(db => db.WithHostPort(8085))
       .ConfigureComposeFile(file =>
       {
           file.Name = "aspire-ai-chat";
       });

// This is the AI model our application will use
var model = builder.AddAIModel("llm");

if (OperatingSystem.IsMacOS())
{
    // Just use OpenAI on MacOS, running ollama does not work well via docker
    // see https://github.com/CommunityToolkit/Aspire/issues/608
    model.AsOpenAI("gpt-4.1");
}
else
{
    model.RunAsOllama("phi4", c =>
    {
        // Enable to enable GPU support (if your machine has a GPU)
        c.WithGPUSupport();
        c.WithLifetime(ContainerLifetime.Persistent);
    })
    .PublishAsOpenAI("gpt-4.1");
}

// Postgres server (multiple logical databases: conversations + weather)
var pg = builder.AddPostgres("pg")
                .WithDataVolume(builder.ExecutionContext.IsPublishMode ? "pgvolume" : null)
                .WithPgAdmin();
var conversationsDb = pg.AddDatabase("conversations");
var weatherDb = pg.AddDatabase("weather");

// Redis is used to store and broadcast the live message stream
// so that multiple clients can connect to the same conversation.
var cache = builder.AddRedis("cache")
                   .WithRedisInsight();

var chatapi = builder.AddProject<Projects.ChatApi>("chatapi")
                     .WithReference(model)
                     .WaitFor(model)
                     .WithReference(conversationsDb)
                     .WaitFor(conversationsDb)
                     .WithReference(cache)
                     .WaitFor(cache);

// --- YOUR CUSTOM API ---
var myapi = builder.AddProject<Projects.MyApi>("myapi")
                   .WithReference(model)
                   .WaitFor(model)
                   .WithReference(weatherDb)
                   .WaitFor(weatherDb)
                   .WithExternalHttpEndpoints();

builder.AddNpmApp("chatui", "../chatui")
       .WithNpmPackageInstallation()
       .WithHttpEndpoint(env: "PORT")
       .WithReverseProxy(chatapi.GetEndpoint("http"))
       .WithExternalHttpEndpoints()
       .WithOtlpExporter()
       .WithEnvironment("BROWSER", "none");

// --- YOUR CUSTOM NEXT.JS APP ---
builder.AddNpmApp("nextapp", "../nextapp")
       .WithNpmPackageInstallation()
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints()
       .WithEnvironment("NEXT_PUBLIC_API_BASE", myapi.GetEndpoint("http"))
       .WithReference(myapi)
       .WithOtlpExporter()
       .WithEnvironment("BROWSER", "none");

builder.Build().Run();

