using Base;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-01: BaseEntity has Id (Guid), CreatedAt, UpdatedAt with proper defaults.
/// A new instance gets a non-empty Guid Id; CreatedAt/UpdatedAt are default (unset by constructor).
/// </summary>
public class BaseEntityTests
{
    // Concrete subclass for testing the abstract BaseEntity
    private sealed class TestEntity : BaseEntity { }

    [Fact]
    public void New_entity_gets_non_empty_Guid_Id_by_default()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Two_new_entities_receive_distinct_Ids()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void New_entity_CreatedAt_is_default_DateTime()
    {
        var entity = new TestEntity();

        Assert.Equal(default, entity.CreatedAt);
    }

    [Fact]
    public void New_entity_UpdatedAt_is_default_DateTime()
    {
        var entity = new TestEntity();

        Assert.Equal(default, entity.UpdatedAt);
    }

    [Fact]
    public void BaseEntity_implements_IBaseEntity()
    {
        var entity = new TestEntity();

        Assert.IsAssignableFrom<IBaseEntity>(entity);
    }
}
