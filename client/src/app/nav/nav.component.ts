import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';

/** Opens or closes the navbar on small screens by pressing Bootstrap's own toggler. */
function setMenuOpen(open: boolean) {
  const menu = document.getElementById('navbarCollapse');
  if (menu && menu.classList.contains('show') !== open) {
    (document.querySelector('.navbar-toggler') as HTMLElement | null)?.click();
  }
}

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
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {}

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
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  /** Collapses the mobile menu after a link is chosen. */
  closeMenu() {
    setMenuOpen(false);
  }

  redirectUser(user: User) {
    if (this.isAdmin(user)) {
      this.router.navigateByUrl('/admin');
    } else {
      this.router.navigateByUrl('/member');
    }
  }
}
