public interface ICancellationManager
{
    CancellationToken GetCancellationToken(Guid assistantReplyId);
    
    Task CancelAsync(Guid assistantReplyId);
}
