import { Component, OnInit, OnDestroy } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MembersService } from '../_services/members.service';
import { ParkingSpace } from '../_models/parking-space.model';
import { Reservation } from '../_models/reservation.model';
import { Member } from '../_models/member';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom, Subject, Subscription, takeUntil } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';

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
  activeReservation: Reservation | null = null;
  private ws: WebSocket | null = null;

  private loadCurrentUserSubject = new Subject<void>();
  private currentUserSubscriptions = new Subscription();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private trafficAndParkingService: TrafficAndParkingService,
    private membersService: MembersService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.loadTrafficData();
    this.loadParkingSpaces();
    this.initCurrentUserSubscription();
    this.setupWebSocket();
    this.loadActiveReservation();

    document.addEventListener('visibilitychange', this.handleVisibilityChange.bind(this));
  }

  ngOnDestroy(): void {
    this.currentUserSubscriptions.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
    this.closeWebSocket();

    document.removeEventListener('visibilitychange', this.handleVisibilityChange.bind(this));
  }

  setupWebSocket() {
    this.ws = new WebSocket('wss://example.com/socket'); // Replace with your WebSocket URL
    this.ws.onopen = (event) => {
      console.log('WebSocket connection opened', event);
    };
    this.ws.onmessage = (event) => {
      const message = JSON.parse(event.data);
      console.log('WebSocket message received:', message);
      // Handle WebSocket message
    };
    this.ws.onclose = (event) => {
      console.log('WebSocket connection closed', event);
    };
    this.ws.onerror = (error) => {
      console.error('WebSocket error', error);
    };
  }

  closeWebSocket() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }

  handleVisibilityChange() {
    if (document.hidden) {
      console.log('Page hidden, closing WebSocket');
      this.closeWebSocket();
    } else {
      console.log('Page visible, reopening WebSocket');
      this.setupWebSocket();
    }
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

  async loadActiveReservation() {
    try {
      const reservation = await lastValueFrom(this.trafficAndParkingService.getActiveReservation().pipe(takeUntil(this.destroy$)));
      this.activeReservation = reservation;
      console.log('Active reservation loaded:', this.activeReservation);
    } catch (error) {
      console.log('No active reservation found.');
      this.activeReservation = null;
    }
  }

  loadCurrentUser(): Promise<void> {
    return new Promise((resolve, reject) => {
      const userStr = localStorage.getItem('user');
      if (userStr) {
        const user: User = JSON.parse(userStr);
        const username = user.username;

        if (username) {
          this.membersService.getMemberByUsername(username).subscribe({
            next: member => {
              this.currentUser = member;
              console.log('Current user loaded:', this.currentUser);
              resolve();
            },
            error: err => {
              console.error('Error loading current user:', err);
              reject(err);
            }
          });
        } else {
          console.error('Username not found in user object');
          reject('Username not found in user object');
        }
      } else {
        console.warn('No user token found in localStorage');
        reject('No token found');
      }
    });
  }

  selectParkingSpace(space: ParkingSpace) {
    this.selectedParkingSpaceId = space.id;
    this.reservationMessage = '';
    this.reservationDuration = null;
  }

  async confirmReservation() {
    console.log('confirmReservation called');

    if (!this.currentUser) {
      console.log('Current user not loaded, loading user...');
      try {
        await this.loadCurrentUser();
        console.log('Retrying reservation after loading user...');
        await this.confirmReservation(); // Retry reservation after loading user
      } catch (error) {
        console.error('Failed to load user, cannot proceed with reservation:', error);
        this.toastr.error('Failed to load user information.');
      }
      return;
    }

    console.log('Current User:', this.currentUser);
    console.log('Selected Parking Space ID:', this.selectedParkingSpaceId);
    console.log('Reservation Duration:', this.reservationDuration);

    if (this.currentUser && this.selectedParkingSpaceId && this.reservationDuration) {
      console.log('Creating reservation object');
      const reservation: Reservation = {
        userId: this.currentUser.id.toString(),
        parkingSpaceId: this.selectedParkingSpaceId,
        reservationTime: new Date(this.reservationTime.getTime() - (this.reservationTime.getTimezoneOffset() * 60000)),
        duration: this.reservationDuration
      };

      console.log('Reservation Object:', reservation);

      try {
        console.log('Making API call to reserve parking space');
        const response = await lastValueFrom(this.trafficAndParkingService.reserveParkingSpace(reservation).pipe(takeUntil(this.destroy$)));
        console.log('API call successful', response); // Log the response
        this.toastr.success(`Reservation successful! Start Time: ${response.startTime}, End Time: ${response.endTime}`);
        await this.loadParkingSpaces(); // reload parking spaces data to update the page
        this.loadActiveReservation(); // reload active reservation
        this.resetReservationForm(); // reset the reservation form
      } catch (error) {
        console.error('Error while making API call:', error);
        this.toastr.error('Failed to reserve parking space');
        console.error('Detailed Error:', error);
      }
    } else {
      if (!this.selectedParkingSpaceId) console.warn('No parking space selected');
      if (!this.reservationDuration) console.warn('No reservation duration selected');
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

  getEndTime(reservation: Reservation): Date {
    return new Date(new Date(reservation.reservationTime).getTime() + reservation.duration * 60000);
  }

  hasActiveReservation(): boolean {
    return !!this.activeReservation;
  }
}
