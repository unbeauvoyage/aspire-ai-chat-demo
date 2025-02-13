
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
            var ollamaModel = builder.ApplicationBuilder.AddOllama("ollama")
                .WithGPUSupport()
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .AddModel(builder.Resource.Name, model);

            ollamaModel.WithParentRelationship(builder.Resource);

            builder.Resource.OllamaModelResource = ollamaModel.Resource;
        }

        return builder;
    }

    public static IResourceBuilder<AIModel> PublishAsAzureOpenAI(this IResourceBuilder<AIModel> builder, string modelName, string modelVersion)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var openAIModel = builder.ApplicationBuilder.AddAzureOpenAI(builder.Resource.Name)
                .AddDeployment(new(modelName, modelName, modelVersion));

            builder.Resource.AzureOpenAIModelResource = openAIModel.Resource;
        }

        return builder;
    }

    public class AIModel(string name) : Resource(name), IResourceWithConnectionString
    {
        internal OllamaModelResource? OllamaModelResource { get; set; }

        internal AzureOpenAIResource? AzureOpenAIModelResource { get; set; }

        public ReferenceExpression ConnectionStringExpression =>
            OllamaModelResource?.ConnectionStringExpression
            ?? AzureOpenAIModelResource?.ConnectionStringExpression
            ?? throw new InvalidOperationException("No connection string available.");
    }
}
