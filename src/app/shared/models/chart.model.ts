/** A value at a point in time — the input of the area chart. */
export interface SeriesPoint {
  date: string;
  value: number;
}

/** One labelled group holding two comparable measures — the bar chart's input. */
export interface GroupedPoint {
  label: string;
  primary: number;
  secondary: number;
}

/** One slice of a categorical breakdown — the meter's input. */
export interface Slice {
  name: string;
  value: number;
}
