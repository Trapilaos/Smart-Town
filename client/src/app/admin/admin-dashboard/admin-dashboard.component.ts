import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { EventService } from '../../_services/event.service';
import { CommentService } from '../../_services/comment.service';
import { ToastrService } from 'ngx-toastr';
import { Event } from '../../_models/event.model';
import { Comment } from '../../_models/comment.model';

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrls: ['./admin-dashboard.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminDashboardComponent implements OnInit {
  events: Event[] = [];
  comments: Comment[] = [];

  constructor(
    private eventService: EventService,
    private commentService: CommentService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.loadEvents();
    this.loadComments();
  }

  loadEvents(): void {
    this.eventService.getEvents().subscribe((data) => {
      this.events = data;
    });
  }

  loadComments(): void {
    this.commentService.getComments().subscribe((data) => {
      this.comments = data;
    });
  }

  deleteEvent(eventId: number): void {
    this.eventService.deleteEvent(eventId).subscribe({
      next: () => {
        this.events = this.events.filter(e => e.id !== eventId);
        this.toastr.success('Event deleted successfully');
      },
      error: () => {} // the ErrorInterceptor already shows the server message
    });
  }

  markCommentAsSeen(commentId: number): void {
    this.commentService.markCommentAsSeen(commentId).subscribe({
      next: (comment) => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        this.toastr.success('Comment marked as done');
      },
      error: () => {} // the ErrorInterceptor already shows the server message
    });
  }
}
