import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { EventDTO } from '../_models/event.model';
import { Comment } from '../_models/comment.model';
import { EventService } from '../_services/event.service';
import { CommentService } from '../_services/comment.service';
import { take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../_services/account.service';

@Component({
    selector: 'app-member',
    templateUrl: './member.component.html',
    styleUrls: ['./member.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class MemberComponent implements OnInit {
  readonly maxCommentLength = 2000;

  events: EventDTO[] = [];
  comments: Comment[] = [];
  newCommentContent = '';
  isAdmin = false;

  eventsLoading = false;
  commentsLoading = false;
  commentSubmitting = false;
  interestPendingEventId: number | null = null;

  constructor(
    private eventService: EventService,
    private commentService: CommentService,
    private toastr: ToastrService,
    public accountService: AccountService
  ) { }

  ngOnInit(): void {
    this.loadEvents();
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.isAdmin = !!user?.roles?.includes('Admin');
      // GET /api/comments is Admin-only, so only admins load the list.
      if (this.isAdmin) this.loadComments();
    });
  }

  loadEvents(): void {
    this.eventsLoading = true;
    this.eventService.getEvents().subscribe({
      next: (data) => {
        this.events = data;
        this.eventsLoading = false;
      },
      error: () => {
        this.eventsLoading = false;
      }
    });
  }

  loadComments(): void {
    this.commentsLoading = true;
    this.commentService.getComments().subscribe({
      next: (data) => {
        this.comments = data;
        this.commentsLoading = false;
      },
      error: () => {
        this.commentsLoading = false;
      }
    });
  }

  /** Declares interest, or withdraws it when the user is already interested. The server returns the updated event. */
  toggleInterest(event: EventDTO): void {
    if (this.interestPendingEventId !== null) return;

    this.interestPendingEventId = event.id;
    const request = event.isInterested
      ? this.eventService.withdrawInterest(event.id)
      : this.eventService.declareInterest(event.id);

    request.subscribe({
      next: (updated) => {
        this.events = this.events.map(e => e.id === updated.id ? updated : e);
        this.interestPendingEventId = null;
        this.toastr.success(updated.isInterested
          ? `You are marked as interested in ${updated.title}.`
          : `You are no longer interested in ${updated.title}.`);
      },
      error: () => {
        // The ErrorInterceptor shows the server message.
        this.interestPendingEventId = null;
      }
    });
  }

  canSubmitComment(): boolean {
    return this.newCommentContent.trim().length > 0
      && this.newCommentContent.length <= this.maxCommentLength
      && !this.commentSubmitting;
  }

  addComment(): void {
    if (!this.canSubmitComment()) return;

    const newComment: Comment = {
      id: 0,
      content: this.newCommentContent.trim(),
      userId: '', // ignored by the server, which uses the authenticated user
      date: new Date(),
      seen: false // ignored by the server
    };

    this.commentSubmitting = true;
    this.commentService.addComment(newComment).subscribe({
      next: () => {
        this.commentSubmitting = false;
        this.newCommentContent = '';
        this.toastr.success('Comment posted.');
        if (this.isAdmin) this.loadComments();
      },
      error: () => {
        this.commentSubmitting = false;
      }
    });
  }
}
