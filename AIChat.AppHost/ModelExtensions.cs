
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

        // At publish time, we're going to use Azure OpenAI with gpt-4o
        return builder.AddAzureOpenAI(name).AddDeployment(new("gpt-4o", "gpt-4o", "2024-05-13"));
    }
}
