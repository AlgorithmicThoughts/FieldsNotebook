namespace Common.Errors
{
    /// <summary>A request a Manager rejects before any read or write; the message is safe to show the user.</summary>
    public sealed class InvalidRequestException(string message) : Exception(message);
}
