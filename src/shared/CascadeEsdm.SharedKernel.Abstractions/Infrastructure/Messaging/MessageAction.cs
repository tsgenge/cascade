namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public enum MessageAction
{
    Complete,
    Abandon,
    DeadLetter,
    Schedule
}
