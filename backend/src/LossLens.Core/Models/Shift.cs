namespace LossLens.Core.Models;

public class Shift
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public Shift(string name, TimeOnly startTime, TimeOnly endTime)
    {
        Id = Guid.NewGuid();
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
    }
}
