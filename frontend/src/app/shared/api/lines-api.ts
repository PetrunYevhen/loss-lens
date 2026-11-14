import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { LineDto } from '../models/line.dto';
import { RoleState } from '../role/role-state';
import { API_BASE } from './api-config';

@Injectable({ providedIn: 'root' })
export class LinesApi {
  private readonly http = inject(HttpClient);

  /** Lines the current role may see. */
  getLines(): Observable<LineDto[]> {
    return this.http.get<LineDto[]>(`${API_BASE}/lines`);
  }

  /** Demo only: the role picker needs every line whatever the current role is. */
  getAllLines(): Observable<LineDto[]> {
    return this.http.get<LineDto[]>(`${API_BASE}/lines`, { headers: { 'X-Demo-Role': 'manager' } });
  }
}

/** Lines visible to the current role, reloaded when the role changes. */
@Injectable({ providedIn: 'root' })
export class LinesState {
  private readonly api = inject(LinesApi);
  private readonly role = inject(RoleState);

  readonly resource = rxResource({
    params: () => ({ role: this.role.role(), lineIds: this.role.lineIds().join(',') }),
    stream: () => this.api.getLines(),
  });

  readonly list = computed(() => this.resource.value() ?? []);
}
