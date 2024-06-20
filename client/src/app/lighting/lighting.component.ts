// src/app/lighting/lighting.component.ts
import { Component, OnInit } from '@angular/core';
import { LightingService } from '../_services/lighting.service';

@Component({
  selector: 'app-lighting',
  templateUrl: './lighting.component.html',
  styleUrls: ['./lighting.component.css']
})
export class LightingComponent implements OnInit {
  statusMessage: string = 'Checking...';
  lightingStatus: string = 'Unknown';
  brightnessLevel: string = '';

  constructor(private lightingService: LightingService) {}

  ngOnInit() {
    this.checkLightingStatus();
  }

  checkLightingStatus() {
    this.lightingService.getLightingStatus('Glyfada,Athens').subscribe({
      next: (response) => {
        this.lightingStatus = response.status;
        this.brightnessLevel = response.brightness;
        this.statusMessage = this.brightnessLevel 
          ? `Lights are ${this.lightingStatus} at ${this.brightnessLevel} brightness` 
          : `Lights are ${this.lightingStatus}`;
      },
      error: (error) => {
        this.statusMessage = 'Error fetching lighting status: ' + error.message;
        console.error(error);
      }
    });
  }
}
