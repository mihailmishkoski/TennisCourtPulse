import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LiveMatchSummary } from '@courtpulse/data-access';
import { PlayerAvatarComponent } from './player-avatar.component';

/** A single match tile for the live list. Links through to the detail page. */
@Component({
  selector: 'cp-match-card',
  standalone: true,
  imports: [CommonModule, RouterLink, PlayerAvatarComponent],
  templateUrl: './match-card.component.html',
  styleUrl: './match-card.component.scss',
})
export class MatchCardComponent {
  @Input({ required: true }) match!: LiveMatchSummary;

  /** Map the (unbounded) differential to a 0–100 marker position with a soft squash. */
  get markerPct(): number {
    return 50 + 45 * Math.tanh(this.match.momentumDifferential / 12);
  }

  /** Live in-progress game score in colon format, e.g. "15:0". */
  get gameScore(): string | null {
    return this.match.currentGameScore ? this.match.currentGameScore.replace(' - ', ':') : null;
  }
}
