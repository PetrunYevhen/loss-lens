import { Routes } from '@angular/router';
import { UK } from './shared/i18n/uk';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: `${UK.nav.dashboard} · ${UK.appName}`,
    loadComponent: () => import('./dashboard/dashboard-page').then((m) => m.DashboardPage),
  },
  {
    path: 'plan',
    title: `${UK.nav.plan} · ${UK.appName}`,
    loadComponent: () => import('./plan/plan-page').then((m) => m.PlanPage),
  },
  {
    path: 'fpy',
    title: `${UK.nav.fpy} · ${UK.appName}`,
    loadComponent: () => import('./fpy/fpy-page').then((m) => m.FpyPage),
  },
  {
    path: 'defects',
    title: `${UK.nav.defects} · ${UK.appName}`,
    loadComponent: () => import('./defects/defects-page').then((m) => m.DefectsPage),
  },
  {
    path: 'roles',
    title: `${UK.nav.roles} · ${UK.appName}`,
    loadComponent: () => import('./shared/role/role-selector').then((m) => m.RoleSelector),
  },
  { path: '**', redirectTo: '' },
];
