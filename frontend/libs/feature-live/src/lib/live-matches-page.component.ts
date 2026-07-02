import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { LiveMatchesStore, LiveMatchSummary } from '@courtpulse/data-access';
import { MatchCardComponent } from '@courtpulse/ui';

/** Landing page: the live-updating grid of all matches (live + just-finished). */
@Component({
  selector: 'cp-live-matches-page',
  standalone: true,
  imports: [CommonModule, MatchCardComponent],
  templateUrl: './live-matches-page.component.html',
  styleUrl: './live-matches-page.component.scss',
})
export class LiveMatchesPageComponent {
  private readonly store = inject(LiveMatchesStore);
  readonly state$ = this.store.state$;

  live(matches: LiveMatchSummary[]): LiveMatchSummary[] {
    return matches.filter((m) => m.isLive && !m.isFinished);
  }

  finished(matches: LiveMatchSummary[]): LiveMatchSummary[] {
    return matches.filter((m) => m.isFinished);
  }
}
