import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { AuthState } from '../auth/auth-state';
import { ShiftDto } from '../models/shift.dto';
import { API_BASE } from './api-config';

@Injectable({ providedIn: 'root' })
export class ShiftsApi {
  private readonly http = inject(HttpClient);

  getShifts(): Observable<ShiftDto[]> {
    return this.http.get<ShiftDto[]>(`${API_BASE}/shifts`);
  }
}

/** Shifts for the filter bar. Reloaded when another user signs in. */
@Injectable({ providedIn: 'root' })
export class ShiftsState {
  private readonly api = inject(ShiftsApi);
  private readonly auth = inject(AuthState);

  readonly resource = rxResource({
    // No user, no request: the resource stays idle until someone signs in.
    params: () => this.auth.username() ?? undefined,
    stream: () => this.api.getShifts(),
  });

  readonly list = computed(() => this.resource.value() ?? []);
}
