using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class UnauthorizedException : ExceptionBase
{
    public UnauthorizedException() : base("The command or query was unauthorised.", 403)
    {

    }

    public UnauthorizedException(string message) : base(message, 403)
    {
    }

    public UnauthorizedException(int httpStatus) : base(httpStatus)
    {
    }

    public UnauthorizedException(string message, Exception inner) : base(message, 403, inner)
    {
    }

    public UnauthorizedException(string message, int httpStatus) : base(message, httpStatus)
    {
    }

    public UnauthorizedException(string message, int httpStatus, Exception inner) : base(message, httpStatus, inner)
    {
    }
}
