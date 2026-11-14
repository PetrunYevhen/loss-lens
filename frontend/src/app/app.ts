import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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

  protected readonly roleItem: NavItem = {
    label: UK.nav.roles,
    path: '/roles',
    icon: 'M12 4a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM4 21c1-4 4-6 8-6s7 2 8 6',
  };
}
