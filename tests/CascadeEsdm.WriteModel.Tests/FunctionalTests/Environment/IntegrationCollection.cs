namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

/// <summary>
/// Single collection shared by every functional integration test class. Placing all integration
/// tests in one collection makes them run sequentially and share one <see cref="SharedContainerFixture"/>,
/// so the Docker test containers start only once per test run.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<SharedContainerFixture>;
