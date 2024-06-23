import { Component, OnInit } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MembersService } from '../_services/members.service';
import { ParkingSpace } from '../_models/parking-space.model';
import { Reservation } from '../_models/reservation.model';
import { Member } from '../_models/member';

@Component({
  selector: 'app-traffic',
  templateUrl: './traffic.component.html',
  styleUrls: ['./traffic.component.css']
})
export class TrafficComponent implements OnInit {
  parkingSpaces: ParkingSpace[] = [];
  trafficData: any[] = [];
  selectedParkingSpaceId: number | null = null;
  reservationMessage: string = '';
  loading: boolean = false;
  errorMessage: string = '';
  currentUser: Member | null = null;
  reservationDuration: number | null = null;
  reservationTime: Date | null = null;

  constructor(
    private trafficAndParkingService: TrafficAndParkingService,
    private membersService: MembersService
  ) { }

  ngOnInit(): void {
    this.loadTrafficData();
    this.loadParkingSpaces();
    this.loadCurrentUser();
  }

  loadTrafficData() {
    this.loading = true;
    this.trafficAndParkingService.getTrafficData().subscribe({
      next: (data: any[]) => {
        this.trafficData = data;
        this.loading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to load traffic data';
        this.loading = false;
      }
    });
  }

  loadParkingSpaces() {
    this.loading = true;
    this.trafficAndParkingService.getParkingSpaces().subscribe({
      next: (data: ParkingSpace[]) => {
        this.parkingSpaces = data;
        this.loading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to load parking spaces';
        this.loading = false;
      }
    });
  }

  loadCurrentUser() {
    const token = localStorage.getItem('token');
    const username = localStorage.getItem('username');
    console.log('Token:', token);
  
    if (token && username) {
      this.membersService.getMemberByUsername(username).subscribe({
        next: (user: Member) => {
          this.membersService.setCurrentUser(user);
        },
        error: (error: any) => {
          this.errorMessage = 'Failed to load user data';
        }
      });
    }
  }
  
  selectParkingSpace(space: ParkingSpace) {
    this.selectedParkingSpaceId = space.id;
    this.reservationMessage = '';
    this.reservationDuration = null; // Reset duration when a new space is selected
  }

  confirmReservation() {
    if (this.currentUser && this.selectedParkingSpaceId && this.reservationDuration) {
      const reservation: Reservation = {
        userId: this.currentUser.id.toString(),
        parkingSpaceId: this.selectedParkingSpaceId,
        reservationTime: new Date(),
        duration: this.reservationDuration
      };
  
      this.trafficAndParkingService.reserveParkingSpace(reservation).subscribe({
        next: () => {
          this.reservationMessage = `Reservation successful for ${this.reservationDuration} minutes!`;
          const space = this.parkingSpaces.find(space => space.id === this.selectedParkingSpaceId);
          if (space) {
            space.currentVehicles++;
          }
          alert(this.reservationMessage); // Display success alert
        },
        error: (error: any) => {
          this.reservationMessage = 'Failed to reserve parking space';
          alert(this.reservationMessage); // Display error alert
        }
      });
    } else {
      this.reservationMessage = 'Please select a duration';
      alert(this.reservationMessage); // Display error alert for missing duration
    }
  }
  

  cancelReservation() {
    this.selectedParkingSpaceId = null;
    this.reservationMessage = '';
  }

  getSelectedParkingSpace() {
    return this.parkingSpaces.find(space => space.id === this.selectedParkingSpaceId);
  }

  getTrafficClass(trafficFlow: number) {
    if (trafficFlow > 75) {
      return 'traffic-high';
    } else if (trafficFlow > 40) {
      return 'traffic-medium';
    } else {
      return 'traffic-low';
    }
  }

  getReservationTime(): Date {
    return new Date();
  }
}
