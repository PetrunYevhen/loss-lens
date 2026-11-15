namespace LossLens.Application.Dtos;

public sealed record KpiSummaryDto(
    DateOnly Date,
    MetricDto PlanAttainment,
    RtyMetricDto LineRty,
    DpmoDto Dpmo,
    TopDefectDto? TopDefect);
