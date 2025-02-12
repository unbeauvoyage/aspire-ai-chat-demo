var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> model = builder.ExecutionContext.IsPublishMode
? builder.AddModel("llm")
         .WithEndpoint(builder.AddParameter("modelep"))
         .WithModelName("Phi-4")
         .WithAccessKey(builder.AddParameter("modelkey", secret: true))
: builder.AddOllama("ollama")
         .WithGPUSupport()
         .WithDataVolume()
         .WithLifetime(ContainerLifetime.Persistent)
         .AddModel("llm", "phi4");

var backend = builder.AddProject<Projects.ChatApi>("chatapi")
       .WithReference(model)
       .WaitFor(model)
       .PublishAsAzureContainerApp((infra, app) =>
        {
            app.Configuration.Ingress.AllowInsecure = true;
        });

builder.AddDockerfile("chatui", "../chatui")
       .WithHttpEndpoint(targetPort: 80, env: "PORT")
       .WithEnvironment(c =>
       {
           var be = backend.GetEndpoint("http");

           // In the docker file, caddy uses the host and port without the scheme
           var hostAndPort = ReferenceExpression.Create($"{be.Property(EndpointProperty.Host)}:{be.Property(EndpointProperty.Port)}");

           c.EnvironmentVariables["BACKEND_URL"] = hostAndPort;
           c.EnvironmentVariables["SPAN"] = "chatui";
       })
        .WithExternalHttpEndpoints()
        .WithOtlpExporter();

builder.Build().Run();
