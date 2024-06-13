import { Component, OnInit } from '@angular/core';
import { WeatherService } from '../_services/weather.service';
import { TimeService } from '../_services/time.service';
import { WeatherResponse } from '../_models/weather.model';

@Component({
  selector: 'app-lighting',
  templateUrl: './lighting.component.html',
  styleUrls: ['./lighting.component.css']
})
export class LightingComponent implements OnInit {
  statusMessage: string = 'Checking...';
  weatherCondition: string = 'Unknown';
  temperature: string = '';
  humidity: string = '';
  currentTime: string = '';
  lightsOn: boolean = false;
  nextLightOnTime: string = '';

  private moodyConditions: string[] = ['rain', 'storm', 'overcast', 'drizzle', 'fog', 'mist', 'snow', 'cloudy'];

  constructor(private weatherService: WeatherService, private timeService: TimeService) {}

  ngOnInit() {
    this.updateCurrentTime();
    this.checkLightingConditions();
    setInterval(() => this.updateCurrentTime(), 60000); // Update time every minute
    setInterval(() => this.checkLightingConditions(), 3600000); // Check every hour
  }

  updateCurrentTime() {
    const now = new Date();
    this.currentTime = now.toLocaleTimeString();
  }

  checkLightingConditions() {
    this.weatherService.getWeather('Glyfada,Athens').subscribe({
      next: (data: WeatherResponse) => {
        console.log('Weather data received:', data);  // Log the entire weather data to verify structure

        // Verify if the response has the expected structure
        if (data && data.current && data.current.condition && data.current.condition.text) {
          this.weatherCondition = data.current.condition.text;
        } else {
          // Log the unexpected structure of the response for debugging
          console.error('Unexpected structure or missing condition text:', data);
          this.weatherCondition = 'Unknown';
        }

        // Extract and format other weather data
        this.temperature = data.current ? `${data.current.temp_c} °C` : 'N/A';
        this.humidity = data.current ? `${data.current.humidity} %` : 'N/A';

        // Determine if the weather is moody
        const lowerCaseCondition = this.weatherCondition.toLowerCase();
        const isMoodyWeather = this.moodyConditions.some(condition => lowerCaseCondition.includes(condition));
        const isNight = this.timeService.isNightTime();
        const currentHour = new Date().getHours();

        // Decide when to turn on the lights based on the weather and time of day
        if (isMoodyWeather && currentHour >= 12) {
          this.turnOnLights();
        } else if (isNight) {
          this.turnOnLights();
        } else {
          this.turnOffLights();
          this.calculateNextLightOnTime(currentHour, isMoodyWeather);
        }
      },
      error: (error) => {
        this.statusMessage = 'Error fetching weather data: ' + error.message;  // Update status message on error
        console.error(error);  // Log the error
      }
    });
  }

  calculateNextLightOnTime(currentHour: number, isMoodyWeather: boolean) {
    const eveningStartHour = 18;  // 6 PM
    const soonThreshold = 2; // Turn on lights 2 hours before evening if moody weather

    if (isMoodyWeather && currentHour >= 12) {
      const nextLightOnTime = new Date();
      nextLightOnTime.setHours(currentHour + soonThreshold, 0, 0, 0);
      this.nextLightOnTime = nextLightOnTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (currentHour < eveningStartHour) {
      const nextLightOnTime = new Date();
      nextLightOnTime.setHours(eveningStartHour, 0, 0, 0);
      this.nextLightOnTime = nextLightOnTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else {
      this.nextLightOnTime = 'Lights are on for the night';
    }
  }

  turnOnLights() {
    this.lightsOn = true;
    this.statusMessage = 'Lights are ON';
  }

  turnOffLights() {
    this.lightsOn = false;
    this.statusMessage = 'Lights are OFF';
  }
}
