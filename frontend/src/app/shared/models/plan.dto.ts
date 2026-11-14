export interface PlanVsActualPointDto {
  /** Hour of day, 0-23. */
  hour: number;
  planned: number;
  /** Null for hours that have not started yet. */
  good: number | null;
}
