public static class ProxyExtensions
{
    public static IResourceBuilder<NodeAppResource> WithReverseProxy(this IResourceBuilder<NodeAppResource> builder, EndpointReference upstreamEndpoint)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder.WithEnvironment("BACKEND_URL", upstreamEndpoint);
        }

        return builder.PublishAsDockerFile(c => c.WithCaddyReverseProxy(upstreamEndpoint));
    }

    public static IResourceBuilder<ContainerResource> WithCaddyReverseProxy(this IResourceBuilder<ContainerResource> builder, EndpointReference upstreamEndpoint)
    {
        // Caddy listens on port 80
        builder.WithEndpoint("http", e => e.TargetPort = 80);

        return builder.WithEnvironment(context =>
        {
            // In the docker file, caddy uses the host and port without the scheme
            var hostAndPort = ReferenceExpression.Create($"{upstreamEndpoint.Property(EndpointProperty.Host)}:{upstreamEndpoint.Property(EndpointProperty.Port)}");

            context.EnvironmentVariables["BACKEND_URL"] = hostAndPort;
            context.EnvironmentVariables["SPAN"] = builder.Resource.Name;
        });
    }
}