import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminService, ManagedUser } from '../../_services/admin.service';
import { AccountService } from '../../_services/account.service';
import { ToastrService } from 'ngx-toastr';

export type ManagedRole = 'Member' | 'Admin';

@Component({
    selector: 'app-user-management',
    templateUrl: './user-management.component.html',
    styleUrls: ['./user-management.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class UserManagementComponent implements OnInit, OnDestroy {
  users: ManagedUser[] = [];
  readonly roleOptions: ManagedRole[] = ['Member', 'Admin'];

  loading = true;
  loadError: string | null = null;
  currentUsername: string | null = null;
  /** Username whose roles are being saved, or null when idle. */
  savingUser: string | null = null;

  private subscriptions = new Subscription();

  constructor(
    private adminService: AdminService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.subscriptions.add(
      this.accountService.currentUser$.subscribe(user => this.currentUsername = user?.username ?? null)
    );
    this.loadUsersWithRoles();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadUsersWithRoles(): void {
    this.loading = true;
    this.fetchUsers();
  }

  /** The role a user holds for this page: Admin if they have it, otherwise Member. */
  currentRole(user: ManagedUser): ManagedRole {
    return user.roles.includes('Admin') ? 'Admin' : 'Member';
  }

  isOwnRow(user: ManagedUser): boolean {
    return user.username === this.currentUsername;
  }

  updateUserRoles(user: ManagedUser, role: ManagedRole): void {
    if (this.savingUser !== null || this.isOwnRow(user) || this.currentRole(user) === role) {
      return;
    }

    this.savingUser = user.username;
    this.adminService.updateUserRoles(user.username, [role]).subscribe({
      next: () => {
        this.savingUser = null;
        this.toastr.success(`${user.username} is now ${role === 'Admin' ? 'an admin' : 'a member'}`);
        this.fetchUsers();
      },
      error: () => {
        // the ErrorInterceptor already shows the server message
        this.savingUser = null;
      }
    });
  }

  private fetchUsers(): void {
    this.loadError = null;
    this.adminService.getUsersWithRoles().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: () => {
        // the ErrorInterceptor already shows the server message
        this.loadError = 'The user list could not be loaded. Try again in a moment.';
        this.loading = false;
      }
    });
  }
}
