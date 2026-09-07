// The maximum lengths the API enforces, carried on every authoring read so the client states them without a constant of its own.
export interface AuthoringLimits {
  key: number; title: number; summary: number; reading: number; effortEstimate: number; practiceStep: number; prompt: number;
}
