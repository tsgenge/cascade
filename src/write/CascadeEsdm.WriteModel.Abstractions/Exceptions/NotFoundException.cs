using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class NotFoundException : ExceptionBase
{
    public NotFoundException() : base(404)
    {

    }
    public NotFoundException(string message) : base(message, 404)
    {

    }

    public NotFoundException(string message, Exception ex) : base(message, ex) { }
}
