import { Resource } from '@angular/core';
import { viewStatus } from './view-state';

function failed(status: number | undefined): Resource<number | undefined> {
  return {
    status: () => 'error',
    error: () => (status === undefined ? new Error('network') : { status }),
    hasValue: () => false,
    value: () => undefined,
  } as unknown as Resource<number | undefined>;
}

function loaded(value: number | undefined): Resource<number | undefined> {
  return {
    status: () => 'resolved',
    error: () => undefined,
    hasValue: () => value !== undefined,
    value: () => value,
  } as unknown as Resource<number | undefined>;
}

describe('viewStatus', () => {
  it('says forbidden for 403', () => {
    expect(viewStatus(failed(403))).toBe('forbidden');
  });

  it('says invalid for 400 and 404, because retrying cannot help', () => {
    expect(viewStatus(failed(400))).toBe('invalid');
    expect(viewStatus(failed(404))).toBe('invalid');
  });

  it('says error for a server failure or a lost connection', () => {
    expect(viewStatus(failed(500))).toBe('error');
    expect(viewStatus(failed(0))).toBe('error');
    expect(viewStatus(failed(undefined))).toBe('error');
  });

  it('is ready with a value, empty when the value counts as empty', () => {
    expect(viewStatus(loaded(5))).toBe('ready');
    expect(viewStatus(loaded(0), (n) => n === 0)).toBe('empty');
    expect(viewStatus(loaded(undefined))).toBe('empty');
  });
});
