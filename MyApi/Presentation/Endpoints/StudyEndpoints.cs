using Microsoft.AspNetCore.Mvc;

namespace MyApi;

public static class StudyEndpoints
{
    public static void RegisterStudyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/study").WithOpenApi();

        group.MapPost("/start", (
            [FromServices] MyApi.StudyService study,
            [FromBody] MyApi.StartStudyRequest request) =>
        {
            var session = study.Start(request.Topic, request.Level, request.Exam);
            return Results.Ok(session);
        })
        .WithSummary("Start a study session");

        group.MapPost("/send", async (
            [FromServices] MyApi.StudyService study,
            [FromBody] MyApi.SendMessageRequest request,
            CancellationToken ct) =>
        {
            var session = await study.SendAsync(request.SessionId, request.Message, ct);
            return Results.Ok(session);
        })
        .WithSummary("Send a message and get assistant reply");
    }
}


