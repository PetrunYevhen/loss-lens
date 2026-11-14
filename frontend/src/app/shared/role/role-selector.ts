import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { LinesApi } from '../api/lines-api';
import { UK } from '../i18n/uk';
import { ViewState, viewStatus } from '../ui/view-state/view-state';
import { RoleState, UserRole } from './role-state';

interface RoleOption {
  role: UserRole;
  title: string;
  text: string;
  scope: string;
}

/** Demo role picker. Not secure: the role is sent as headers and enforced by the backend. */
@Component({
  selector: 'app-role-selector',
  imports: [ViewState],
  templateUrl: './role-selector.html',
  styleUrl: './role-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleSelector {
  protected readonly t = UK.rolePicker;
  private readonly roleState = inject(RoleState);
  private readonly router = inject(Router);
  private readonly linesApi = inject(LinesApi);

  protected readonly options: readonly RoleOption[] = [
    {
      role: 'operator',
      title: UK.roles.operator,
      text: UK.rolePicker.operatorText,
      scope: UK.rolePicker.operatorScope,
    },
    {
      role: 'supervisor',
      title: UK.roles.supervisor,
      text: UK.rolePicker.supervisorText,
      scope: UK.rolePicker.supervisorScope,
    },
    {
      role: 'manager',
      title: UK.roles.manager,
      text: UK.rolePicker.managerText,
      scope: UK.rolePicker.managerScope,
    },
  ];

  protected readonly lines = rxResource({ stream: () => this.linesApi.getAllLines() });
  protected readonly linesState = computed(() => viewStatus(this.lines));

  protected readonly role = signal<UserRole>(this.roleState.role());
  protected readonly lineIds = signal<string[]>(this.roleState.lineIds());

  protected readonly needsLines = computed(() => this.role() !== 'manager');
  protected readonly canSubmit = computed(() => !this.needsLines() || this.lineIds().length > 0);
  protected readonly linesHint = computed(() => {
    switch (this.role()) {
      case 'manager':
        return this.t.linesHintManager;
      case 'operator':
        return this.t.linesHintOperator;
      default:
        return this.t.linesHintSupervisor;
    }
  });

  protected selectRole(role: UserRole): void {
    this.role.set(role);
    if (role === 'operator') {
      this.lineIds.update((ids) => ids.slice(0, 1));
    }
  }

  protected toggleLine(lineId: string): void {
    if (this.role() === 'operator') {
      this.lineIds.set([lineId]);
      return;
    }
    this.lineIds.update((ids) =>
      ids.includes(lineId) ? ids.filter((id) => id !== lineId) : [...ids, lineId],
    );
  }

  protected submit(): void {
    if (!this.canSubmit()) {
      return;
    }
    this.roleState.select(this.role(), this.lineIds());
    void this.router.navigateByUrl('/');
  }
}
