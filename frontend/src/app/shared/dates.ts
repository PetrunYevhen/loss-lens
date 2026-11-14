const PLANT_TIME_ZONE = 'Europe/Kyiv';
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

const dateFormat = new Intl.DateTimeFormat('en-CA', { timeZone: PLANT_TIME_ZONE });
const hourFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: PLANT_TIME_ZONE,
  hour: '2-digit',
  hourCycle: 'h23',
});

/** Today's date (yyyy-MM-dd) in plant local time. */
export function todayInKyiv(): string {
  return dateFormat.format(new Date());
}

/** Current date and hour (0-23) in plant local time. */
export function nowInKyiv(): { date: string; hour: number } {
  const now = new Date();
  return { date: dateFormat.format(now), hour: Number.parseInt(hourFormat.format(now), 10) };
}

export function isIsoDate(value: string | null | undefined): value is string {
  if (!value || !ISO_DATE.test(value)) {
    return false;
  }
  const [year, month, day] = value.split('-').map(Number);
  const parsed = new Date(Date.UTC(year, month - 1, day));
  return (
    parsed.getUTCFullYear() === year &&
    parsed.getUTCMonth() === month - 1 &&
    parsed.getUTCDate() === day
  );
}

export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10);
}

/** Whole days from `from` to `to` (negative when `to` is earlier). */
export function daysBetween(from: string, to: string): number {
  const toMs = (value: string) => {
    const [year, month, day] = value.split('-').map(Number);
    return Date.UTC(year, month - 1, day);
  };
  return Math.round((toMs(to) - toMs(from)) / 86_400_000);
}
