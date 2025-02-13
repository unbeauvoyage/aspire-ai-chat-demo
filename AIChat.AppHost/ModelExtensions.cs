
public static class ModelExtensions
{
    public static IResourceBuilder<IResourceWithConnectionString> AddAIModel(this IDistributedApplicationBuilder builder, string name)
    {
        if (builder.ExecutionContext.IsRunMode)
        {
            // In run mode, we're going to use ollama (hard code phi4 for this application)
            return builder.AddOllama("ollama")
                .WithGPUSupport()
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .AddModel(name, "phi4");
        }

        return builder.AddConnectionString(name);
    }
}
