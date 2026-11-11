import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';
import { JwtInterceptor } from './jwt.interceptor';
import { ErrorInterceptor } from './error.interceptor';

describe('JwtInterceptor', () => {
  let currentUser: BehaviorSubject<User | null>;
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    currentUser = new BehaviorSubject<User | null>(null);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
        { provide: AccountService, useValue: { currentUser$: currentUser.asObservable() } }
      ]
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('adds the Bearer token when a user is signed in', () => {
    currentUser.next({ userId: 1, username: 'lisa', token: 'abc.def.ghi', roles: ['Member'] });

    http.get('/api/anything').subscribe();

    const req = controller.expectOne('/api/anything');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    req.flush({});
  });

  it('sends no Authorization header when anonymous', () => {
    http.get('/api/anything').subscribe();

    const req = controller.expectOne('/api/anything');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });
});

describe('ErrorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let toastr: jasmine.SpyObj<ToastrService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    toastr = jasmine.createSpyObj('ToastrService', ['error']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
        { provide: ToastrService, useValue: toastr },
        { provide: Router, useValue: router }
      ]
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('passes successful responses through untouched', () => {
    let body: unknown;
    http.get('/api/ok').subscribe(b => (body = b));

    controller.expectOne('/api/ok').flush({ fine: true });

    expect(body).toEqual({ fine: true });
    expect(toastr.error).not.toHaveBeenCalled();
  });

  it('toasts on 401 and rethrows the error', () => {
    let error: any;
    http.get('/api/secret').subscribe({ error: e => (error = e) });

    controller.expectOne('/api/secret').flush('nope', { status: 401, statusText: 'Unauthorized' });

    expect(toastr.error).toHaveBeenCalledWith('Unauthorized', '401');
    expect(error.status).toBe(401);
  });

  it('flattens model-state validation errors on 400 and rethrows them', () => {
    let error: unknown;
    http.post('/api/x', {}).subscribe({ error: e => (error = e) });

    controller.expectOne('/api/x').flush(
      { errors: { Username: ['Required'], Password: ['Too short', 'Too weak'] } },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(error).toEqual(['Required', 'Too short', 'Too weak']);
  });

  it('redirects to /not-found on 404 and rethrows', () => {
    let error: any;
    http.get('/api/missing').subscribe({ error: e => (error = e) });

    controller.expectOne('/api/missing').flush('', { status: 404, statusText: 'Not Found' });

    expect(router.navigateByUrl).toHaveBeenCalledWith('/not-found');
    expect(error.status).toBe(404);
  });

  it('redirects to /server-error on 500 and rethrows', () => {
    let error: any;
    http.get('/api/boom').subscribe({ error: e => (error = e) });

    controller.expectOne('/api/boom').flush({ message: 'x' }, { status: 500, statusText: 'Server Error' });

    expect(router.navigateByUrl.calls.mostRecent().args[0]).toBe('/server-error');
    expect(error.status).toBe(500);
  });
});
