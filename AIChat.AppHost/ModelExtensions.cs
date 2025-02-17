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
            if (builder.Resource.UnderlyingResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.UnderlyingResource);
            }

            var ollama = builder.ApplicationBuilder.AddOllama("ollama")
                .WithDataVolume();

            configure?.Invoke(ollama);

            var ollamaModel = ollama.AddModel(builder.Resource.Name, model);

            builder.Resource.UnderlyingResource = ollamaModel.Resource;
            builder.Resource.ConnectionString = ollamaModel.Resource.ConnectionStringExpression;
            builder.Resource.Provider = "Ollama";
        }

        return builder;
    }

    public static IResourceBuilder<AIModel> RunAsOpenAI(this IResourceBuilder<AIModel> builder, string modelName, IResourceBuilder<ParameterResource> apiKey)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            if (builder.Resource.UnderlyingResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.UnderlyingResource);
            }

            // See: https://github.com/dotnet/aspire/issues/7641
            var csb = new ReferenceExpressionBuilder();
            csb.Append($"Endpoint=https://api.openai.com/v1;");
            csb.Append($"AccessKey={apiKey.Resource};");
            csb.Append($"Model={modelName}");
            var cs = csb.Build();

            var csTask = cs.GetValueAsync(default).AsTask();
            if (!csTask.IsCompletedSuccessfully) throw new InvalidOperationException("Connection string could not be resolved!");

            builder.ApplicationBuilder.AddResource(builder.Resource)
                                      .WithInitialState(new CustomResourceSnapshot
                                      {
                                          ResourceType = "OpenAI Model",
                                          State = KnownResourceStates.Running,
                                          Properties = [
                                            new("ConnectionString", csTask.Result ) { IsSensitive = true }
                                          ]
                                      });

            builder.Resource.UnderlyingResource = builder.Resource;
            builder.Resource.ConnectionString = cs;
            builder.Resource.Provider = "OpenAI";
        }

        return builder;
    }

    public static IResourceBuilder<AIModel> PublishAsAzureOpenAI(this IResourceBuilder<AIModel> builder, string modelName, string modelVersion, Action<IResourceBuilder<AzureOpenAIResource>>? configure = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            if (builder.Resource.UnderlyingResource is not null)
            {
                builder.ApplicationBuilder.Resources.Remove(builder.Resource.UnderlyingResource);
            }

            var openAIModel = builder.ApplicationBuilder.AddAzureOpenAI(builder.Resource.Name)
                .AddDeployment(new(modelName, modelName, modelVersion));

            configure?.Invoke(openAIModel);

            builder.Resource.UnderlyingResource = openAIModel.Resource;
            builder.Resource.ConnectionString = openAIModel.Resource.ConnectionStringExpression;
            builder.Resource.Provider = "AzureOpenAI";
        }

        return builder;
    }
}

// A resource representing an AI model.
public class AIModel(string name) : Resource(name), IResourceWithConnectionString
{
    internal string? Provider { get; set; }
    internal IResourceWithConnectionString? UnderlyingResource { get; set; }
    internal ReferenceExpression? ConnectionString { get; set; }

    public ReferenceExpression ConnectionStringExpression =>
        Build();

    public ReferenceExpression Build()
    {
        var connectionString = ConnectionString ?? throw new InvalidOperationException("No connection string available.");

        if (Provider is null)
        {
            throw new InvalidOperationException("No provider available.");
        }

        return ReferenceExpression.Create($"{connectionString};Provider={Provider}");
    }
}

