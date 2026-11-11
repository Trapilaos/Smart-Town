import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { WasteManagementService } from '../_services/waste-management.service';
import { WasteBin } from '../_models/waste-bin.model';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { AccountService } from '../_services/account.service';

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
  loading: boolean = false;
  errorMessage: string = '';
  statusMessage: string = '';
  isAdmin = false;

  constructor(private wasteManagementService: WasteManagementService, private toastr: ToastrService, private accountService: AccountService) { }

  ngOnInit(): void {
    // Emptying a bin (PUT /api/waste/{id}) is Admin-only.
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.isAdmin = !!user?.roles?.includes('Admin');
    });
    this.loadWasteBins();
    this.calculateOptimalPath();
  }

  loadWasteBins() {
    this.loading = true;
    this.wasteManagementService.getWasteBins().subscribe({
      next: (data: WasteBin[]) => {
        this.wasteBins = data;
        this.loading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to load waste bins';
        this.loading = false;
      }
    });
  }

  calculateOptimalPath() {
    this.loading = true;
    this.wasteManagementService.getOptimalPath().subscribe({
      next: (response: any) => {
        if (response.message) {
          this.statusMessage = response.message;
          this.optimalPath = [];
          this.toastr.info(this.statusMessage);
        } else {
          this.optimalPath = response;
          this.statusMessage = '';
        }
        this.loading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to calculate optimal path';
        this.loading = false;
      }
    });
  }

  emptyBin(bin: WasteBin) {
    bin.currentFillLevel = 0;
    this.wasteManagementService.updateWasteBin(bin).subscribe({
      next: (updatedBin: WasteBin) => {
        console.log('Waste bin updated:', updatedBin);
        this.wasteBins = this.wasteBins.map(b => b.id === bin.id ? updatedBin : b);
        this.calculateOptimalPath(); // Recalculate the optimal path after emptying a bin
      },
      error: (error: any) => {
        console.error('Error updating waste bin:', error);
      }
    });
  }
}
