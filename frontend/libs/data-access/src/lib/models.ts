/** DTO mirrors of the CourtPulse API (camelCased by System.Text.Json). */

export interface LiveMatchSummary {
  id: string;
  tournament: string;
  firstPlayer: string;
  firstPlayerLogo: string | null;
  secondPlayer: string;
  secondPlayerLogo: string | null;
  status: string | null;
  isLive: boolean;
  isFinished: boolean;
  finalResult: string | null;
  /** Cumulative momentum lead (First − Second). */
  momentumDifferential: number;
}

export interface SetScore {
  setNumber: number;
  first: number;
  second: number;
}

export interface MatchDetail {
  id: string;
  tournament: string;
  round: string | null;
  eventType: string | null;
  firstPlayer: string;
  firstPlayerLogo: string | null;
  secondPlayer: string;
  secondPlayerLogo: string | null;
  status: string | null;
  isLive: boolean;
  isFinished: boolean;
  finalResult: string | null;
  winner: string | null;
  sets: SetScore[];
}

export interface MomentumPoint {
  setNumber: number;
  gameNumber: number;
  pointNumber: number;
  beneficiary: 'First' | 'Second';
  delta: number;
  reason: string | null;
  firstCumulative: number;
  secondCumulative: number;
  firstEwma: number;
  secondEwma: number;
}

export interface StatItem {
  type: string;
  name: string;
  value: string;
  won: number | null;
  total: number | null;
}

export interface PlayerStatistic {
  playerId: string;
  playerName: string;
  stats: StatItem[];
}

export interface StatInsight {
  metric: string;
  summary: string;
  playerValue: number | null;
  opponentValue: number | null;
  weight: number;
}

export interface PlayerSummary {
  playerKey: number;
  strengths: StatInsight[];
  weaknesses: StatInsight[];
  highlights: StatInsight[];
}

export interface MatchSummary {
  first: PlayerSummary;
  second: PlayerSummary;
  headlines: string[];
}

export interface WinProbability {
  first: number;
  second: number;
}

export interface TimelinePoint {
  pointNumber: number;
  /** Running game score at this point, e.g. "15 - 30". */
  score: string | null;
  isBreakPoint: boolean;
  isSetPoint: boolean;
  isMatchPoint: boolean;
}

export interface TimelineGame {
  gameNumber: number;
  server: string;
  serveWinner: string | null;
  points: TimelinePoint[];
}

export interface TimelineSet {
  setNumber: number;
  games: TimelineGame[];
}

export interface MatchTimeline {
  matchId: string;
  sets: TimelineSet[];
}
