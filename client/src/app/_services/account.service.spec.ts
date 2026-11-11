import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { AccountService } from './account.service';

/** Builds an unsigned JWT-shaped string; the service only decodes the payload. */
export function fakeToken(roles: string | string[]): string {
  const encode = (value: object) => btoa(JSON.stringify(value));
  const exp = Math.floor(Date.now() / 1000) + 3600;
  return `${encode({ alg: 'none' })}.${encode({ nameid: '1', unique_name: 'lisa', role: roles, exp })}.sig`;
}

describe('AccountService', () => {
  let service: AccountService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AccountService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('login posts the credentials and stores the user', async () => {
    const result = service.login({ username: 'lisa', password: 'Pa$$w0rd' }).toPromise();

    const req = http.expectOne(r => r.url.endsWith('account/login'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'lisa', password: 'Pa$$w0rd' });
    req.flush({ username: 'lisa', token: fakeToken('Member') });
    await result;

    const user = await firstValueFrom(service.currentUser$);
    expect(user?.username).toBe('lisa');
    expect(user?.roles).toEqual(['Member']);
    expect(JSON.parse(localStorage.getItem('user')!).username).toBe('lisa');
  });

  it('restores a saved user from local storage when the service starts', async () => {
    localStorage.setItem('user', JSON.stringify({ userId: 1, username: 'lisa', token: fakeToken('Member'), roles: [] }));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const restored = TestBed.inject(AccountService);

    expect((await firstValueFrom(restored.currentUser$))?.roles).toEqual(['Member']);
  });

  it('drops a corrupt saved user', async () => {
    localStorage.setItem('user', '{not json');
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const restored = TestBed.inject(AccountService);

    expect(await firstValueFrom(restored.currentUser$)).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('keeps every role when the token carries several', async () => {
    service.setCurrentUser({ userId: 1, username: 'admin', token: fakeToken(['Admin', 'Moderator']), roles: [] });

    const user = await firstValueFrom(service.currentUser$);
    expect(user?.roles).toEqual(['Admin', 'Moderator']);
  });

  it('register stores the new user', async () => {
    const result = service.register({ username: 'new', password: 'Passw0rd' }).toPromise();

    http.expectOne(r => r.url.endsWith('account/register')).flush({ username: 'new', token: fakeToken('Member') });
    await result;

    expect((await firstValueFrom(service.currentUser$))?.username).toBe('new');
  });

  it('logout clears the user and local storage', async () => {
    service.setCurrentUser({ userId: 1, username: 'lisa', token: fakeToken('Member'), roles: [] });
    expect(localStorage.getItem('user')).not.toBeNull();

    service.logout();

    expect(await firstValueFrom(service.currentUser$)).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('restore drops an expired user from local storage', async () => {
    const expired = fakeToken('Member').replace(/\.[^.]*\.sig$/, '.' + btoa(JSON.stringify({ unique_name: 'lisa', role: 'Member', exp: 1 })) + '.sig');
    localStorage.setItem('user', JSON.stringify({ userId: 1, username: 'lisa', token: expired, roles: [] }));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const restored = TestBed.inject(AccountService);

    expect(await firstValueFrom(restored.currentUser$)).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('setCurrentUser logs out and throws a clear error when the token cannot be read', async () => {
    service.setCurrentUser({ userId: 1, username: 'lisa', token: fakeToken('Member'), roles: [] });

    expect(() => service.setCurrentUser({ userId: 1, username: 'lisa', token: 'not-a-jwt', roles: [] }))
      .toThrowError('The sign-in token could not be read. Please log in again.');
    expect(await firstValueFrom(service.currentUser$)).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });
});
