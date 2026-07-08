using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.OtherTestDomain.Domain.Monsters.Events;

public record PersonEaten(Guid PersonId, int PainLevel) : IDomainEvent;