import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { EliminationBanner } from './EliminationBanner';

describe('EliminationBanner', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders kingdom name in elimination message', () => {
    render(<EliminationBanner kingdomName="Kingdom Alpha" onFaded={() => {}} />);
    expect(screen.getByText('Kingdom Alpha has been eliminated!')).toBeDefined();
  });

  it('calls onFaded after 5 seconds', () => {
    const onFaded = vi.fn();
    render(<EliminationBanner kingdomName="Kingdom Alpha" onFaded={onFaded} />);

    act(() => {
      vi.advanceTimersByTime(4999);
    });
    expect(onFaded).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(onFaded).toHaveBeenCalled();
  });

  it('starts visible and transitions to hidden', () => {
    render(<EliminationBanner kingdomName="Kingdom Alpha" onFaded={() => {}} />);

    const text = screen.getByText('Kingdom Alpha has been eliminated!');
    const opacityDiv = text.closest('.flex.items-center')!.parentElement!;
    expect(opacityDiv.className).toContain('opacity-100');

    act(() => {
      vi.advanceTimersByTime(4000);
    });
    expect(opacityDiv.className).toContain('opacity-0');
  });
});
