import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api/api-config';
import { LoginResultDto } from '../models/auth.dto';

export const LOGIN_URL = `${API_BASE}/auth/login`;

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  login(username: string, password: string): Observable<LoginResultDto> {
    return this.http.post<LoginResultDto>(LOGIN_URL, { username, password });
  }
}
