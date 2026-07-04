using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.TestDomain.Monsters.Events;

public record PersonEaten(Guid PersonId, int PainLevel) : IDomainEvent;