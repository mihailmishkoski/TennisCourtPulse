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

  get firstPct(): number {
    return Math.round(this.first * 100);
  }
}
