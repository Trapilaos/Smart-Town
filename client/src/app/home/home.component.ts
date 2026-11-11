import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from 'src/app/_services/account.service';
import { NavStateService } from 'src/app/_services/nav-state.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HomeComponent implements OnInit {
  registerMode = false;

  constructor(public accountService: AccountService, private navState: NavStateService) { }

  ngOnInit(): void { }

  isAdmin(roles: string[]): boolean {
    return roles.includes('Admin');
  }

  registerToggle() {
    this.registerMode = !this.registerMode;
  }

  cancelRegisterMode(event: boolean) {
    this.registerMode = event;
  }

  /** Opens the navbar login form (on mobile it sits in the collapsed menu) and focuses the username. */
  focusLogin() {
    this.navState.openMenu();
    // Wait for the navbar to render its open state before focusing a field that may be hidden.
    setTimeout(() => document.getElementById('navUsername')?.focus());
  }
}
