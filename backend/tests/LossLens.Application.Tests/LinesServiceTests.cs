using LossLens.Application.Security;
using LossLens.Application.Services;
using LossLens.Application.Tests.Fakes;
using LossLens.Application.Tests.Support;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests;

public class LinesServiceTests
{
    private readonly TestWorld _world = new();

    private LinesService Service => new(_world.Repo);

    [Fact]
    public async Task Manager_GetsAllLinesByName()
    {
        var lines = await Service.GetLinesAsync(UserContext.Manager);

        Assert.Equal(new[] { "Line 1", "Line 2" }, lines.Select(l => l.Name));
    }

    [Fact]
    public async Task Operator_GetsOnlyOwnLine()
    {
        var lines = await Service.GetLinesAsync(TestWorld.Operator(_world.Line2));

        var line = Assert.Single(lines);
        Assert.Equal(_world.Line2.Id, line.Id);
    }

    [Fact]
    public async Task OperatorWithoutLines_GetsEmptyList()
    {
        var lines = await Service.GetLinesAsync(new UserContext(UserRole.Operator, []));

        Assert.Empty(lines);
    }

    [Fact]
    public async Task Stations_AreOrderedAlongTheLineAndNumberedFromOne()
    {
        var repo = new FakeMetricsRepository();
        var line = new ProductionLine("Line 1");
        repo.Lines.Add(line);
        // Added in the wrong order on purpose: the repository sorts them SPI, AOI, ICT, FCT.
        repo.Stations.Add(new Station(line.Id, "FCT-1", StationType.Fct));
        repo.Stations.Add(new Station(line.Id, "SPI-1", StationType.Spi));
        repo.Stations.Add(new Station(line.Id, "ICT-1", StationType.Ict));
        repo.Stations.Add(new Station(line.Id, "AOI-1", StationType.Aoi));

        var result = await new LinesService(repo).GetLinesAsync(UserContext.Manager);

        var stations = result[0].Stations;
        Assert.Equal(new[] { "SPI", "AOI", "ICT", "FCT" }, stations.Select(s => s.Type));
        Assert.Equal(new[] { 1, 2, 3, 4 }, stations.Select(s => s.Order));
    }

    [Fact]
    public async Task StationsOfDifferentLines_AreNotMixed()
    {
        var lines = await Service.GetLinesAsync(UserContext.Manager);

        Assert.All(lines, line => Assert.Equal(4, line.Stations.Count));
        Assert.Equal(
            new[] { "SPI-1", "AOI-1", "ICT-1", "FCT-1" },
            lines[0].Stations.Select(s => s.Name));
        Assert.Equal(
            new[] { "SPI-2", "AOI-2", "ICT-2", "FCT-2" },
            lines[1].Stations.Select(s => s.Name));
    }

    [Fact]
    public async Task LineWithoutStations_HasEmptyStationList()
    {
        var repo = new FakeMetricsRepository();
        repo.Lines.Add(new ProductionLine("Empty line"));

        var result = await new LinesService(repo).GetLinesAsync(UserContext.Manager);

        Assert.Empty(Assert.Single(result).Stations);
    }
}
