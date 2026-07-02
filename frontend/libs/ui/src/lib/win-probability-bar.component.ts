import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

/** Live win-probability as a split bar (First = teal, Second = amber). */
@Component({
  selector: 'cp-win-probability-bar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './win-probability-bar.component.html',
  styleUrl: './win-probability-bar.component.scss',
})
export class WinProbabilityBarComponent {
  @Input() first = 0.5;
  @Input() firstName = 'First';
  @Input() secondName = 'Second';

  /** Bar width for the first player, floored/capped so both colours stay visible. */
  get firstPct(): number {
    return Math.round(this.first * 100);
  }

  get firstLabel(): string {
    return this.format(this.first);
  }

  get secondLabel(): string {
    return this.format(1 - this.first);
  }

  /**
   * A live match is never a certainty, so a near-decided probability that rounds
   * to 0/100 is shown as "<1%" / ">99%". An exact 0 or 1 (a finished match) still
   * reads as 0% / 100%.
   */
  private format(p: number): string {
    const pct = p * 100;
    if (pct > 0 && pct < 1) {
      return '<1%';
    }
    if (pct < 100 && pct > 99) {
      return '>99%';
    }
    return `${Math.round(pct)}%`;
  }
}
