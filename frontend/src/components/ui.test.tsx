import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Chili, Highlighted, Stars, fmt, metaLine, price } from './ui';

describe('formatting helpers', () => {
  it('price shows one $ per level and nothing when unknown', () => {
    expect(price(0)).toBe('');
    expect(price(1)).toBe('$');
    expect(price(3)).toBe('$$$');
  });

  it('fmt shows one decimal, or a dash for no rating', () => {
    expect(fmt(null)).toBe('—');
    expect(fmt(4)).toBe('4.0');
    expect(fmt(4.26)).toBe('4.3');
  });

  it('metaLine joins non-empty parts with a dot', () => {
    expect(metaLine('Kaze Robata', '', 'Japanese', undefined, '$$$')).toBe('Kaze Robata · Japanese · $$$');
    expect(metaLine('', undefined)).toBe('');
  });
});

describe('<Highlighted>', () => {
  it('wraps each phrase in <mark>, case-insensitively', () => {
    const { container } = render(
      <Highlighted text="The LAKSA was great but too salty." phrases={['laksa', 'too salty']} />,
    );
    const marks = [...container.querySelectorAll('mark')].map((m) => m.textContent);
    expect(marks).toEqual(['LAKSA', 'too salty']);
    expect(container.textContent).toBe('The LAKSA was great but too salty.');
  });

  it('treats regex characters in phrases literally', () => {
    const { container } = render(<Highlighted text="Only $16 (huge bowl)" phrases={['$16 (huge']} />);
    expect(container.querySelector('mark')?.textContent).toBe('$16 (huge');
  });

  it('renders plain text when there is nothing to highlight', () => {
    const { container } = render(<Highlighted text="Nice place" phrases={[]} />);
    expect(container.querySelector('mark')).toBeNull();
    expect(container.textContent).toBe('Nice place');
  });
});

describe('<Chili> and <Stars>', () => {
  it('shows "Not spicy" for level 0', () => {
    const { container } = render(<Chili level={0} />);
    expect(container.textContent).toBe('Not spicy');
  });

  it('shows filled and empty stars', () => {
    const { container } = render(<Stars n={3} />);
    expect(container.textContent).toBe('★★★☆☆');
  });
});
