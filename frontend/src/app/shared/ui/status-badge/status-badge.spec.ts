import { TestBed } from '@angular/core/testing';
import { UK } from '../../i18n/uk';
import { RagStatus } from '../../models/rag-status.dto';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  function render(status: RagStatus): HTMLElement {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it.each([
    ['green', UK.status.green],
    ['amber', UK.status.amber],
    ['red', UK.status.red],
    ['unknown', UK.status.unknown],
  ] as const)('shows the text "%s" -> %s next to the icon', (status, label) => {
    const element = render(status);

    expect(element.textContent).toContain(label);
    expect(element.querySelector('svg')).not.toBeNull();
  });

  it('exposes the status as a class for styling', () => {
    expect(render('red').classList).toContain('badge--red');
  });
});
