using CascadeEsdm.ReadModel.ValueObjects;

namespace CascadeEsdm.ReadModel.Querying;

public interface INotifyingResult
{
    NotificationGroup NotificationGroup { get; }
}