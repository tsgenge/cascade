namespace CascadeEsdm.SharedKernel.Exceptions;

public abstract class ExceptionBase : Exception
{
    protected ExceptionBase(string message, Exception inner) : base(message, inner) { }

    protected ExceptionBase(string message) : base(message) { }

    protected ExceptionBase(string message, int httpStatus) : base(message)
    {
        HttpStatusCode = httpStatus;
    }

    protected ExceptionBase(string message, int httpStatus, Exception inner) : base(message, inner)
    {
        HttpStatusCode = httpStatus;
    }

    protected ExceptionBase(int httpStatus)
    {
        HttpStatusCode = httpStatus;
    }

    public int HttpStatusCode { get; protected set; } = 500;
    public string? PublicMessage { get; protected set; }
}