namespace OIO.Infrastructure.Outbox;

internal static class OutboxConstants
{
    /// <summary>
    /// The maximum number of processing attempts before a message is considered "poison".
    /// This value is hardcoded in the partial index <c>idx_outbox_messages_unprocessed</c>
    /// (<c>WHERE processed_at IS NULL AND attempt_count &lt; 3</c>).
    /// Changing this value requires a corresponding database migration to update the index filter.
    /// </summary>
    public const int MaxAttemptsIndexFilter = 3;
}
