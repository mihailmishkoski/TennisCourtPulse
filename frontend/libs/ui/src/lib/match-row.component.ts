import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LiveMatchSummary } from '@courtpulse/data-access';
import { PlayerAvatarComponent } from './player-avatar.component';

/**
 * Compact Flashscore-style row: two stacked player lines with small avatars,
 * per-set game columns, and (when live) the current game points. Much denser
 * than the card grid.
 */
@Component({
  selector: 'cp-match-row',
  standalone: true,
  imports: [CommonModule, RouterLink, PlayerAvatarComponent],
  templateUrl: './match-row.component.html',
  styleUrl: './match-row.component.scss',
})
export class MatchRowComponent {
  @Input({ required: true }) match!: LiveMatchSummary;

  get statusLabel(): string {
    return this.match.isFinished ? 'Fin.' : this.match.status || 'Live';
  }

  /** Current game points as [first, second], e.g. "15 - 0" → ['15','0']. */
  get points(): [string, string] | null {
    if (!this.match.isLive || !this.match.currentGameScore) {
      return null;
    }
    const parts = this.match.currentGameScore.split(' - ');
    return parts.length === 2 ? [parts[0], parts[1]] : null;
  }

  /** True when the first (top) player currently holds the momentum. */
  get momentumFirst(): boolean {
    return this.match.momentumDifferential > 0;
  }

  /** Magnitude of the momentum lead as a 0–46% half-width of the centered bar. */
  get momentumPct(): number {
    return 46 * Math.abs(Math.tanh(this.match.momentumDifferential / 12));
  }

  /** Left edge (%) of the fill: grows leftward from centre for the first player. */
  get momentumLeft(): number {
    return this.momentumFirst ? 50 - this.momentumPct : 50;
  }

  /** Winner side for a finished match, derived from the sets-won finalResult "2 - 0". */
  get winner(): 'first' | 'second' | null {
    if (!this.match.isFinished || !this.match.finalResult) {
      return null;
    }
    const [a, b] = this.match.finalResult.split(' - ').map((n) => parseInt(n, 10));
    if (isNaN(a) || isNaN(b) || a === b) {
      return null;
    }
    return a > b ? 'first' : 'second';
  }
}
