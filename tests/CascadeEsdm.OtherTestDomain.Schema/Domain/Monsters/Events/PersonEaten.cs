using System;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.OtherTestDomain.Schema.Domain.Monsters.Events;
public record PersonEaten(Guid PersonId, int PainLevel) : IDomainEvent;