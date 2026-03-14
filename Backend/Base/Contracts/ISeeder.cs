namespace Base.Contracts;

public interface ISeeder
{
    int Order { get; }
    void Seed(object context);
}
