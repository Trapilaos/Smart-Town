import { Component, OnInit, OnDestroy } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MembersService } from '../_services/members.service';
import { ParkingSpace } from '../_models/parking-space.model';
import { Reservation } from '../_models/reservation.model';
import { Member } from '../_models/member';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';
import { lastValueFrom, Subject, Subscription, takeUntil } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

@Component({
  selector: 'app-traffic',
  templateUrl: './traffic.component.html',
  styleUrls: ['./traffic.component.css']
})

export class TrafficComponent implements OnInit, OnDestroy {
  parkingSpaces: ParkingSpace[] = [];
  trafficData: any[] = [];
  selectedParkingSpaceId: number | null = null;
  reservationMessage: string = '';
  loading: boolean = false;
  errorMessage: string = '';
  currentUser: Member | null = null;
  reservationDuration: number | null = null;
  reservationTime: Date = new Date();

  private loadCurrentUserSubject = new Subject<void>();
  private currentUserSubscriptions = new Subscription();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private trafficAndParkingService: TrafficAndParkingService,
    private membersService: MembersService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.loadTrafficData();
    this.loadParkingSpaces();
    this.initCurrentUserSubscription();
  }

  ngOnDestroy(): void {
    this.currentUserSubscriptions.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  async loadTrafficData() {
    this.loading = true;
    try {
      const data = await lastValueFrom(this.trafficAndParkingService.getTrafficData().pipe(takeUntil(this.destroy$)));
      this.trafficData = data;
    } catch (error) {
      this.errorMessage = 'Failed to load traffic data';
    } finally {
      this.loading = false;
    }
  }

  async loadParkingSpaces() {
    this.loading = true;
    try {
      const data = await lastValueFrom(this.trafficAndParkingService.getParkingSpaces().pipe(takeUntil(this.destroy$)));
      this.parkingSpaces = data;
    } catch (error) {
      this.errorMessage = 'Failed to load parking spaces';
    } finally {
      this.loading = false;
    }
  }

  loadCurrentUser() {
    const token = localStorage.getItem('user');
    if (token) {
      const user: User = JSON.parse(token);
      this.loadCurrentUserSubject.next();
    }
  }

  selectParkingSpace(space: ParkingSpace) {
    this.selectedParkingSpaceId = space.id;
    this.reservationMessage = '';
    this.reservationDuration = null;
  }

  async confirmReservation() {
    console.log('confirmReservation called');
    if (!this.currentUser) {
      this.loadCurrentUser();
      return;
    }

    if (this.currentUser && this.selectedParkingSpaceId && this.reservationDuration) {
      console.log('Creating reservation object');
      const reservation: Reservation = {
        userId: this.currentUser.id.toString(),
        parkingSpaceId: this.selectedParkingSpaceId,
        reservationTime: new Date(this.reservationTime.getTime() - (this.reservationTime.getTimezoneOffset() * 60000)), // use current time as reservation time
        duration: this.reservationDuration
      };

      try {
        console.log('Making API call to reserve parking space');
        const response = await lastValueFrom(this.trafficAndParkingService.reserveParkingSpace(reservation).pipe(takeUntil(this.destroy$)));
        console.log('API call successful');
        this.toastr.success('Reservation successful!');
        await this.loadParkingSpaces(); // reload parking spaces data to update the page
        this.resetReservationForm(); // reset the reservation form
      } catch (error) {
        console.error('Error while making API call:', error);
        this.toastr.error('Failed to reserve parking space');
      }
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

  trackByFn(index: number, item: ParkingSpace) {
    return item.id;
  }

  private initCurrentUserSubscription() {
    this.currentUserSubscriptions.add(
      this.loadCurrentUserSubject
        .pipe(debounceTime(500))
        .subscribe(() => this.loadCurrentUser())
    );
  }
}
