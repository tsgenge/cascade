namespace CascadeEsdm.SharedKernel.Exceptions;

public interface ICascadeException
{
    int HttpStatusCode { get; }
    string? PublicMessage { get; }
    string Message { get; }
}

public abstract class ExceptionBase : Exception, ICascadeException
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

public abstract class AggregateExceptionBase : AggregateException, ICascadeException
{
    protected AggregateExceptionBase(string message, Exception[] inners) : base(message, inners) { }

    protected AggregateExceptionBase(string message) : base(message) { }

    protected AggregateExceptionBase(string message, int httpStatus) : base(message)
    {
        HttpStatusCode = httpStatus;
    }

    protected AggregateExceptionBase(string message, int httpStatus, Exception[] inners) : base(message, inners)
    {
        HttpStatusCode = httpStatus;
    }

    protected AggregateExceptionBase(int httpStatus)
    {
        HttpStatusCode = httpStatus;
    }

    public int HttpStatusCode { get; protected set; } = 500;
    public string? PublicMessage { get; protected set; }
}