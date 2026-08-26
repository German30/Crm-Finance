import { countTicks, fold, formatNumber, initials, niceTicks } from './format';

describe('format utils', () => {
  it('returns an em dash for values that are not finite', () => {
    expect(formatNumber(Number.NaN)).toBe('—');
    expect(formatNumber(Number.POSITIVE_INFINITY)).toBe('—');
  });

  it('builds a monogram from the first and last name parts', () => {
    expect(initials('Renata Ocampo Salas')).toBe('RS');
    expect(initials('Meridian')).toBe('Me');
    expect(initials('   ')).toBe('—');
  });

  it('folds accents and case so search matches either spelling', () => {
    expect(fold('Peñaloza')).toBe('penaloza');
    expect(fold('Café de Altura')).toBe('cafe de altura');
    expect(fold('Joaquín').includes(fold('joaquin'))).toBe(true);
  });

  describe('countTicks', () => {
    it('never repeats a label on a small axis', () => {
      const ticks = countTicks(3);
      expect(ticks).toEqual([0, 1, 2, 3]);
      expect(new Set(ticks).size).toBe(ticks.length);
    });

    it('always produces at least a zero and a top for an empty axis', () => {
      expect(countTicks(0)).toEqual([0, 1]);
    });

    it('steps in whole numbers on a large axis', () => {
      const ticks = countTicks(40);
      expect(ticks.every((t) => Number.isInteger(t))).toBe(true);
      expect(ticks[0]).toBe(0);
      expect(ticks[ticks.length - 1]).toBeGreaterThanOrEqual(40);
    });
  });

  describe('niceTicks', () => {
    it('lands on round values inside the range', () => {
      const ticks = niceTicks(0, 100, 4);
      expect(ticks).toEqual([0, 25, 50, 75, 100]);
    });

    it('does not emit floating point dust', () => {
      for (const tick of niceTicks(0, 1, 4)) {
        expect(tick.toString().length).toBeLessThan(6);
      }
    });

    it('collapses a zero-width range to a single tick', () => {
      expect(niceTicks(7, 7)).toEqual([7]);
    });
  });
});
