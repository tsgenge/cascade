namespace CascadeEsdm.SharedKernel.Exceptions;

public class BadMessageFormatException : ExceptionBase
{
    public string EntityPath { get; set; }
    public string? SessionId { get; set; }
    public BadMessageFormatException(string entityPath, string? sessionId, Exception inner) : base("The message was in an invalid format, sending to a the deadletter queue.", 400, inner)
    {
        EntityPath = entityPath;
        SessionId = sessionId;
    }
}
