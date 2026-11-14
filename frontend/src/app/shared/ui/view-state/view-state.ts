import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  contentChild,
  input,
  output,
  Resource,
  TemplateRef,
} from '@angular/core';
import { UK } from '../../i18n/uk';

export type ViewStatus = 'loading' | 'error' | 'forbidden' | 'empty' | 'ready';

function httpStatus(error: unknown): number | null {
  const candidate = error as { status?: unknown; cause?: { status?: unknown } } | undefined;
  const status = candidate?.status ?? candidate?.cause?.status;
  return typeof status === 'number' ? status : null;
}

/** Maps a resource to the loading / error / forbidden / empty / ready state of a view. */
export function viewStatus<T>(
  resource: Resource<T | undefined>,
  isEmpty: (value: T) => boolean = () => false,
): ViewStatus {
  switch (resource.status()) {
    case 'idle':
    case 'loading':
      return 'loading';
    case 'error':
      return httpStatus(resource.error()) === 403 ? 'forbidden' : 'error';
    default:
      return resource.hasValue() && !isEmpty(resource.value() as T) ? 'ready' : 'empty';
  }
}

/**
 * Shows loading / empty / forbidden / error, or the content once ready.
 * Put the content in an <ng-template>: it is only created when ready, so charts
 * never initialize on an element that has no size yet.
 */
@Component({
  selector: 'app-view-state',
  imports: [NgTemplateOutlet],
  templateUrl: './view-state.html',
  styleUrl: './view-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ViewState {
  readonly status = input.required<ViewStatus>();
  readonly retry = output<void>();
  protected readonly content = contentChild(TemplateRef);
  protected readonly t = UK.viewState;
}
