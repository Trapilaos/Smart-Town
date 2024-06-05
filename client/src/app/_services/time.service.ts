// src/app/_services/time.service.ts
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class TimeService {
    isNightTime(): boolean {
        const currentHour = new Date().getHours();
        return currentHour >= 18 || currentHour < 6; // Lights on from 6 PM to 6 AM
    }
}
