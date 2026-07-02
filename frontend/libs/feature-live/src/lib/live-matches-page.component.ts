import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { LiveMatchesStore, LiveMatchSummary } from '@courtpulse/data-access';
import { MatchRowComponent } from '@courtpulse/ui';

/** Landing page: the live-updating grid of all matches (live + just-finished). */
@Component({
  selector: 'cp-live-matches-page',
  standalone: true,
  imports: [CommonModule, MatchRowComponent],
  templateUrl: './live-matches-page.component.html',
  styleUrl: './live-matches-page.component.scss',
})
export class LiveMatchesPageComponent {
  private readonly store = inject(LiveMatchesStore);
  readonly state$ = this.store.state$;

  /** Gender buckets rendered as separate rows, in display order. */
  readonly genderGroups: { key: 'Men' | 'Women' | 'Other'; label: string }[] = [
    { key: 'Men', label: 'Men (ATP · Challenger · ITF)' },
    { key: 'Women', label: 'Women (WTA · ITF)' },
    { key: 'Other', label: 'Other' },
  ];

  live(matches: LiveMatchSummary[]): LiveMatchSummary[] {
    return matches.filter((m) => m.isLive && !m.isFinished);
  }

  finished(matches: LiveMatchSummary[]): LiveMatchSummary[] {
    return matches.filter((m) => m.isFinished);
  }

  /** Live matches for one gender bucket. */
  liveByGender(matches: LiveMatchSummary[], gender: 'Men' | 'Women' | 'Other'): LiveMatchSummary[] {
    return this.live(matches).filter((m) => m.gender === gender);
  }
}
