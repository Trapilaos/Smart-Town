import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AuditEntry } from '../../_models/audit.model';
import { AdminDashboardService } from '../../_services/admin-dashboard.service';
import { getErrorMessages } from '../../_helpers/api-error';

/** How many entries the audit page loads. */
const AUDIT_PAGE_SIZE = 50;

/** Admin audit log: who did what, and when (UTC in the API, shown in local time here). */
@Component({
    selector: 'app-admin-audit',
    templateUrl: './admin-audit.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminAuditComponent implements OnInit {
  readonly pageSize = AUDIT_PAGE_SIZE;
  entries: AuditEntry[] = [];
  loading = true;
  loadError = '';

  constructor(private dashboard: AdminDashboardService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.dashboard.getAudit(AUDIT_PAGE_SIZE).subscribe({
      next: entries => {
        this.entries = entries;
        this.loading = false;
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }
}
