import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from './_services/account.service';
import { User } from './_models/user';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AppComponent implements OnInit {
  title = 'Smart-Town';
  currentYear = new Date().getFullYear();


  constructor(private accountService : AccountService) { }

  ngOnInit(): void {
    this.setCurrentUser();
  }

  

  setCurrentUser(){
    const userString = localStorage.getItem('user');
    if(!userString) return;
    try {
      const user : User = JSON.parse(userString);
      this.accountService.setCurrentUser(user);
    } catch {
      localStorage.removeItem('user');
    }

  }

  
}
