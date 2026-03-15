using Base;
using Shouldly;

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

        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Two_new_entities_receive_distinct_Ids()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        a.Id.ShouldNotBe(b.Id);
    }

    [Fact]
    public void New_entity_CreatedAt_is_default_DateTime()
    {
        var entity = new TestEntity();

        entity.CreatedAt.ShouldBe(default);
    }

    [Fact]
    public void New_entity_UpdatedAt_is_default_DateTime()
    {
        var entity = new TestEntity();

        entity.UpdatedAt.ShouldBe(default);
    }

    [Fact]
    public void BaseEntity_implements_IBaseEntity()
    {
        var entity = new TestEntity();

        entity.ShouldBeAssignableTo<IBaseEntity>();
    }
}
