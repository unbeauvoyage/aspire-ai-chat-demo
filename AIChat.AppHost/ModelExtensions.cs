
public static class ModelExtensions
{
    public static IResourceBuilder<AIModel> AddAIModel(this IDistributedApplicationBuilder builder, string name)
    {
        var model = new AIModel(name);
        return builder.CreateResourceBuilder(model);
    }

    public static IResourceBuilder<AIModel> RunAsOllama(this IResourceBuilder<AIModel> builder, string model)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            if (builder.Resource.InnerResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.InnerResource);
            }

            var ollamaModel = builder.ApplicationBuilder.AddOllama("ollama")
                .WithGPUSupport()
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .AddModel(builder.Resource.Name, model);

            ollamaModel.WithParentRelationship(builder.Resource);

            builder.Resource.InnerResource = ollamaModel.Resource;
        }

        return builder;
    }

    public static IResourceBuilder<AIModel> PublishAsAzureOpenAI(this IResourceBuilder<AIModel> builder, string modelName, string modelVersion)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            if (builder.Resource.InnerResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.InnerResource);
            }

            var openAIModel = builder.ApplicationBuilder.AddAzureOpenAI(builder.Resource.Name)
                .AddDeployment(new(modelName, modelName, modelVersion));

            builder.Resource.InnerResource = openAIModel.Resource;
        }

        return builder;
    }
}

// A resource representing an AI model.
public class AIModel(string name) : Resource(name), IResourceWithConnectionString
{
    internal IResourceWithConnectionString? InnerResource { get; set; }

    public ReferenceExpression ConnectionStringExpression =>
        InnerResource?.ConnectionStringExpression
        ?? throw new InvalidOperationException("No connection string available.");
}
