/** One marker for MapComponent. `color` and `iconClass` are applied as CSS, never as HTML. */
export interface MapMarker {
  id: number | string;
  lat: number;
  lng: number;
  color: string;
  iconClass: string;
  title: string;
  subtitle?: string;
}
