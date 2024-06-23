export interface Reservation {
    userId: string;
    parkingSpaceId: number;
    reservationTime: Date;
    duration: number; // Duration in minutes
}
  