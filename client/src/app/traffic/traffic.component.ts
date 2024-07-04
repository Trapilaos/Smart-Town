import { Component, OnInit } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MembersService } from '../_services/members.service';
import { ParkingSpace } from '../_models/parking-space.model';
import { Reservation } from '../_models/reservation.model';
import { Member } from '../_models/member';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';

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
  reservationTime: Date = new Date();

  constructor(
    private trafficAndParkingService: TrafficAndParkingService,
    private membersService: MembersService,
    private toastr: ToastrService
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
    const token = localStorage.getItem('user');
    if (token) {
      const user: User = JSON.parse(token);
      this.membersService.getMemberByUsername(user.username).subscribe({
        next: (member: Member) => {
          this.currentUser = member;
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
    this.reservationDuration = null;
  }

  confirmReservation() {
    if (!this.currentUser) {
      this.loadCurrentUser();
      return;
    }

    if (this.currentUser && this.selectedParkingSpaceId && this.reservationDuration) {
      const reservation: Reservation = {
        userId: this.currentUser.id.toString(),
        parkingSpaceId: this.selectedParkingSpaceId,
        reservationTime: new Date(this.reservationTime.getTime() - (this.reservationTime.getTimezoneOffset() * 60000)), // use current time as reservation time
        duration: this.reservationDuration
      };

      this.trafficAndParkingService.reserveParkingSpace(reservation).subscribe({
        next: (response: Reservation) => {
          this.toastr.success('Reservation successful!');
          this.loadParkingSpaces(); // reload parking spaces data to update the page
          this.resetReservationForm(); // reset the reservation form
        },
        error: (error: any) => {
          this.toastr.error('Failed to reserve parking space');
        }
      });
    } else {
      this.toastr.warning('Please select a duration');
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
    return new Date(this.reservationTime.getTime() + this.reservationDuration! * 60000);
  }

  decrementVehicle(parkingSpaceId: number) {
    const space = this.parkingSpaces.find(space => space.id === parkingSpaceId);
    if (space) {
      space.currentVehicles--;
    }
  }

  resetReservationForm() {
    this.selectedParkingSpaceId = null;
    this.reservationDuration = null;
    this.reservationTime = new Date();
  }
}
