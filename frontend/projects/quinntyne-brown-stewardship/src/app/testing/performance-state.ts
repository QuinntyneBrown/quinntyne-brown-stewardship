export interface PerformanceState {
  responses: Record<string, { body: unknown; compressedBytes: number; headerBytes: number; serverMilliseconds: number }>;
  calls: string[];
  latencyMs: number;
  downloadBytesPerMs: number;
}

declare global {
  interface Window { __stewardshipPerformance: PerformanceState; }
}
