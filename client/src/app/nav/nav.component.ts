import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { NavStateService } from '../_services/nav-state.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';

@Component({
    selector: 'app-nav',
    templateUrl: './nav.component.html',
    styleUrls: ['./nav.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class NavComponent implements OnInit {
  model: any = {};

  constructor(
    public accountService: AccountService,
    public navState: NavStateService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {}

  get isMenuOpen(): boolean {
    return this.navState.menuOpen();
  }

  isAdmin(user: User): boolean {
    return user.roles.includes('Admin');
  }

  login() {
    this.accountService.login(this.model).subscribe({
      next: (user: User) => this.redirectUser(user),
      error: () => {} // the ErrorInterceptor already shows the server message
    });
  }

  logout() {
    this.navState.closeMenu();
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  /** Opens or closes the mobile menu (the hamburger button). */
  toggleMenu() {
    this.navState.toggleMenu();
  }

  /** Collapses the mobile menu after a link is chosen. */
  closeMenu() {
    this.navState.closeMenu();
  }

  redirectUser(user: User) {
    this.navState.closeMenu();
    if (this.isAdmin(user)) {
      this.router.navigateByUrl('/admin');
    } else {
      this.router.navigateByUrl('/member');
    }
  }
}
