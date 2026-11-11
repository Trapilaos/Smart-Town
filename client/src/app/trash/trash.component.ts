import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { WasteManagementService } from '../_services/waste-management.service';
import { WasteBin } from '../_models/waste-bin.model';
import { take } from 'rxjs';
import { AccountService } from '../_services/account.service';

type OptimalPathResponse = string[] | { message: string };

@Component({
    selector: 'app-trash',
    templateUrl: './trash.component.html',
    styleUrls: ['./trash.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class TrashComponent implements OnInit {
  wasteBins: WasteBin[] = [];
  optimalPath: string[] = [];
  pathMessage = '';
  binsLoading = true;
  pathLoading = true;
  binsError = '';
  pathError = '';
  emptyingBinId: number | null = null;
  isAdmin = false;

  private readonly relativeFormat = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

  constructor(private wasteManagementService: WasteManagementService, private accountService: AccountService) { }

  ngOnInit(): void {
    // Emptying a bin (PUT /api/waste/{id}) is Admin-only.
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.isAdmin = !!user?.roles?.includes('Admin');
    });
    this.loadWasteBins();
    this.calculateOptimalPath();
  }

  loadWasteBins(): void {
    this.wasteManagementService.getWasteBins().subscribe({
      next: (data: WasteBin[]) => {
        this.wasteBins = data;
        this.binsError = '';
        this.binsLoading = false;
      },
      error: () => {
        this.binsError = 'Waste bins are unavailable right now.';
        this.binsLoading = false;
      }
    });
  }

  calculateOptimalPath(): void {
    this.pathLoading = true;
    this.wasteManagementService.getOptimalPath().subscribe({
      next: (response: OptimalPathResponse) => {
        if (Array.isArray(response)) {
          this.optimalPath = response;
          this.pathMessage = '';
        } else {
          this.optimalPath = [];
          this.pathMessage = response.message;
        }
        this.pathError = '';
        this.pathLoading = false;
      },
      error: () => {
        this.pathError = 'The collection route is unavailable right now.';
        this.pathLoading = false;
      }
    });
  }

  emptyBin(bin: WasteBin): void {
    if (this.emptyingBinId !== null) return;

    const previousFillLevel = bin.currentFillLevel;
    const emptied: WasteBin = { ...bin, currentFillLevel: 0 };
    this.emptyingBinId = bin.id;
    this.wasteManagementService.updateWasteBin(emptied).subscribe({
      next: (updatedBin: WasteBin) => {
        this.wasteBins = this.wasteBins.map(b => b.id === bin.id ? updatedBin : b);
        this.emptyingBinId = null;
        this.calculateOptimalPath(); // The route depends on which bins still need collecting.
      },
      error: () => {
        // The interceptor shows the API message, so only undo the optimistic change here.
        bin.currentFillLevel = previousFillLevel;
        this.emptyingBinId = null;
      }
    });
  }

  fillPercent(bin: WasteBin): number {
    if (!bin.capacity) return 0;
    return Math.min(100, Math.round((bin.currentFillLevel / bin.capacity) * 100));
  }

  fillClass(bin: WasteBin): string {
    const percent = this.fillPercent(bin);
    if (percent >= 80) return 'bg-danger';
    if (percent >= 50) return 'bg-warning';
    return 'bg-success';
  }

  fillTextClass(bin: WasteBin): string {
    return this.fillClass(bin).replace('bg-', 'text-');
  }

  isBackToStart(index: number): boolean {
    return this.optimalPath.length > 1
      && index === this.optimalPath.length - 1
      && this.optimalPath[index] === this.optimalPath[0];
  }

  relativeTime(value: Date | string): string {
    const time = new Date(value).getTime();
    if (isNaN(time)) return '';

    const minutes = Math.round((time - Date.now()) / 60000);
    const units: [Intl.RelativeTimeFormatUnit, number][] = [
      ['year', 525600],
      ['month', 43200],
      ['day', 1440],
      ['hour', 60],
      ['minute', 1]
    ];
    for (const [unit, minutesPerUnit] of units) {
      if (Math.abs(minutes) >= minutesPerUnit) {
        return this.relativeFormat.format(Math.round(minutes / minutesPerUnit), unit);
      }
    }
    return 'just now';
  }
}
