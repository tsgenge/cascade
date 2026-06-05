# CascadeEsdm.Storage.Azure

Azure Table Storage implementation of `ITableStore<TEntity>` for the Cascade ESDM framework.

## Usage

```csharp
services.AddCascadeEsdm(cascade =>
{
    cascade.WithInfrastructure(infra =>
    {
        infra.UseAzureTableStorage(tables =>
        {
            tables.WithConnectionString("UseDevelopmentStorage=true");
        });
    });
});
```
