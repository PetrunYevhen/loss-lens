export interface ShiftDto {
  id: string;
  name: string;
  /** HH:mm */
  start: string;
  /** HH:mm. Earlier than the start for a shift that crosses midnight. */
  end: string;
}
