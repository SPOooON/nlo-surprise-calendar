namespace NloSurpriseCalendar.App.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
