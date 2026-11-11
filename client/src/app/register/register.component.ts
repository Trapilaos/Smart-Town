import { Component, EventEmitter, Input, OnInit, Output, ChangeDetectionStrategy } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages } from '../_helpers/api-error';

@Component({
    selector: 'app-register',
    templateUrl: './register.component.html',
    styleUrls: ['./register.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class RegisterComponent implements OnInit {
  @Output() cancelRegister  = new EventEmitter();
  model: any = {}
  confirmPassword = '';
  serverErrors: string[] = [];
  isSubmitting = false;

  constructor(private accountService: AccountService, private toastr: ToastrService){ }

  ngOnInit(): void {
  }

  get passwordsMismatch(): boolean {
    return this.confirmPassword !== (this.model.password ?? '');
  }

  register(){
    this.serverErrors = [];
    this.isSubmitting = true;
    this.accountService.register(this.model).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastr.success('Welcome to Smart-Town!');
        this.cancel();
      },
      error: error => {
        this.isSubmitting = false;
        this.serverErrors = getErrorMessages(error);
      }
    })
  }

  cancel(){
    this.cancelRegister.emit(false);
  }

}
