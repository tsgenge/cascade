namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class RowAdder<TView>
{
    public bool Creates { get; set; } = true;
    public Guid NewRowId { get; set; } = Guid.NewGuid();

    public RowAdder(Guid newRowId)
    {
        NewRowId = newRowId;
    }
}
