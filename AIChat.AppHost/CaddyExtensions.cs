public static class CaddyExtensions
{
    public static IResourceBuilder<ContainerResource> WithCaddyUpstream(this IResourceBuilder<ContainerResource> builder, EndpointReference upstreamEndpoint)
    {
        return builder.WithEnvironment(c =>
        {
            // In the docker file, caddy uses the host and port without the scheme
            var hostAndPort = ReferenceExpression.Create($"{upstreamEndpoint.Property(EndpointProperty.Host)}:{upstreamEndpoint.Property(EndpointProperty.Port)}");

            c.EnvironmentVariables["BACKEND_URL"] = hostAndPort;
            c.EnvironmentVariables["SPAN"] = builder.Resource.Name;
        });
    }
}