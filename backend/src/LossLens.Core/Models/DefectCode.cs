namespace LossLens.Core.Models;

public class DefectCode
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }

    public DefectCode(string code, string name, string category)
    {
        Code = code;
        Name = name;
        Category = category;
    }
}
