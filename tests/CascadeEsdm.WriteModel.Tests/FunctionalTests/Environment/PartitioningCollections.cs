namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

[CollectionDefinition("AllSharedPolicies")]
public class AllSharedPoliciesCollection : ICollectionFixture<AllSharedPoliciesEnvironment>;

[CollectionDefinition("MixedPartitioning")]
public class MixedPartitioningCollection : ICollectionFixture<MixedPartitioningEnvironment>;

[CollectionDefinition("OnlyPartitioning")]
public class OnlyPartitioningCollection : ICollectionFixture<OnlyPartitioningEnvironment>;
