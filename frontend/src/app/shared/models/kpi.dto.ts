import { RagStatus } from './rag-status.dto';

export interface MetricDto {
  value: number | null;
  target: number;
  status: RagStatus;
  deltaVsPrevDay: number | null;
  /** True when some stations had no data and were skipped. */
  incomplete?: boolean;
}

export interface DpmoDto {
  value: number | null;
}

export interface TopDefectDto {
  code: string;
  name: string;
  count: number;
}

export interface KpiSummaryDto {
  date: string;
  planAttainment: MetricDto;
  lineRty: MetricDto;
  dpmo: DpmoDto;
  topDefect: TopDefectDto | null;
}
