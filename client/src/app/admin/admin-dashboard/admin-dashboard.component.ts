import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { EventService } from '../../_services/event.service';
import { CommentService } from '../../_services/comment.service';
import { ToastrService } from 'ngx-toastr';
import { Event } from '../../_models/event.model';
import { Comment } from '../../_models/comment.model';
import { getErrorMessages } from '../../_helpers/api-error';

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

  eventsLoading = true;
  commentsLoading = true;
  eventsError: string | null = null;
  commentsError: string | null = null;

  /** Event waiting for a second click on "Delete". */
  confirmDeleteId: number | null = null;
  deletingEventId: number | null = null;
  seeingCommentId: number | null = null;

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
    this.eventsLoading = true;
    this.eventsError = null;
    this.eventService.getEvents().subscribe({
      next: (data) => {
        this.events = data;
        this.eventsLoading = false;
      },
      error: (error) => {
        this.eventsError = getErrorMessages(error)[0];
        this.eventsLoading = false;
      }
    });
  }

  loadComments(): void {
    this.commentsLoading = true;
    this.commentsError = null;
    this.commentService.getComments().subscribe({
      next: (data) => {
        this.comments = data;
        this.commentsLoading = false;
      },
      error: (error) => {
        this.commentsError = getErrorMessages(error)[0];
        this.commentsLoading = false;
      }
    });
  }

  askDeleteEvent(eventId: number): void {
    this.confirmDeleteId = eventId;
  }

  cancelDeleteEvent(): void {
    this.confirmDeleteId = null;
  }

  deleteEvent(eventId: number): void {
    this.deletingEventId = eventId;
    this.eventService.deleteEvent(eventId).subscribe({
      next: () => {
        this.events = this.events.filter(e => e.id !== eventId);
        this.confirmDeleteId = null;
        this.deletingEventId = null;
        this.toastr.success('Event deleted successfully');
      },
      error: () => {
        // the ErrorInterceptor already shows the server message
        this.deletingEventId = null;
      }
    });
  }

  markCommentAsSeen(commentId: number): void {
    this.seeingCommentId = commentId;
    this.commentService.markCommentAsSeen(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        this.seeingCommentId = null;
        this.toastr.success('Comment marked as seen');
      },
      error: () => {
        // the ErrorInterceptor already shows the server message
        this.seeingCommentId = null;
      }
    });
  }
}
