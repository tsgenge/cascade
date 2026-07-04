using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.TestDomain.People.Commands;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.TestDomain.Schema.Monsters.Events;
using CascadeEsdm.WriteModel;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Policies;

namespace CascadeEsdm.TestDomain.People.Policies;

public class PersonEatenRemovesPersonPolicy : IPolicy
{
    private readonly ICommandHandler<RemovePerson> _handler;

    public PersonEatenRemovesPersonPolicy(ICommandHandler<RemovePerson> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public bool Supports(EventEnvelope envelope)
    {
        return envelope.Type == nameof(PersonEaten);
    }

    public async Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope.Event is PersonEaten @event) {
            await _handler.HandleAsync(new CommandEnvelope<RemovePerson>(
                new RemovePerson(new PersonId(@event!.PersonId)),
                envelope.SecurityContext,
                envelope.Channel
            ));
        }
    }
}