export interface ThresholdDto {
  green: number;
  amber: number;
}

export interface ThresholdsDto {
  planAttainment: ThresholdDto;
  lineFpy: ThresholdDto;
  stationFpy: ThresholdDto;
}
