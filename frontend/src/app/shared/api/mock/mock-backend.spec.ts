import { FpyTrendPointDto, StationFpyDto } from '../../models/fpy.dto';
import { KpiSummaryDto } from '../../models/kpi.dto';
import { LineDto } from '../../models/line.dto';
import { ParetoResponseDto } from '../../models/defects.dto';
import { PlanVsActualPointDto } from '../../models/plan.dto';
import { handleMockRequest, MockRequest } from './mock-backend';

const NOW = { date: '2026-11-13', hour: 14 };

function request(
  path: string,
  query: Record<string, string> = {},
  role = 'manager',
  lineIds: string[] = [],
): MockRequest {
  return { path, query, role, lineIds };
}

function ok<T>(req: MockRequest): T {
  const response = handleMockRequest(req, NOW);
  expect(response.status).toBe(200);
  return response.body as T;
}

describe('mock backend', () => {
  it('is deterministic: the same request gives the same data', () => {
    const req = request('/kpi/summary', { date: '2026-11-12' });

    expect(handleMockRequest(req, NOW)).toEqual(handleMockRequest(req, NOW));
  });

  it('shows a manager every line and an operator only their own', () => {
    const manager = ok<LineDto[]>(request('/lines'));
    const operator = ok<LineDto[]>(request('/lines', {}, 'operator', ['L1']));

    expect(manager.map((l) => l.id)).toEqual(['L1', 'L2', 'L3']);
    expect(operator.map((l) => l.id)).toEqual(['L1']);
    expect(manager[0].stations.map((s) => s.name)).toEqual(['SPI', 'AOI', 'ICT', 'FCT']);
  });

  it('answers 403 when a role asks for a line outside its scope (AC-7)', () => {
    const response = handleMockRequest(
      request('/kpi/summary', { date: '2026-11-12', lineId: 'L2' }, 'operator', ['L1']),
      NOW,
    );

    expect(response.status).toBe(403);
  });

  it('answers 404 for an unknown line and 400 for bad parameters', () => {
    expect(
      handleMockRequest(request('/kpi/summary', { date: '2026-11-12', lineId: 'L9' }), NOW).status,
    ).toBe(404);
    expect(handleMockRequest(request('/kpi/summary', { date: 'yesterday' }), NOW).status).toBe(400);
    expect(
      handleMockRequest(request('/fpy/trend', { from: '2026-11-10', to: '2026-11-01' }), NOW)
        .status,
    ).toBe(400);
    expect(
      handleMockRequest(request('/fpy/trend', { from: '2026-01-01', to: '2026-11-01' }), NOW)
        .status,
    ).toBe(400);
  });

  it('returns KPI values with targets, status and a delta against the previous day', () => {
    const summary = ok<KpiSummaryDto>(request('/kpi/summary', { date: '2026-11-12' }));

    expect(summary.planAttainment.target).toBe(95);
    expect(summary.lineRty.target).toBe(92);
    expect(['green', 'amber', 'red']).toContain(summary.planAttainment.status);
    expect(summary.planAttainment.deltaVsPrevDay).not.toBeNull();
    expect(summary.dpmo.value).toBeGreaterThan(0);
    expect(summary.topDefect?.count).toBeGreaterThan(0);
  });

  it('has no actual value for hours that have not started yet (today)', () => {
    const points = ok<PlanVsActualPointDto[]>(request('/plan-vs-actual', { date: NOW.date }));

    expect(points).toHaveLength(24);
    expect(points.filter((p) => p.hour <= NOW.hour).every((p) => p.good !== null)).toBe(true);
    expect(points.filter((p) => p.hour > NOW.hour).every((p) => p.good === null)).toBe(true);
    expect(points.every((p) => p.planned > 0)).toBe(true);
  });

  it('limits plan vs actual to the hours of the chosen shift', () => {
    const points = ok<PlanVsActualPointDto[]>(
      request('/plan-vs-actual', { date: '2026-11-12', shiftId: 'C' }),
    );

    expect(points.map((p) => p.hour)).toEqual([0, 1, 2, 3, 4, 5, 22, 23]);
  });

  it('returns the FPY trend only up to today', () => {
    const trend = ok<FpyTrendPointDto[]>(
      request('/fpy/trend', { from: '2026-11-10', to: '2026-11-20' }),
    );

    expect(trend.map((p) => p.date)).toEqual([
      '2026-11-10',
      '2026-11-11',
      '2026-11-12',
      '2026-11-13',
    ]);
  });

  it('returns four stations with FPY between 0 and 100', () => {
    const stations = ok<StationFpyDto[]>(request('/fpy/stations', { date: '2026-11-12' }));

    expect(stations.map((s) => s.name)).toEqual(['SPI', 'AOI', 'ICT', 'FCT']);
    for (const station of stations) {
      expect(station.firstPass).toBeLessThanOrEqual(station.tested);
      expect(station.fpy).toBeGreaterThan(0);
      expect(station.fpy).toBeLessThanOrEqual(100);
    }
  });

  it('builds a Pareto sorted by count that accumulates to exactly 100 (AC-5)', () => {
    const pareto = ok<ParetoResponseDto>(
      request('/defects/pareto', { from: '2026-11-06', to: '2026-11-12' }),
    );
    const counts = pareto.items.map((i) => i.count);

    expect(counts).toEqual([...counts].sort((a, b) => b - a));
    expect(pareto.items.at(-1)?.cumulativeShare).toBe(100);
    expect(pareto.dpmo).toBeGreaterThan(0);
  });

  it('filters the Pareto by station', () => {
    const all = ok<ParetoResponseDto>(
      request('/defects/pareto', { from: '2026-11-12', to: '2026-11-12' }),
    );
    const spi = ok<ParetoResponseDto>(
      request('/defects/pareto', { from: '2026-11-12', to: '2026-11-12', stationId: 'SPI' }),
    );

    const sum = (r: ParetoResponseDto) => r.items.reduce((s, i) => s + i.count, 0);
    expect(sum(spi)).toBeLessThan(sum(all));
    expect(
      handleMockRequest(
        request('/defects/pareto', { from: '2026-11-12', to: '2026-11-12', stationId: 'XYZ' }),
        NOW,
      ).status,
    ).toBe(404);
  });
});
