import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE } from '../api/api-config';
import { RoleState } from './role-state';

/** Adds the demo role headers to API requests. A request that sets X-Demo-Role itself is left alone. */
export const demoRoleInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API_BASE) || req.headers.has('X-Demo-Role')) {
    return next(req);
  }

  const role = inject(RoleState);
  return next(
    req.clone({
      setHeaders: {
        'X-Demo-Role': role.role(),
        'X-Demo-LineIds': role.lineIds().join(','),
      },
    }),
  );
};
