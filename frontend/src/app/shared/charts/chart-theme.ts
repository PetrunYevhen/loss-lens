/** Colors shared by all charts. Keep in sync with the CSS variables in styles.scss. */
export const CHART = {
  ink: '#14161a',
  ink2: '#4a4f58',
  muted: '#5f6570',
  line: '#e3e4e0',
  blue: '#2a78d6',
  pale: '#c5d0e0',
  good: '#1a7f4b',
  bad: '#c0392b',
} as const;

export const AXIS_LABEL = { color: CHART.muted, fontSize: 11 } as const;
export const SPLIT_LINE = { lineStyle: { color: CHART.line } } as const;
