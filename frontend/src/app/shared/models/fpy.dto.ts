import { RagStatus } from './rag-status.dto';

export interface FpyTrendPointDto {
  date: string;
  rty: number | null;
}

export interface StationFpyDto {
  stationId: string;
  name: string;
  tested: number;
  firstPass: number;
  fpy: number | null;
  status: RagStatus;
}
