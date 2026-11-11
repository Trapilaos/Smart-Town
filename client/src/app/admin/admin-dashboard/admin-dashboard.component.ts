import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { CommentService } from '../../_services/comment.service';
import { AdminDashboardService } from '../../_services/admin-dashboard.service';
import { InvoiceService } from '../../_services/invoice.service';
import { TrafficAndParkingService } from '../../_services/traffic-and-parking.service';
import { Comment } from '../../_models/comment.model';
import { AuditEntry } from '../../_models/audit.model';
import { getErrorMessages } from '../../_helpers/api-error';

/** One KPI card. `value` is null while loading or when the source failed. */
export interface KpiState {
  loading: boolean;
  failed: boolean;
  value: number | null;
}

const RECENT_AUDIT_COUNT = 10;
const HIGH_FILL_PERCENT = 80;

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrls: ['./admin-dashboard.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminDashboardComponent implements OnInit {
  readonly highFillPercent = HIGH_FILL_PERCENT;
  readonly recentAuditCount = RECENT_AUDIT_COUNT;

  binsHigh: KpiState = loadingKpi();
  freeParking: KpiState = loadingKpi();
  unpaidTotal: KpiState = loadingKpi();
  overdueInvoices: KpiState = loadingKpi();
  upcomingEvents: KpiState = loadingKpi();

  /** Unpaid total is money, so it is kept apart from the count KPIs. */
  unpaidTotalAmount: number | null = null;

  recentAudit: AuditEntry[] = [];
  auditLoading = true;
  auditError = '';

  comments: Comment[] = [];
  commentsLoading = true;
  commentsError: string | null = null;
  seeingCommentId: number | null = null;

  constructor(
    private dashboard: AdminDashboardService,
    private parkingService: TrafficAndParkingService,
    private invoiceService: InvoiceService,
    private commentService: CommentService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadBins();
    this.loadParking();
    this.loadInvoiceSummary();
    this.loadUpcomingEvents();
    this.loadAudit();
    this.loadComments();
  }

  loadBins(): void {
    this.binsHigh = loadingKpi();
    this.dashboard.getBins().subscribe({
      next: bins => this.binsHigh = doneKpi(bins.filter(bin => bin.fillPercent >= HIGH_FILL_PERCENT).length),
      error: () => this.binsHigh = failedKpi()
    });
  }

  loadParking(): void {
    this.freeParking = loadingKpi();
    this.parkingService.getParkingSpaces().subscribe({
      next: spaces => this.freeParking = doneKpi(spaces.reduce((total, space) => total + space.freeNow, 0)),
      error: () => this.freeParking = failedKpi()
    });
  }

  loadInvoiceSummary(): void {
    this.unpaidTotal = loadingKpi();
    this.overdueInvoices = loadingKpi();
    this.invoiceService.getSummary().subscribe({
      next: summary => {
        this.unpaidTotalAmount = summary.unpaidTotal;
        this.unpaidTotal = doneKpi(summary.unpaidTotal);
        this.overdueInvoices = doneKpi(summary.overdueCount);
      },
      error: () => {
        this.unpaidTotal = failedKpi();
        this.overdueInvoices = failedKpi();
      }
    });
  }

  loadUpcomingEvents(): void {
    this.upcomingEvents = loadingKpi();
    this.dashboard.getUpcomingEventCount().subscribe({
      next: count => this.upcomingEvents = doneKpi(count),
      error: () => this.upcomingEvents = failedKpi()
    });
  }

  loadAudit(): void {
    this.auditLoading = true;
    this.auditError = '';
    this.dashboard.getAudit(RECENT_AUDIT_COUNT).subscribe({
      next: entries => {
        this.recentAudit = entries;
        this.auditLoading = false;
      },
      error: error => {
        this.auditError = getErrorMessages(error)[0];
        this.auditLoading = false;
      }
    });
  }

  loadComments(): void {
    this.commentsLoading = true;
    this.commentsError = null;
    this.commentService.getComments().subscribe({
      next: data => {
        this.comments = data;
        this.commentsLoading = false;
      },
      error: error => {
        this.commentsError = getErrorMessages(error)[0];
        this.commentsLoading = false;
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

function loadingKpi(): KpiState {
  return { loading: true, failed: false, value: null };
}

function doneKpi(value: number): KpiState {
  return { loading: false, failed: false, value };
}

function failedKpi(): KpiState {
  return { loading: false, failed: true, value: null };
}
