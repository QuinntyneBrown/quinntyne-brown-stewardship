export function safeDestination(value: string | null): string {
  return value &&
    /^\/(curriculum|sessions|notes|modules\/[a-zA-Z0-9-]+)$/.test(value)
    ? value
    : "/curriculum";
}
