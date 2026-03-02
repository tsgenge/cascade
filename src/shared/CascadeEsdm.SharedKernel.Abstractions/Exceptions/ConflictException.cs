namespace CascadeEsdm.SharedKernel.Exceptions;

public class ConflictException : ExceptionBase
{
    public ConflictException(string message) : base(message, 409)
    {

    }
}