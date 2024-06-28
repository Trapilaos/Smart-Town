export interface WasteBin {
    id: number;
    location: string;
    capacity: number;
    currentFillLevel: number;
    lastEmptied: Date;
}
  