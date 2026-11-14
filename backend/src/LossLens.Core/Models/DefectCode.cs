namespace LossLens.Core.Models;

public class DefectCode
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }

    public DefectCode(string code, string name, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Code = code.Trim().ToUpperInvariant();
        Name = name;
        Category = category;
    }
}
