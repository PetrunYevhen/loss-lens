import { Routes } from '@angular/router';
import { authGuard } from './shared/auth/auth.guard';
import { UK } from './shared/i18n/uk';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: `${UK.nav.dashboard} · ${UK.appName}`,
    canActivate: [authGuard],
    loadComponent: () => import('./dashboard/dashboard-page').then((m) => m.DashboardPage),
  },
  {
    path: 'plan',
    title: `${UK.nav.plan} · ${UK.appName}`,
    canActivate: [authGuard],
    loadComponent: () => import('./plan/plan-page').then((m) => m.PlanPage),
  },
  {
    path: 'fpy',
    title: `${UK.nav.fpy} · ${UK.appName}`,
    canActivate: [authGuard],
    loadComponent: () => import('./fpy/fpy-page').then((m) => m.FpyPage),
  },
  {
    path: 'defects',
    title: `${UK.nav.defects} · ${UK.appName}`,
    canActivate: [authGuard],
    loadComponent: () => import('./defects/defects-page').then((m) => m.DefectsPage),
  },
  {
    path: 'login',
    title: `${UK.login.title} · ${UK.appName}`,
    loadComponent: () => import('./login/login-page').then((m) => m.LoginPage),
  },
  { path: '**', redirectTo: '' },
];
