import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { LightingService, LightingStatus } from '../_services/lighting.service';

@Component({
    selector: 'app-lighting',
    templateUrl: './lighting.component.html',
    styleUrls: ['./lighting.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LightingComponent implements OnInit {
  town = 'Glyfada, Athens';
  status: LightingStatus | null = null;
  loading = false;
  error = '';

  constructor(private lightingService: LightingService) {}

  ngOnInit(): void {
    this.checkLightingStatus();
  }

  get isOn(): boolean {
    return this.status?.status?.toLowerCase() === 'on';
  }

  checkLightingStatus(): void {
    const town = this.town.trim();
    if (!town || this.loading) return;

    this.loading = true;
    this.error = '';
    this.lightingService.getLightingStatus(town).subscribe({
      next: (response) => {
        this.status = response;
        this.loading = false;
      },
      error: () => {
        // Shown inline instead of navigating to an error page.
        this.status = null;
        this.error = 'Lighting data is unavailable right now.';
        this.loading = false;
      }
    });
  }
}
