const LOCALE = 'uk-UA';
export const EMPTY_VALUE = '—';

const formats = new Map<number, Intl.NumberFormat>();

function numberFormat(digits: number): Intl.NumberFormat {
  let format = formats.get(digits);
  if (!format) {
    format = new Intl.NumberFormat(LOCALE, {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
    });
    formats.set(digits, format);
  }
  return format;
}

export function formatNumber(value: number | null | undefined, digits = 1): string {
  return value === null || value === undefined ? EMPTY_VALUE : numberFormat(digits).format(value);
}

export function formatInt(value: number | null | undefined): string {
  return formatNumber(value, 0);
}

export function formatPercent(value: number | null | undefined, digits = 1): string {
  return value === null || value === undefined
    ? EMPTY_VALUE
    : `${numberFormat(digits).format(value)}%`;
}

/** Signed difference such as "+0,4" or "−2,1". */
export function formatDelta(value: number, digits = 1): string {
  const sign = value < 0 ? '−' : '+';
  return `${sign}${numberFormat(digits).format(Math.abs(value))}`;
}

/** yyyy-MM-dd to dd.MM.yyyy. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return `${day}.${month}.${year}`;
}

export function formatHourRange(hour: number): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${pad(hour)}:00–${pad(hour + 1)}:00`;
}
