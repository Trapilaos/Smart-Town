import { Component, OnInit } from '@angular/core';
import { EventService } from '../../_services/event.service';
import { ToastrService } from 'ngx-toastr';
import { Event } from '../../_models/event.model';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  events: Event[] = [];

  constructor(private eventService: EventService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    this.eventService.getEvents().subscribe((data) => {
      this.events = data;
    });
  }

  deleteEvent(eventId: number): void {
    this.eventService.deleteEvent(eventId).subscribe({
      next: () => {
        this.events = this.events.filter(e => e.id !== eventId);
        this.toastr.success('Event deleted successfully');
      },
      error: error => this.toastr.error(error.error)
    });
  }
}
