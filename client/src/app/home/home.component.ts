import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from 'src/app/_services/account.service';
/** Opens or closes the navbar on small screens by pressing Bootstrap's own toggler. */
function setMenuOpen(open: boolean) {
  const menu = document.getElementById('navbarCollapse');
  if (menu && menu.classList.contains('show') !== open) {
    (document.querySelector('.navbar-toggler') as HTMLElement | null)?.click();
  }
}

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HomeComponent implements OnInit {
  registerMode = false;

  constructor(public accountService: AccountService) { }

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
    setMenuOpen(true);
    document.getElementById('navUsername')?.focus();
  }
}
