using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace MyApi;

public static class TutorEndpoints
{
    public static void RegisterTutorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();

        group.MapPost("/goals", async (
            [FromServices] AppDbContext db,
            [FromServices] Kernel kernel,
            [FromBody] CreateGoalRequest req,
            CancellationToken ct) =>
        {
            // use single demo student
            var student = await db.Students.FirstOrDefaultAsync(ct) ?? new Student();
            if (student.Id == 0) db.Students.Add(student);
            var goal = new StudyGoal { StudentId = student.Id, Description = req.Description };
            db.StudyGoals.Add(goal);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { goalId = goal.Id });
        });

        group.MapPost("/concepts/{goalId:int}", async (
            [FromServices] AppDbContext db,
            [FromServices] Kernel kernel,
            int goalId,
            CancellationToken ct) =>
        {
            var goal = await db.StudyGoals.FindAsync(new object?[] { goalId }, ct);
            if (goal is null) return Results.NotFound();

            var prompt = $"List 5 key concepts to master for: '{goal.Description}'. Return compact JSON array of objects with title and content fields.";
            var json = await kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct) ?? "[]";
            try
            {
                var items = JsonSerializer.Deserialize<List<ConceptGen>>(json) ?? new();
                foreach (var it in items)
                {
                    db.Concepts.Add(new Concept { GoalId = goal.Id, Title = it.title ?? "Concept", Content = it.content ?? string.Empty });
                }
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                // fallback: naive split lines
                db.Concepts.Add(new Concept { GoalId = goal.Id, Title = "Overview", Content = json });
                await db.SaveChangesAsync(ct);
            }
            var concepts = await db.Concepts.Where(c => c.GoalId == goal.Id).ToListAsync(ct);
            return Results.Ok(concepts);
        });

        group.MapPost("/quiz/{conceptId:int}", async (
            [FromServices] AppDbContext db,
            [FromServices] Kernel kernel,
            int conceptId,
            CancellationToken ct) =>
        {
            var concept = await db.Concepts.FindAsync(new object?[] { conceptId }, ct);
            if (concept is null) return Results.NotFound();
            var prompt = $"Create 3 short Q&A pairs to test understanding of: '{concept.Title}'. Include fields question and answer. Return JSON array.";
            var json = await kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct) ?? "[]";
            try
            {
                var items = JsonSerializer.Deserialize<List<QuizGen>>(json) ?? new();
                foreach (var q in items)
                {
                    db.QuizQuestions.Add(new QuizQuestion { ConceptId = concept.Id, Question = q.question ?? string.Empty, Answer = q.answer ?? string.Empty });
                }
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                db.QuizQuestions.Add(new QuizQuestion { ConceptId = concept.Id, Question = "Explain:", Answer = json });
                await db.SaveChangesAsync(ct);
            }
            var quiz = await db.QuizQuestions.Where(q => q.ConceptId == concept.Id).ToListAsync(ct);
            return Results.Ok(quiz);
        });
    }

    private record CreateGoalRequest(string Description);
    private record ConceptGen(string? title, string? content);
    private record QuizGen(string? question, string? answer);
}


