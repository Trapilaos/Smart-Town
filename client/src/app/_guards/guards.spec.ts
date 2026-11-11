import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
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
      ) as Observable<boolean | UrlTree>
    );

  beforeEach(() => {
    currentUser = new BehaviorSubject<User | null>(null);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AccountService, useValue: { currentUser$: currentUser.asObservable() } },
        { provide: ToastrService, useValue: toastr }
      ]
    });
  });

  describe('authGuard', () => {
    it('redirects anonymous visitors to home and shows an error', async () => {
      const result = await run(authGuard);
      expect(result instanceof UrlTree).toBeTrue();
      expect((result as UrlTree).toString()).toBe('/');
      expect(toastr.error).toHaveBeenCalled();
    });

    it('lets a signed-in user through', async () => {
      currentUser.next(member);
      expect(await run(authGuard)).toBeTrue();
      expect(toastr.error).not.toHaveBeenCalled();
    });
  });

  describe('adminGuard', () => {
    it('redirects anonymous visitors to home', async () => {
      const result = await run(adminGuard);
      expect(result instanceof UrlTree).toBeTrue();
      expect((result as UrlTree).toString()).toBe('/');
      expect(toastr.error).toHaveBeenCalled();
    });

    it('redirects a member without the Admin role to home', async () => {
      currentUser.next(member);
      const result = await run(adminGuard);
      expect(result instanceof UrlTree).toBeTrue();
      expect((result as UrlTree).toString()).toBe('/');
      expect(toastr.error).toHaveBeenCalled();
    });

    it('lets an admin through', async () => {
      currentUser.next(admin);
      expect(await run(adminGuard)).toBeTrue();
      expect(toastr.error).not.toHaveBeenCalled();
    });
  });
});
