import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { jwtInterceptor } from './jwt-interceptor';
import { AuthService } from '../services/auth.service';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  it('leaves the request untouched when there is no token', () => {
    http.get('/api/User').subscribe();
    const req = controller.expectOne('/api/User');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('attaches the bearer token once one is set', () => {
    auth.setToken('abc.def.ghi');
    http.get('/api/User').subscribe();
    const req = controller.expectOne('/api/User');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    req.flush([]);
  });

  it('does not overwrite an Authorization header the caller already set', () => {
    auth.setToken('abc.def.ghi');
    http.get('/api/User', { headers: { Authorization: 'Basic other' } }).subscribe();
    const req = controller.expectOne('/api/User');
    expect(req.request.headers.get('Authorization')).toBe('Basic other');
    req.flush([]);
  });
});
