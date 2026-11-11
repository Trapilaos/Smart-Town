import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, HttpClient, HttpContext, HttpErrorResponse, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
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

    expect(toastr.error).toHaveBeenCalledWith('nope', '401');
    expect(error.status).toBe(401);
  });

  it('toasts a generic message on 401 when the body is empty', () => {
    http.get('/api/secret').subscribe({ error: () => {} });

    controller.expectOne('/api/secret').flush('', { status: 401, statusText: 'Unauthorized' });

    expect(toastr.error).toHaveBeenCalledWith('Please log in to continue', '401');
  });

  it('toasts each model-state validation error on 400 and rethrows the HttpErrorResponse', () => {
    let error: any;
    http.post('/api/x', {}).subscribe({ error: e => (error = e) });

    controller.expectOne('/api/x').flush(
      { errors: { Username: ['Required'], Password: ['Too short', 'Too weak'] } },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(toastr.error).toHaveBeenCalledTimes(3);
    expect(toastr.error).toHaveBeenCalledWith('Required', 'Error');
    expect(toastr.error).toHaveBeenCalledWith('Too short', 'Error');
    expect(toastr.error).toHaveBeenCalledWith('Too weak', 'Error');
    expect(error instanceof HttpErrorResponse).toBeTrue();
    expect(error.status).toBe(400);
  });

  it('toasts each Identity error description on 400 and rethrows the HttpErrorResponse', () => {
    let error: any;
    http.post('/api/account/register', {}).subscribe({ error: e => (error = e) });

    controller.expectOne('/api/account/register').flush(
      [
        { code: 'PasswordRequiresLower', description: "Passwords must have at least one lowercase ('a'-'z')." },
        { code: 'PasswordRequiresDigit', description: "Passwords must have at least one digit ('0'-'9')." }
      ],
      { status: 400, statusText: 'Bad Request' }
    );

    expect(toastr.error).toHaveBeenCalledTimes(2);
    expect(toastr.error).toHaveBeenCalledWith("Passwords must have at least one lowercase ('a'-'z').", 'Error');
    expect(toastr.error).toHaveBeenCalledWith("Passwords must have at least one digit ('0'-'9').", 'Error');
    expect(error instanceof HttpErrorResponse).toBeTrue();
    expect(error.status).toBe(400);
  });

  it('does not toast on 400 when the request sets SKIP_ERROR_TOAST, but still rethrows', () => {
    let error: any;
    http.post('/api/account/register', {}, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .subscribe({ error: e => (error = e) });

    controller.expectOne('/api/account/register').flush('Username is taken!', { status: 400, statusText: 'Bad Request' });

    expect(toastr.error).not.toHaveBeenCalled();
    expect(error.status).toBe(400);
  });

  it('does not toast or navigate on 404, and rethrows so the component can decide', () => {
    let error: any;
    http.get('/api/missing').subscribe({ error: e => (error = e) });

    controller.expectOne('/api/missing').flush('', { status: 404, statusText: 'Not Found' });

    expect(toastr.error).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(error.status).toBe(404);
  });

  it('toasts a friendly message on 500, does not navigate, and rethrows', () => {
    let error: any;
    http.get('/api/boom').subscribe({ error: e => (error = e) });

    controller.expectOne('/api/boom').flush({ message: 'x' }, { status: 500, statusText: 'Server Error' });

    expect(toastr.error).toHaveBeenCalledWith('Something went wrong on our side. Please try again.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(error.status).toBe(500);
  });
});
