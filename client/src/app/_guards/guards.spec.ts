import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { BehaviorSubject, Observable, firstValueFrom } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';
import { authGuard } from './auth.guard';
import { adminGuard } from './admin.guard';

describe('route guards', () => {
  let currentUser: BehaviorSubject<User | null>;
  let toastr: jasmine.SpyObj<ToastrService>;

  const member: User = { userId: 1, username: 'lisa', token: 't', roles: ['Member'] };
  const admin: User = { userId: 2, username: 'admin', token: 't', roles: ['Admin', 'Moderator'] };

  const run = (guard: typeof authGuard) =>
    firstValueFrom(
      TestBed.runInInjectionContext(() =>
        guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot)
      ) as Observable<boolean>
    );

  beforeEach(() => {
    currentUser = new BehaviorSubject<User | null>(null);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);
    TestBed.configureTestingModule({
      providers: [
        { provide: AccountService, useValue: { currentUser$: currentUser.asObservable() } },
        { provide: ToastrService, useValue: toastr }
      ]
    });
  });

  describe('authGuard', () => {
    it('blocks anonymous visitors and shows an error', async () => {
      expect(await run(authGuard)).toBeFalse();
      expect(toastr.error).toHaveBeenCalled();
    });

    it('lets a signed-in user through', async () => {
      currentUser.next(member);
      expect(await run(authGuard)).toBeTrue();
      expect(toastr.error).not.toHaveBeenCalled();
    });
  });

  describe('adminGuard', () => {
    it('blocks anonymous visitors', async () => {
      expect(await run(adminGuard)).toBeFalse();
      expect(toastr.error).toHaveBeenCalled();
    });

    it('blocks a member without the Admin role', async () => {
      currentUser.next(member);
      expect(await run(adminGuard)).toBeFalse();
      expect(toastr.error).toHaveBeenCalled();
    });

    it('lets an admin through', async () => {
      currentUser.next(admin);
      expect(await run(adminGuard)).toBeTrue();
      expect(toastr.error).not.toHaveBeenCalled();
    });
  });
});
