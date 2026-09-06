export function safeDestination(value: string | null): string {
  return value &&
    /^\/(curriculum|sessions(?:\/[a-zA-Z0-9-]+)?|notes(?:\/[a-zA-Z0-9-]+)?|modules\/[a-zA-Z0-9-]+)(?:\?(?:moduleId|sessionId|promptId)=[a-zA-Z0-9-]+(?:&(?:moduleId|sessionId|promptId)=[a-zA-Z0-9-]+)*)?$/.test(value)
    ? value
    : "/curriculum";
}
