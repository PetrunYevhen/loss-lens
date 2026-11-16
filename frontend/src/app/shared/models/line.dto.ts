export type StationType = 'SPI' | 'AOI' | 'ICT' | 'FCT';

export interface StationDto {
  id: string;
  name: string;
  type: StationType;
  order: number;
}

export interface LineDto {
  id: string;
  name: string;
  stations: StationDto[];
}
