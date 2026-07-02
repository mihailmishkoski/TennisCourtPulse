import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, forkJoin, of, timer } from 'rxjs';
import { catchError, map, shareReplay, switchMap } from 'rxjs/operators';
import {
  MatchDetail,
  MatchSummary,
  MatchTimeline,
  MatchesApiService,
  MomentumPoint,
  PlayerStatistic,
  TimelinePoint,
  WinProbability,
} from '@courtpulse/data-access';
import {
  InsightListComponent,
  MomentumChartComponent,
  PlayerAvatarComponent,
  StatComparisonComponent,
  WinProbabilityBarComponent,
} from '@courtpulse/ui';

interface KeyMoment {
  label: string;
  player: string;
  set: number;
  game: number;
}

interface MatchVm {
  detail: MatchDetail | null;
  momentum: MomentumPoint[];
  stats: PlayerStatistic[];
  summary: MatchSummary | null;
  winProbability: WinProbability | null;
  timeline: MatchTimeline | null;
  keyMoments: KeyMoment[];
}

type TabKey = 'overview' | 'stats' | 'summary' | 'timeline';

/** Detail page: every derived feature for a single match, on tabs. */
@Component({
  selector: 'cp-match-detail-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MomentumChartComponent,
    WinProbabilityBarComponent,
    StatComparisonComponent,
    InsightListComponent,
    PlayerAvatarComponent,
  ],
  templateUrl: './match-detail-page.component.html',
  styleUrl: './match-detail-page.component.scss',
})
export class MatchDetailPageComponent implements OnInit {
  /** Bound from the route param via withComponentInputBinding(). */
  @Input() id!: string;

  private readonly api = inject(MatchesApiService);

  readonly tabs: { key: TabKey; label: string }[] = [
    { key: 'overview', label: 'Overview' },
    { key: 'stats', label: 'Statistics' },
    { key: 'summary', label: 'Summary' },
    { key: 'timeline', label: 'Timeline' },
  ];
  activeTab: TabKey = 'overview';

  vm$!: Observable<MatchVm>;

  ngOnInit(): void {
    const id = this.id;
    // Refresh every 15s so a live match keeps updating; a finished one just re-reads.
    this.vm$ = timer(0, 15_000).pipe(
      switchMap(() =>
        forkJoin({
          detail: this.api.getById(id).pipe(catchError(() => of(null))),
          momentum: this.api.getMomentum(id).pipe(catchError(() => of([] as MomentumPoint[]))),
          stats: this.api.getStatistics(id).pipe(catchError(() => of([] as PlayerStatistic[]))),
          summary: this.api.getSummary(id).pipe(catchError(() => of(null))),
          winProbability: this.api.getWinProbability(id).pipe(catchError(() => of(null))),
          timeline: this.api.getTimeline(id).pipe(catchError(() => of(null))),
        })
      ),
      map((r) => ({ ...r, keyMoments: this.deriveKeyMoments(r.momentum, r.detail) })),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  /** Map the feed's "First"/"Second" side to the actual player name. */
  sideName(side: string, detail: MatchDetail): string {
    return side === 'First' ? detail.firstPlayer : detail.secondPlayer;
  }

  /** Render the running score with a colon, e.g. "15 - 0" → "15:0". */
  formatScore(score: string | null): string {
    return score ? score.replace(' - ', ':') : '·';
  }

  pointTitle(p: TimelinePoint): string {
    if (p.isMatchPoint) {
      return 'Match point';
    }
    if (p.isSetPoint) {
      return 'Set point';
    }
    if (p.isBreakPoint) {
      return 'Break point';
    }
    return `Point ${p.pointNumber}`;
  }

  /** Top momentum swings, derived client-side from the momentum stream. */
  private deriveKeyMoments(momentum: MomentumPoint[], detail: MatchDetail | null): KeyMoment[] {
    if (!detail) {
      return [];
    }
    return [...momentum]
      .sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta))
      .slice(0, 5)
      .map((m) => ({
        label: m.reason ?? 'momentum swing',
        player: m.beneficiary === 'First' ? detail.firstPlayer : detail.secondPlayer,
        set: m.setNumber,
        game: m.gameNumber,
      }));
  }
}
