public static class SseExtensions
{
    public static SseResult SseStream(this IResultExtensions _, Func<CancellationToken, IAsyncEnumerable<string?>> factory) =>
        new(factory);
}

public class SseResult(Func<CancellationToken, IAsyncEnumerable<string?>> factory) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");

        await foreach (var message in factory(context.RequestAborted))
        {
            if (message is not null)
            {
                var escapedMessage = EscapeSseData(message);
                await context.Response.WriteAsync($"data: {escapedMessage}\n\n");
                await context.Response.Body.FlushAsync();
            }
        }
    }

    private static string EscapeSseData(string data) =>
        data.Replace("\n", "\\n"); // Escape newlines for SSE data
}
