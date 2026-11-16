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

export type ViewStatus = 'loading' | 'error' | 'forbidden' | 'invalid' | 'empty' | 'ready';

function httpStatus(error: unknown): number | null {
  const candidate = error as { status?: unknown; cause?: { status?: unknown } } | undefined;
  const status = candidate?.status ?? candidate?.cause?.status;
  return typeof status === 'number' ? status : null;
}

function statusOfError(status: number | null): ViewStatus {
  if (status === 403) {
    return 'forbidden';
  }
  return status === 400 || status === 404 ? 'invalid' : 'error';
}

/**
 * Maps a resource to the loading / error / forbidden / invalid / empty / ready state of a view.
 * 403 is "not your data", 400 and 404 mean the filters name something that does not exist (retrying cannot help),
 * everything else is a failed request that may work next time.
 */
export function viewStatus<T>(
  resource: Resource<T | undefined>,
  isEmpty: (value: T) => boolean = () => false,
): ViewStatus {
  switch (resource.status()) {
    case 'idle':
    case 'loading':
      return 'loading';
    case 'error':
      return statusOfError(httpStatus(resource.error()));
    default:
      return resource.hasValue() && !isEmpty(resource.value() as T) ? 'ready' : 'empty';
  }
}

/**
 * Shows loading / empty / forbidden / invalid / error, or the content once ready.
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
