// Match status classification mirroring the backend FixtureStatus enum
// (Azulyoro.Domain.Enums.FixtureStatus). The API returns the enum NAME as a
// string, e.g. "NotStarted" | "SecondHalf" | "Finished".

export type MatchState = "live" | "finished" | "scheduled";

const LIVE = new Set([
  "firsthalf",
  "halftime",
  "secondhalf",
  "extratime",
  "breaktime",
  "penalty",
]);

const FINISHED = new Set([
  "finished",
  "cancelled",
  "abandoned",
  "awarded",
  "walkover",
]);

export function classifyStatus(status: string): MatchState {
  const s = status.toLowerCase();
  if (LIVE.has(s)) return "live";
  if (FINISHED.has(s)) return "finished";
  return "scheduled";
}

export const isLiveStatus = (status: string) => classifyStatus(status) === "live";
