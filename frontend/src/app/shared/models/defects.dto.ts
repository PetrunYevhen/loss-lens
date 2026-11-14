export interface ParetoItemDto {
  code: string;
  name: string;
  count: number;
  /** Percent of all defects, 0-100. */
  share: number;
  /** Percent of all defects up to and including this item, 0-100. */
  cumulativeShare: number;
}

export interface ParetoResponseDto {
  items: ParetoItemDto[];
  dpmo: number | null;
}
