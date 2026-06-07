using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal record SupportedEvent<TEvent>() where TEvent : IDomainEvent;
