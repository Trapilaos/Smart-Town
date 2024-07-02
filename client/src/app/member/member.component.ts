import { Component, OnInit } from '@angular/core';
import { Event } from '../_models/event.model';
import { Comment } from '../_models/comment.model';
import { EventService } from '../_services/event.service';
import { CommentService } from '../_services/comment.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-member',
  templateUrl: './member.component.html',
  styleUrls: ['./member.component.css']
})
export class MemberComponent implements OnInit {
  events: Event[] = [];
  comments: Comment[] = [];
  newCommentContent = '';

  constructor(
    private eventService: EventService,
    private commentService: CommentService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.eventService.getEvents().subscribe((data) => {
      this.events = data;
    });

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
      userId: 'currentUserId', // Replace with actual user ID
      date: new Date()
    };

    this.commentService.addComment(newComment).subscribe((comment) => {
      this.comments.push(comment);
      this.newCommentContent = '';
      this.toastr.success('Comment added successfully!');
    });
  }
}
