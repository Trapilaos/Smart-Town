// src/app/lighting/lighting.component.ts
import { Component, OnInit } from '@angular/core';
import { WeatherService } from '../_services/weather.service';
import { TimeService } from '../_services/time.service';

@Component({
  selector: 'app-lighting',
  templateUrl: './lighting.component.html',
  styleUrls: ['./lighting.component.css']
})
export class LightingComponent implements OnInit {
  statusMessage: string = 'Checking...';  // Initial status message
  weatherCondition: string = 'Unknown';   // Weather condition description
  temperature: string = '';               // Current temperature
  humidity: string = '';                  // Current humidity
  currentTime: string = '';               // Current local time
  lightsOn: boolean = false;              // Status of the lights
  nextLightOnTime: string = '';           // Next scheduled light on time

  constructor(private weatherService: WeatherService, private timeService: TimeService) {}

  ngOnInit() {
    // Update the current time and check lighting conditions on initialization
    this.updateCurrentTime();
    this.checkLightingConditions();
    setInterval(() => this.updateCurrentTime(), 60000);  // Update time every minute
    setInterval(() => this.checkLightingConditions(), 3600000);  // Check lighting conditions every hour
  }

  // Update the current local time
  updateCurrentTime() {
    const now = new Date();
    this.currentTime = now.toLocaleTimeString();
  }

  // Check the weather conditions and determine if the lights should be on or off
  checkLightingConditions() {
    this.weatherService.getWeather('Glyfada,Athens').subscribe({
      next: (data: any) => {
        console.log('Weather data received:', data);  // Log the weather data

        // Update weather condition, temperature, and humidity
        this.weatherCondition = data.current.condition.text;
        this.temperature = data.current.temp_c + ' °C';
        this.humidity = data.current.humidity + ' %';

        // Determine if the weather is severe
        const isSevereWeather = this.weatherCondition.toLowerCase().includes('rain') || this.weatherCondition.toLowerCase().includes('storm');

        // Check if it is currently night time
        const isNight = this.timeService.isNightTime();
        const currentHour = new Date().getHours();

        // Turn on lights if it's severe weather in the afternoon or if it's night time
        if (isSevereWeather && currentHour >= 12) {
          this.turnOnLights();
        } else if (isNight) {
          this.turnOnLights();
        } else {
          this.turnOffLights();
          this.calculateNextLightOnTime(currentHour, isSevereWeather);
        }
      },
      error: (error) => {
        this.statusMessage = 'Error fetching weather data: ' + error.message;  // Update status message on error
        console.error(error);  // Log the error
      }
    });
  }

  // Calculate the next time the lights should turn on
  calculateNextLightOnTime(currentHour: number, isSevereWeather: boolean) {
    if (isSevereWeather && currentHour >= 12) {
      this.nextLightOnTime = 'Soon due to severe weather';  // Next light on time due to severe weather
    } else {
      this.nextLightOnTime = '18:00 (6 PM)';  // Default next light on time at 6 PM
    }
  }

  // Turn on the lights and update the status message
  turnOnLights() {
    this.lightsOn = true;
    this.statusMessage = 'Lights are ON';
  }

  // Turn off the lights and update the status message
  turnOffLights() {
    this.lightsOn = false;
    this.statusMessage = 'Lights are OFF';
  }
}
