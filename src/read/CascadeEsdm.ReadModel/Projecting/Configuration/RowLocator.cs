using CascadeEsdm.ReadModel.Querying;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal record RowLocator<TView>(KeyValuePair<string, Guid> PropertySelector, QueryOperation Operation);
