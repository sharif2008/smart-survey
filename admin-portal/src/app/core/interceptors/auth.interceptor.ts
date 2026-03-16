import { HttpInterceptorFn, HttpErrorResponse, HttpContextToken } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth.service';

/** When true, do not attach Authorization header (e.g. public survey view). 403 from these requests is not treated as auth error. */
export const SKIP_AUTH_FOR_PUBLIC_SURVEY = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const skipAuth = req.context.get(SKIP_AUTH_FOR_PUBLIC_SURVEY);
  if (!skipAuth) {
    const token = auth.getToken();
    if (token) {
      req = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` }
      });
    }
  }
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (skipAuth && err?.status === 403) {
        // Public survey 403 = survey not available; let the caller handle it, don't trigger auth error
        return throwError(() => err);
      }
      if (err?.status === 401) {
        const message =
          (typeof err?.error === 'object' && err?.error && 'message' in err?.error
            ? (err.error as { message?: string }).message
            : null) ??
          (typeof err?.error === 'string' ? err.error : null) ??
          'Your session has expired or you are not logged in.';
        auth.handleAuthError(message);
      } else if (err?.status === 403) {
        const message =
          (typeof err?.error === 'object' && err?.error && 'message' in err?.error
            ? (err.error as { message?: string }).message
            : null) ??
          (typeof err?.error === 'string' ? err.error : null) ??
          'You do not have permission to perform this action.';
        auth.handleAuthError(message);
      }
      return throwError(() => err);
    })
  );
};
