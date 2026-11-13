namespace LossLens.Core.Config;

public record Threshold
{
    public double Green { get; }
    public double Amber { get; }
    
    public Threshold(double green, double amber)
    {
        if (green < amber) 
            throw new ArgumentException("Green threshold must be greater than or equal to amber.");
        
        Green = green;
        Amber = amber;
    }
}