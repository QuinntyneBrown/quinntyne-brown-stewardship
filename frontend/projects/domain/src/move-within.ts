// Moves one item of an ordered list by a step, returning a new list; the caller announces and stores the arrangement.
export function moveWithin<T>(items: readonly T[], index: number, delta: number): T[] {
  const next = [...items]; const target = index + delta;
  if (target < 0 || target >= next.length) return next;
  const [item] = next.splice(index, 1); next.splice(target, 0, item); return next;
}
