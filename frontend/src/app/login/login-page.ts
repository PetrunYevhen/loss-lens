import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthApi } from '../shared/auth/auth-api';
import { AuthState } from '../shared/auth/auth-state';
import { UK } from '../shared/i18n/uk';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  protected readonly t = UK.login;
  private readonly api = inject(AuthApi);
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(event: Event, username: string, password: string): void {
    event.preventDefault();
    const name = username.trim();
    if (!name || !password || this.busy()) {
      this.error.set(this.t.required);
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.api.login(name, password).subscribe({
      next: (result) => {
        this.auth.start(result, name.toLowerCase());
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(this.messageFor(error));
      },
    });
  }

  /** Only paths inside the app are accepted, so a crafted link cannot send the user to another site. */
  private returnUrl(): string {
    const value = this.route.snapshot.queryParamMap.get('returnUrl');
    return value?.startsWith('/') && !value.startsWith('//') ? value : '/';
  }

  private messageFor(error: unknown): string {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    if (status === 401) {
      return this.t.invalid;
    }
    if (status === 429) {
      return this.t.tooManyAttempts;
    }
    return status === 0 ? this.t.unreachable : this.t.failed;
  }
}
