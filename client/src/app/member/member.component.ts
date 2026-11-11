import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Event } from '../_models/event.model';
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

  events: Event[] = [];
  comments: Comment[] = [];
  newCommentContent = '';
  isAdmin = false;
  currentUserId: string | null = null;

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
      this.currentUserId = user ? String(user.userId) : null;
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

  isInterested(event: Event): boolean {
    return this.currentUserId !== null && event.interestedUsers.includes(this.currentUserId);
  }

  // The server only adds interest (there is no way to withdraw it), so the button stays in its "Interested" state.
  declareInterest(event: Event): void {
    if (this.isInterested(event) || this.interestPendingEventId !== null) return;

    this.interestPendingEventId = event.id;
    this.eventService.declareInterest(event.id).subscribe({
      next: (updated) => {
        this.events = this.events.map(e => e.id === updated.id ? updated : e);
        this.interestPendingEventId = null;
        this.toastr.success(`You are marked as interested in ${event.title}.`);
      },
      error: () => {
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
