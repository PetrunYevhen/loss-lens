import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthState } from './shared/auth/auth-state';
import { UK } from './shared/i18n/uk';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  exact?: boolean;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly t = UK;
  protected readonly auth = inject(AuthState);
  private readonly router = inject(Router);

  protected readonly items: readonly NavItem[] = [
    {
      label: UK.nav.dashboard,
      path: '/',
      exact: true,
      icon: 'M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z',
    },
    { label: UK.nav.plan, path: '/plan', icon: 'M4 20V10M10 20V4M16 20v-7M22 20H2' },
    { label: UK.nav.fpy, path: '/fpy', icon: 'M3 17l5-6 4 3 8-9' },
    {
      label: UK.nav.defects,
      path: '/defects',
      icon: 'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16zM12 8v5M12 16v.1',
    },
  ];

  protected readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role ? UK.roles[role] : '';
  });

  protected logout(): void {
    this.auth.clear();
    void this.router.navigateByUrl('/login');
  }
}
