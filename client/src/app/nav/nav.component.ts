import { Component, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  model: any = {};

  constructor(
    public accountService: AccountService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {}

  login() {
    this.accountService.login(this.model).subscribe({
      next: (user: User) => this.redirectUser(user),
      error: error => this.toastr.error(error.error)
    });
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  redirectUser(user: User) {
    if (user.roles.includes('Admin')) {
      this.router.navigateByUrl('/admin');
    } else {
      this.router.navigateByUrl('/member');
    }
  }
}
