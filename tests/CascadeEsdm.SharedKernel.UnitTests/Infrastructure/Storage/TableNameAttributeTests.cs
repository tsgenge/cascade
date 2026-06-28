using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Infrastructure.Storage;

public class TableNameAttributeTests
{
    [Fact]
    public void Name_DefaultsToNull()
    {
        var attribute = new TableNameAttribute();

        attribute.Name.Should().BeNull();
    }

    [Fact]
    public void Name_CanBeSet()
    {
        var attribute = new TableNameAttribute { Name = "MyTable" };

        attribute.Name.Should().Be("MyTable");
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        var attribute = typeof(DecoratedClass)
            .GetCustomAttributes(typeof(TableNameAttribute), false)
            .FirstOrDefault() as TableNameAttribute;

        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("TestTable");
    }

    [Fact]
    public void Attribute_DoesNotAllowMultiple()
    {
        var attributeUsage = typeof(TableNameAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse();
    }

    [TableName(Name = "TestTable")]
    private class DecoratedClass { }
}
