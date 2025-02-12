
using Microsoft.Extensions.DependencyInjection;

public static class ModelExtensions
{
    public static IResourceBuilder<LLMResource> AddModel(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new LLMResource(name);

        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            // TODO: Put some logic in there
            var rns = e.Services.GetRequiredService<ResourceNotificationService>();

            _ = Task.Run(async () =>
            {
                var endpoint = await resource.Endpoint!.GetValueAsync(default);
                var accessKey = await resource.AccessKey!.GetValueAsync(default);

                await rns.PublishUpdateAsync(resource, s => s with
                {
                    Properties = [
                        new ResourcePropertySnapshot(CustomResourceKnownProperties.Source, resource.ModelName),
                        new ResourcePropertySnapshot("Endpoint", endpoint),
                        new ResourcePropertySnapshot("AccessKey", accessKey) { IsSensitive = true }
                    ]
                });

                await builder.Eventing.PublishAsync(new ConnectionStringAvailableEvent(resource, e.Services));
            });

            return Task.CompletedTask;
        });

        return builder.AddResource(resource).WithInitialState(new CustomResourceSnapshot
        {
            Properties = [],
            ResourceType = "LLM",
            State = KnownResourceStates.Running
        });
    }

    public static IResourceBuilder<LLMResource> WithEndpoint(this IResourceBuilder<LLMResource> builder, IResourceBuilder<ParameterResource> endpoint)
    {
        builder.Resource.Endpoint = ReferenceExpression.Create($"{endpoint.Resource}");
        return builder;
    }

    public static IResourceBuilder<LLMResource> WithEndpoint(this IResourceBuilder<LLMResource> builder, ReferenceExpression endpoint)
    {
        builder.Resource.Endpoint = endpoint;
        return builder;
    }

    public static IResourceBuilder<LLMResource> WithModelName(this IResourceBuilder<LLMResource> builder, string modelName)
    {
        builder.Resource.ModelName = modelName;
        return builder;
    }

    public static IResourceBuilder<LLMResource> WithAccessKey(this IResourceBuilder<LLMResource> builder, IResourceBuilder<ParameterResource> accessKey)
    {
        builder.Resource.AccessKey = ReferenceExpression.Create($"{accessKey.Resource}");
        return builder;
    }

    public static IResourceBuilder<IResourceWithConnectionString> AsConnectionString(this IResourceBuilder<LLMResource> builder)
     => builder;
}

public class LLMResource(string name) : Resource(name), IResourceWithConnectionString
{
    public ReferenceExpression? Endpoint { get; set; }
    public string? ModelName { get; set; }
    public ReferenceExpression? AccessKey { get; set; }

    public ReferenceExpression ConnectionStringExpression => Build();

    private ReferenceExpression Build()
    {
        var builder = new ReferenceExpressionBuilder();

        if (Endpoint is null)
        {
            throw new InvalidOperationException("Endpoint is required");
        }

        builder.Append($"Endpoint={Endpoint};");

        if (ModelName is null)
        {
            throw new InvalidOperationException("ModelName is required");
        }

        builder.Append($"Model={ModelName}");

        if (AccessKey is not null)
        {
            builder.Append($";AccessKey={AccessKey}");
        }

        return builder.Build();
    }
}