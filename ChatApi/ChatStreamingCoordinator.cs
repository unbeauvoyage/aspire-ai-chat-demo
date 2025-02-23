using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.AI;

public class ChatStreamingCoordinator(
    IChatClient chatClient,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatStreamingCoordinator> logger,
    IConversationState conversationState)
{
    private readonly IConversationState _conversationState = conversationState;

    public async Task AddStreamingMessage(Guid conversationId, Guid assistantReplyId, List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding streaming message for conversation {ConversationId}", conversationId);
        await StreamMessages();

        async Task StreamMessages()
        {
            var allChunks = new List<ChatResponseUpdate>();

            var fragment = new ClientMessageFragment(assistantReplyId, "Generating reply...", Guid.CreateVersion7());
            await _conversationState.PublishFragmentAsync(conversationId, fragment);

            try
            {
                await foreach (var update in chatClient.GetStreamingResponseAsync(messages).WithCancellation(cancellationToken))
                {
                    if (update.Text is not null)
                    {
                        allChunks.Add(update);
                        fragment = new ClientMessageFragment(assistantReplyId, update.Text, Guid.CreateVersion7());
                        await _conversationState.PublishFragmentAsync(conversationId, fragment);
                    }
                }

                logger.LogInformation("Full message received for conversation {ConversationId}", conversationId);

                if (allChunks.Count > 0)
                {
                    var fullMessage = allChunks.ToChatResponse().Message;
                    await SaveAssistantMessageToDatabase(conversationId, assistantReplyId, fullMessage.Text!);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fragment = new ClientMessageFragment(assistantReplyId, "Error streaming message", Guid.CreateVersion7());
                await _conversationState.PublishFragmentAsync(conversationId, fragment);
                logger.LogError(ex, "Error streaming message for conversation {ConversationId}", conversationId);

                await SaveAssistantMessageToDatabase(conversationId, assistantReplyId, "Error streaming message");
            }
        }
    }

    private async Task SaveAssistantMessageToDatabase(Guid conversationId, Guid messageId, string text)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation is not null)
        {
            conversation.Messages.Add(new ConversationChatMessage
            {
                Id = messageId,
                Role = ChatRole.Assistant.Value,
                Text = text
            });

            await db.SaveChangesAsync();
        }

        await _conversationState.CompleteAsync(conversationId, messageId);
    }

    public async IAsyncEnumerable<ClientMessageFragment> GetMessageStream(
        Guid conversationId,
        Guid? lastMessageId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting message stream for conversation {ConversationId}, {LastMessageId}", conversationId, lastMessageId);
        var stream = _conversationState.Subscribe(conversationId, lastMessageId, cancellationToken);

        var lastDeliveredFragment = Guid.Empty;

        await foreach (var fragment in stream.WithCancellation(cancellationToken))
        {
            // Use lastMessageId to filter out fragments from an already delivered message,
            // while using lastDeliveredFragment (a sortable GUID) for ordering and de-duping.
            if (fragment.FragmentId > lastDeliveredFragment)
            {
                lastDeliveredFragment = fragment.FragmentId;
            }
            else
            {
                continue;
            }

            yield return fragment;
        }
    }
}
