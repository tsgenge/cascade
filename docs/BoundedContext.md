# Bounded Context
Bounded context are collections of related aggregates. Ideally, bounded context should allow teams who own them to minimise coordination with other teams during feature development; bounded context tend to be defined by their ubiquitous language. I we call it this, you call it that - we're likely in different bounded context.

## Size
Bounded Context should be as big as you can make it. At the start of the project, your bounded context should be everything. As it grows (complexity increases over time), it should be split away into smaller bounded context based on what you learn during development; new language emerging for example. However, the best reason to break a bounded context into two or more is _cognitive load_ - when the team can no longer hold the entire context in their head.

## Abstraction
Bounded context should be _logically abstracted_ from other bounded contexts. This means that the bounded context should not depend on the internal structure of other bounded contexts. Instead, it should depend on the public interface of other bounded contexts. It should not be a _new microservice_, which just create all sorts of friction and complexity. Instead, abstract into a separate assembly in the same solution, or a new solution but in the same repo.

Just don't take the microservice route unless you really need to.