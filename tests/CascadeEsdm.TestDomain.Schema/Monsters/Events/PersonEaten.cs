using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.TestDomain.Schema.Monsters.Events;

public record PersonEaten(Guid PersonId, int PainLevel) : IDomainEvent;