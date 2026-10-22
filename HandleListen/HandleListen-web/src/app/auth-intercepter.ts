import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth';

const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/refresh', '/api/auth/register'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);
    const token = auth.token();

    const authedReq = token
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

    const isAuthEndpoint = AUTH_ENDPOINTS.some(path => req.url.includes(path));

    return next(authedReq).pipe(
        catchError((err: HttpErrorResponse) => {
            // An expired access token shows up as a 401 on an otherwise normal request. Rather
            // than let that fail silently (or surface as a confusing error), try the refresh
            // token once and transparently retry - the user should never notice their session
            // was about to expire during normal, active use.
            if (err.status !== 401 || isAuthEndpoint) {
                return throwError(() => err);
            }

            return auth.refreshAccessToken().pipe(
                switchMap(response => {
                    const retried = req.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } });
                    return next(retried);
                }),
                catchError(refreshErr => {
                    // The refresh token itself is gone too (truly expired, or revoked) - a full
                    // reload is the only reliable way to clear stale state like the SignalR
                    // connection, matching what manually refreshing the page already fixed.
                    auth.forceLogoutAndReload();
                    return throwError(() => refreshErr);
                })
            );
        })
    );
};
