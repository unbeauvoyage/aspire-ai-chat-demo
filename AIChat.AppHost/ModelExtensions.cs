
using Microsoft.Extensions.DependencyInjection;

public static class ModelExtensions
{
    public static IResourceBuilder<AIModel> AddAIModel(this IDistributedApplicationBuilder builder, string name)
    {
        var model = new AIModel(name);
        return builder.CreateResourceBuilder(model);
    }

    public static IResourceBuilder<AIModel> RunAsOllama(this IResourceBuilder<AIModel> builder, string model, Action<IResourceBuilder<OllamaResource>>? configure = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            if (builder.Resource.InnerResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.InnerResource);
            }

            var ollama = builder.ApplicationBuilder.AddOllama("ollama")
                .WithGPUSupport()
                .WithDataVolume();

            configure?.Invoke(ollama);

            var ollamaModel = ollama.AddModel(builder.Resource.Name, model);

            builder.Resource.InnerResource = ollamaModel.Resource;
        }

        return builder;
    }

    public static IResourceBuilder<AIModel> PublishAsAzureOpenAI(this IResourceBuilder<AIModel> builder, string modelName, string modelVersion, Action<IResourceBuilder<AzureOpenAIResource>>? configure = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            if (builder.Resource.InnerResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.InnerResource);
            }

            var openAIModel = builder.ApplicationBuilder.AddAzureOpenAI(builder.Resource.Name)
                .AddDeployment(new(modelName, modelName, modelVersion));

            configure?.Invoke(openAIModel);

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
