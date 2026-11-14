/**
 * TEMPORARY stand-in for the backend API (milestone M2).
 *
 * It generates deterministic data and answers the same endpoints as REQUIREMENTS section 6.
 * The metric formulas below duplicate the backend Core calculators on purpose and exist only
 * so the UI can be built. Delete this folder and set USE_MOCK_API = false once the API exists.
 */
import { addDays, daysBetween, isIsoDate, nowInKyiv } from '../../dates';
import { ParetoItemDto } from '../../models/defects.dto';
import { FpyTrendPointDto, StationFpyDto } from '../../models/fpy.dto';
import { KpiSummaryDto, MetricDto } from '../../models/kpi.dto';
import { LineDto, StationType } from '../../models/line.dto';
import { PlanVsActualPointDto } from '../../models/plan.dto';
import { RagStatus } from '../../models/rag-status.dto';
import { ThresholdDto, ThresholdsDto } from '../../models/thresholds.dto';

const SEED = 20261113;
const HISTORY_DAYS = 30;
const MAX_RANGE_DAYS = 90;
const OPPORTUNITIES_PER_UNIT = 200;
const STATION_TYPES: readonly StationType[] = ['SPI', 'AOI', 'ICT', 'FCT'];
const LINE_IDS: readonly string[] = ['L1', 'L2', 'L3'];
const SHIFT_HOURS: Record<string, readonly [number, number]> = {
  A: [6, 14],
  B: [14, 22],
  C: [22, 6],
};

const THRESHOLDS: ThresholdsDto = {
  planAttainment: { green: 95, amber: 85 },
  lineFpy: { green: 92, amber: 88 },
  stationFpy: { green: 97, amber: 94 },
};

const MEAN_FPY: Record<StationType, number> = { SPI: 98.6, AOI: 98.0, ICT: 97.6, FCT: 98.2 };
const LINE_FPY_OFFSET: Record<string, number> = { L1: 0.2, L2: 0, L3: -0.3 };

const DEFECT_NAMES: Record<string, string> = {
  SB: 'Перемичка припою',
  IS: 'Недостатньо припою',
  MC: 'Відсутній компонент',
  TS: 'Тумстоунінг',
  WP: 'Неправильна полярність',
  OT: 'Інше',
};

const DEFECT_WEIGHTS: Record<StationType, Record<string, number>> = {
  SPI: { IS: 0.7, SB: 0.2, OT: 0.1 },
  AOI: { SB: 0.35, MC: 0.2, TS: 0.2, WP: 0.15, OT: 0.1 },
  ICT: { SB: 0.4, WP: 0.25, IS: 0.15, MC: 0.1, OT: 0.1 },
  FCT: { OT: 0.5, WP: 0.2, MC: 0.15, SB: 0.15 },
};

// ---------------------------------------------------------------- data generation

class Rng {
  private state: number;

  constructor(seed: number) {
    this.state = seed >>> 0;
  }

  next(): number {
    this.state = (this.state + 0x6d2b79f5) >>> 0;
    let t = this.state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }

  int(min: number, max: number): number {
    return min + Math.floor(this.next() * (max - min + 1));
  }

  uniform(min: number, max: number): number {
    return min + this.next() * (max - min);
  }

  gauss(mean: number, sd: number): number {
    const u = 1 - this.next();
    const v = this.next();
    return mean + sd * Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
  }

  weighted(weights: Record<string, number>): string {
    const entries = Object.entries(weights);
    const total = entries.reduce((sum, [, weight]) => sum + weight, 0);
    let roll = this.next() * total;
    for (const [code, weight] of entries) {
      if (roll < weight) {
        return code;
      }
      roll -= weight;
    }
    return entries[entries.length - 1][0];
  }
}

interface DefectCount {
  station: StationType;
  code: string;
  count: number;
}

interface HourRecord {
  date: string;
  hour: number;
  lineId: string;
  planned: number;
  started: number;
  good: number;
  tested: Record<StationType, number>;
  firstPass: Record<StationType, number>;
  defects: DefectCount[];
}

function dayNumber(isoDate: string): number {
  const [year, month, day] = isoDate.split('-').map(Number);
  return Math.floor(Date.UTC(year, month - 1, day) / 86_400_000);
}

function inShift(hour: number, shiftId: string | undefined): boolean {
  if (!shiftId) {
    return true;
  }
  const [start, end] = SHIFT_HOURS[shiftId];
  return start < end ? hour >= start && hour < end : hour >= start || hour < end;
}

function generate(today: string): HourRecord[] {
  const records: HourRecord[] = [];

  for (let daysAgo = HISTORY_DAYS - 1; daysAgo >= 0; daysAgo--) {
    const date = addDays(today, -daysAgo);

    LINE_IDS.forEach((lineId, lineIndex) => {
      const rng = new Rng(SEED + dayNumber(date) * 31 + lineIndex * 7919);

      for (let hour = 0; hour < 24; hour++) {
        const planned = rng.int(80, 120);
        let started = Math.round(planned * rng.uniform(0.96, 1.03));
        // Planted anomaly: one hour of near-zero output on line 2 yesterday.
        if (daysAgo === 1 && lineId === 'L2' && hour === 11) {
          started = rng.int(1, 3);
        }

        let current = started;
        let failures = 0;
        const tested = {} as Record<StationType, number>;
        const firstPass = {} as Record<StationType, number>;
        const defects: DefectCount[] = [];

        for (const station of STATION_TYPES) {
          let mean = MEAN_FPY[station] + (LINE_FPY_OFFSET[lineId] ?? 0);
          // Planted anomaly: ICT is weak on shift C during the last three days.
          if (station === 'ICT' && inShift(hour, 'C') && daysAgo <= 2) {
            mean = 94.3;
          }

          const rate = Math.min(99.5, Math.max(94, rng.gauss(mean, 0.5))) / 100;
          const passed = Math.min(current, Math.round(current * rate));
          const failed = current - passed;
          tested[station] = current;
          firstPass[station] = passed;
          failures += failed;

          const counts: Record<string, number> = {};
          for (let i = 0; i < failed; i++) {
            const code = rng.weighted(DEFECT_WEIGHTS[station]);
            counts[code] = (counts[code] ?? 0) + 1;
          }
          for (const [code, count] of Object.entries(counts)) {
            defects.push({ station, code, count });
          }

          current = passed;
        }

        const good = Math.min(started, firstPass.FCT + Math.round(0.6 * failures));
        records.push({ date, hour, lineId, planned, started, good, tested, firstPass, defects });
      }
    });
  }

  return records;
}

let cache: { today: string; records: HourRecord[] } | null = null;

function data(today: string): HourRecord[] {
  if (cache?.today !== today) {
    cache = { today, records: generate(today) };
  }
  return cache.records;
}

// ---------------------------------------------------------------- calculations

interface Now {
  date: string;
  hour: number;
}

interface Scope {
  from: string;
  to: string;
  lineIds: readonly string[];
  shiftId?: string;
  /** Only hours up to and including this one (used to compare with "today so far"). */
  hourLimit?: number;
}

function hasStarted(record: HourRecord, now: Now): boolean {
  return record.date < now.date || (record.date === now.date && record.hour <= now.hour);
}

function select(scope: Scope, now: Now, includeFuture = false): HourRecord[] {
  return data(now.date).filter(
    (r) =>
      r.date >= scope.from &&
      r.date <= scope.to &&
      scope.lineIds.includes(r.lineId) &&
      inShift(r.hour, scope.shiftId) &&
      (scope.hourLimit === undefined || r.hour <= scope.hourLimit) &&
      (includeFuture || hasStarted(r, now)),
  );
}

interface Totals {
  planned: number;
  good: number;
  started: number;
  tested: Record<StationType, number>;
  firstPass: Record<StationType, number>;
}

function totals(records: HourRecord[]): Totals {
  const result: Totals = {
    planned: 0,
    good: 0,
    started: 0,
    tested: { SPI: 0, AOI: 0, ICT: 0, FCT: 0 },
    firstPass: { SPI: 0, AOI: 0, ICT: 0, FCT: 0 },
  };
  for (const r of records) {
    result.planned += r.planned;
    result.good += r.good;
    result.started += r.started;
    for (const station of STATION_TYPES) {
      result.tested[station] += r.tested[station];
      result.firstPass[station] += r.firstPass[station];
    }
  }
  return result;
}

function planAttainment(t: Totals): number | null {
  return t.planned === 0 ? null : (t.good / t.planned) * 100;
}

function stationFpy(tested: number, firstPass: number): number | null {
  return tested === 0 ? null : (firstPass / tested) * 100;
}

function lineRty(t: Totals): { value: number | null; incomplete: boolean } {
  let product = 1;
  let hasValue = false;
  let incomplete = false;
  for (const station of STATION_TYPES) {
    const fpy = stationFpy(t.tested[station], t.firstPass[station]);
    if (fpy === null) {
      incomplete = true;
      continue;
    }
    product *= fpy / 100;
    hasValue = true;
  }
  return hasValue ? { value: product * 100, incomplete } : { value: null, incomplete: true };
}

function defectCounts(records: HourRecord[], station?: StationType): Map<string, number> {
  const counts = new Map<string, number>();
  for (const r of records) {
    for (const d of r.defects) {
      if (!station || d.station === station) {
        counts.set(d.code, (counts.get(d.code) ?? 0) + d.count);
      }
    }
  }
  return counts;
}

function sum(values: Iterable<number>): number {
  let result = 0;
  for (const value of values) {
    result += value;
  }
  return result;
}

function dpmo(defects: number, units: number): number | null {
  return units === 0 ? null : (defects / (units * OPPORTUNITIES_PER_UNIT)) * 1_000_000;
}

function rag(value: number | null, threshold: ThresholdDto): RagStatus {
  if (value === null) {
    return 'unknown';
  }
  return value >= threshold.green ? 'green' : value >= threshold.amber ? 'amber' : 'red';
}

function delta(current: number | null, previous: number | null): number | null {
  return current !== null && previous !== null ? current - previous : null;
}

// ---------------------------------------------------------------- request handling

export interface MockRequest {
  path: string;
  query: Record<string, string>;
  role: string;
  lineIds: string[];
}

export interface MockResponse {
  status: number;
  body: unknown;
}

class MockError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

function requireDate(query: Record<string, string>, name: string): string {
  const value = query[name];
  if (!isIsoDate(value)) {
    throw new MockError(400, `${name} must be a date in yyyy-MM-dd format.`);
  }
  return value;
}

function optionalShift(query: Record<string, string>): string | undefined {
  const value = query['shiftId'];
  if (value === undefined || value === '') {
    return undefined;
  }
  if (!(value in SHIFT_HOURS)) {
    throw new MockError(400, 'shiftId must be A, B or C.');
  }
  return value;
}

function requireRange(query: Record<string, string>): { from: string; to: string } {
  const from = requireDate(query, 'from');
  const to = requireDate(query, 'to');
  const days = daysBetween(from, to);
  if (days < 0) {
    throw new MockError(400, 'from must be on or before to.');
  }
  if (days + 1 > MAX_RANGE_DAYS) {
    throw new MockError(400, `The range cannot exceed ${MAX_RANGE_DAYS} days.`);
  }
  return { from, to };
}

function visibleLineIds(req: MockRequest): string[] {
  return req.role === 'manager' ? [...LINE_IDS] : req.lineIds.filter((id) => LINE_IDS.includes(id));
}

function resolveLines(req: MockRequest, lineId: string | undefined): string[] {
  const scope = visibleLineIds(req);
  if (!lineId) {
    return scope;
  }
  if (!LINE_IDS.includes(lineId)) {
    throw new MockError(404, `Line '${lineId}' was not found.`);
  }
  if (!scope.includes(lineId)) {
    throw new MockError(403, 'This role has no access to the requested line.');
  }
  return [lineId];
}

function parseStation(stationId: string | undefined): { type?: StationType; lineId?: string } {
  if (!stationId) {
    return {};
  }
  const [maybeLine, maybeType] = stationId.includes('-')
    ? stationId.split('-')
    : [undefined, stationId];
  const type = STATION_TYPES.find((t) => t === maybeType);
  if (!type) {
    throw new MockError(404, `Station '${stationId}' was not found.`);
  }
  return { type, lineId: maybeLine };
}

function lineDto(lineId: string): LineDto {
  return {
    id: lineId,
    name: `Лінія ${lineId.slice(1)}`,
    stations: STATION_TYPES.map((type, index) => ({
      id: `${lineId}-${type}`,
      name: type,
      type,
      order: index + 1,
    })),
  };
}

function summary(req: MockRequest, now: Now): KpiSummaryDto {
  const date = requireDate(req.query, 'date');
  const lineIds = resolveLines(req, req.query['lineId']);
  const shiftId = optionalShift(req.query);

  const compute = (day: string, hourLimit?: number) => {
    const records = select({ from: day, to: day, lineIds, shiftId, hourLimit }, now);
    const t = totals(records);
    const counts = defectCounts(records);
    return {
      attainment: planAttainment(t),
      rty: lineRty(t),
      dpmo: dpmo(sum(counts.values()), t.started),
      counts,
    };
  };

  const current = compute(date);
  const previous = compute(addDays(date, -1), date === now.date ? now.hour : undefined);

  const planAttainmentMetric: MetricDto = {
    value: current.attainment,
    target: THRESHOLDS.planAttainment.green,
    status: rag(current.attainment, THRESHOLDS.planAttainment),
    deltaVsPrevDay: delta(current.attainment, previous.attainment),
  };
  const rtyMetric: MetricDto = {
    value: current.rty.value,
    target: THRESHOLDS.lineFpy.green,
    status: rag(current.rty.value, THRESHOLDS.lineFpy),
    deltaVsPrevDay: delta(current.rty.value, previous.rty.value),
    incomplete: current.rty.incomplete,
  };

  const top = [...current.counts.entries()].sort(
    (a, b) => b[1] - a[1] || a[0].localeCompare(b[0]),
  )[0];

  return {
    date,
    planAttainment: planAttainmentMetric,
    lineRty: rtyMetric,
    dpmo: { value: current.dpmo },
    topDefect: top ? { code: top[0], name: DEFECT_NAMES[top[0]] ?? top[0], count: top[1] } : null,
  };
}

function planVsActual(req: MockRequest, now: Now): PlanVsActualPointDto[] {
  const date = requireDate(req.query, 'date');
  const lineIds = resolveLines(req, req.query['lineId']);
  const shiftId = optionalShift(req.query);
  const records = select({ from: date, to: date, lineIds, shiftId }, now, true);

  const points: PlanVsActualPointDto[] = [];
  for (let hour = 0; hour < 24; hour++) {
    if (!inShift(hour, shiftId)) {
      continue;
    }
    const atHour = records.filter((r) => r.hour === hour);
    const started = date < now.date || (date === now.date && hour <= now.hour);
    points.push({
      hour,
      planned: sum(atHour.map((r) => r.planned)),
      good: started ? sum(atHour.map((r) => r.good)) : null,
    });
  }
  return points;
}

function fpyTrend(req: MockRequest, now: Now): FpyTrendPointDto[] {
  const { from, to } = requireRange(req.query);
  const lineIds = resolveLines(req, req.query['lineId']);

  const points: FpyTrendPointDto[] = [];
  for (let day = from; day <= to && day <= now.date; day = addDays(day, 1)) {
    const records = select({ from: day, to: day, lineIds }, now);
    points.push({ date: day, rty: records.length ? lineRty(totals(records)).value : null });
  }
  return points;
}

function fpyStations(req: MockRequest, now: Now): StationFpyDto[] {
  const date = requireDate(req.query, 'date');
  const lineId = req.query['lineId'] || undefined;
  const lineIds = resolveLines(req, lineId);
  const shiftId = optionalShift(req.query);
  const t = totals(select({ from: date, to: date, lineIds, shiftId }, now));

  return STATION_TYPES.map((type) => {
    const fpy = stationFpy(t.tested[type], t.firstPass[type]);
    return {
      stationId: lineId ? `${lineId}-${type}` : type,
      name: type,
      tested: t.tested[type],
      firstPass: t.firstPass[type],
      fpy,
      status: rag(fpy, THRESHOLDS.stationFpy),
    };
  });
}

function pareto(req: MockRequest, now: Now): { items: ParetoItemDto[]; dpmo: number | null } {
  const { from, to } = requireRange(req.query);
  const station = parseStation(req.query['stationId']);
  const lineIds = resolveLines(req, station.lineId ?? (req.query['lineId'] || undefined));
  const shiftId = optionalShift(req.query);

  const records = select({ from, to, lineIds, shiftId }, now);
  const counts = defectCounts(records, station.type);
  const total = sum(counts.values());

  let running = 0;
  const items = [...counts.entries()]
    .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
    .map(([code, count]) => {
      running += count;
      return {
        code,
        name: DEFECT_NAMES[code] ?? code,
        count,
        share: (count / total) * 100,
        cumulativeShare: (running / total) * 100,
      };
    });

  const t = totals(records);
  const units = station.type ? t.tested[station.type] : t.started;
  return { items, dpmo: dpmo(total, units) };
}

export function handleMockRequest(req: MockRequest, now: Now = nowInKyiv()): MockResponse {
  try {
    switch (req.path) {
      case '/lines':
        return { status: 200, body: visibleLineIds(req).map(lineDto) };
      case '/config/thresholds':
        return { status: 200, body: THRESHOLDS };
      case '/kpi/summary':
        return { status: 200, body: summary(req, now) };
      case '/plan-vs-actual':
        return { status: 200, body: planVsActual(req, now) };
      case '/fpy/trend':
        return { status: 200, body: fpyTrend(req, now) };
      case '/fpy/stations':
        return { status: 200, body: fpyStations(req, now) };
      case '/defects/pareto':
        return { status: 200, body: pareto(req, now) };
      default:
        throw new MockError(404, `No mock for ${req.path}.`);
    }
  } catch (error) {
    if (error instanceof MockError) {
      return {
        status: error.status,
        body: { type: 'about:blank', title: error.message, status: error.status },
      };
    }
    throw error;
  }
}
