import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { mergeMap, of, throwError, timer } from 'rxjs';
import { API_BASE, USE_MOCK_API } from '../api-config';
import { handleMockRequest } from './mock-backend';

const LATENCY_MS = 250;

/** Answers API calls from the mock backend while USE_MOCK_API is on. */
export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!USE_MOCK_API || !req.url.startsWith(API_BASE)) {
    return next(req);
  }

  const query: Record<string, string> = {};
  for (const key of req.params.keys()) {
    query[key] = req.params.get(key) ?? '';
  }

  const lineIds = (req.headers.get('X-Demo-LineIds') ?? '').split(',').filter(Boolean);
  const role = req.headers.get('X-Demo-Role') ?? 'manager';

  return timer(LATENCY_MS).pipe(
    mergeMap(() => {
      const response = handleMockRequest({
        path: req.url.slice(API_BASE.length),
        query,
        role,
        lineIds,
      });
      return response.status < 400
        ? of(new HttpResponse({ status: response.status, body: response.body, url: req.url }))
        : throwError(
            () =>
              new HttpErrorResponse({
                status: response.status,
                url: req.url,
                error: response.body,
              }),
          );
    }),
  );
};
