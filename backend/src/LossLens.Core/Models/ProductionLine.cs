namespace LossLens.Core.Models;

public class ProductionLine
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public ProductionLine(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Name = name;
    }
}
