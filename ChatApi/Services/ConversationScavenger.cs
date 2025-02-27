using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

public class ConversationScavenger(
    IConversationState conversationState,
    IServiceScopeFactory scopeFactory,
    ILogger<ConversationScavenger> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(ProcessIncompleteConversations);

        async Task ProcessIncompleteConversations()
        {
            try
            {
                // Get the db context from the scope factory
                using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Get all incomplete conversations
                var incomplete = await db.Conversations.Where(c => c.IsWaitingForResponse == true).ToListAsync(cancellationToken);

                logger.LogInformation("Found {Count} incomplete conversations", incomplete.Count);

                foreach (var c in incomplete)
                {
                    var messages = await conversationState.GetUnpublishedMessagesAsync(c.Id);

                    foreach (var m in messages)
                    {
                        var finalFragment = new ClientMessageFragment(m.Id, ChatRole.Assistant.Value, "", Guid.CreateVersion7(), IsFinal: true);

                        await conversationState.PublishFragmentAsync(c.Id, finalFragment);

                        await conversationState.CompleteAsync(c.Id, m.Id);

                        c.Messages.Add(new ConversationChatMessage
                        {
                            Id = Guid.CreateVersion7(),
                            Role = ChatRole.Assistant.Value,
                            Text = m.Text,
                        });
                    }
                }

                await db.SaveChangesAsync();

                logger.LogInformation("Processed incomplete conversations");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing incomplete conversations");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
