import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../_services/admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  users: any[] = [];

  constructor(private adminService: AdminService, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.loadUsersWithRoles();
  }

  loadUsersWithRoles() {
    this.adminService.getUsersWithRoles().subscribe({
      next: users => this.users = users,
      error: error => this.toastr.error(error.error)
    });
  }

  updateUserRoles(username: string, roles: string[]) {
    this.adminService.updateUserRoles(username, roles).subscribe({
      next: () => {
        this.toastr.success('Roles updated successfully');
        this.loadUsersWithRoles(); // Refresh the user list
      },
      error: error => this.toastr.error(error.error)
    });
  }
}
