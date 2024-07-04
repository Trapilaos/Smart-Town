import { Component } from '@angular/core';
import { EventService } from '../../_services/event.service';
import { ToastrService } from 'ngx-toastr';
import { Event } from '../../_models/event.model';

@Component({
  selector: 'app-event-create',
  templateUrl: './event-create.component.html',
  styleUrls: ['./event-create.component.css']
})
export class EventCreateComponent {
  model: Event = {
    id: 0,
    title: '',
    description: '',
    photoUrl: '',
    date: new Date(),
    interestedUsers: []
  };

  constructor(private eventService: EventService, private toastr: ToastrService) {}

  createEvent() {
    this.eventService.createEvent(this.model).subscribe({
      next: response => {
        this.toastr.success('Event created successfully');
        this.model = { id: 0, title: '', description: '', photoUrl: '', date: new Date(), interestedUsers: [] }; // Clear the form
      },
      error: error => this.toastr.error(error.error)
    });
  }
}
