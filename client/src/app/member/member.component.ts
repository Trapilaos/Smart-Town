import { Component, OnInit } from '@angular/core';
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
  styleUrls: ['./member.component.css']
})
export class MemberComponent implements OnInit {
  events: Event[] = [];
  comments: Comment[] = [];
  newCommentContent = '';
  isAdmin = false;

  constructor(
    private eventService: EventService,
    private commentService: CommentService,
    private toastr: ToastrService,
    public accountService: AccountService
  ) { }

  ngOnInit(): void {
    this.loadEvents();
    // GET /api/comments is Admin-only, so only admins load the list.
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.isAdmin = !!user?.roles?.includes('Admin');
      if (this.isAdmin) this.loadComments();
    });
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

  declareInterest(eventId: number): void {
    this.eventService.declareInterest(eventId).subscribe(() => {
      this.toastr.success('Interest declared successfully!');
    });
  }

  addComment(): void {
    const newComment: Comment = {
      id: 0,
      content: this.newCommentContent,
      userId: '', // ignored by the server, which uses the authenticated user
      date: new Date(),
      seen: false // ignored by the server
    };

    this.commentService.addComment(newComment).subscribe(() => {
      if (this.isAdmin) this.loadComments();
      this.newCommentContent = '';
      this.toastr.success('Comment added successfully!');
    });
  }
}
