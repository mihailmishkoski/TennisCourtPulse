import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';
import { PlayerStatistic } from '@courtpulse/data-access';

interface CompRow {
  name: string;
  leftValue: string;
  rightValue: string;
  leftPct: number;
  rightPct: number;
}

/** Head-to-head comparison of the key stats for the two players. */
@Component({
  selector: 'cp-stat-comparison',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-comparison.component.html',
  styleUrl: './stat-comparison.component.scss',
})
export class StatComparisonComponent implements OnChanges {
  @Input() players: PlayerStatistic[] = [];

  rows: CompRow[] = [];

  private static readonly METRICS = [
    '1st serve points won',
    '2nd serve points won',
    'Service Points Won',
    'Return Points Won',
    'Aces',
    'Double Faults',
    'Winners',
    'Unforced errors',
    'Break Points Converted',
  ];

  ngOnChanges(): void {
    if (this.players.length < 2) {
      this.rows = [];
      return;
    }

    const [left, right] = this.players;
    this.rows = StatComparisonComponent.METRICS.map((name) => {
      const l = left.stats.find((s) => s.name === name);
      const r = right.stats.find((s) => s.name === name);
      if (!l && !r) {
        return null;
      }

      const leftNum = this.numeric(l);
      const rightNum = this.numeric(r);
      const total = leftNum + rightNum;
      return {
        name,
        leftValue: l?.value ?? '—',
        rightValue: r?.value ?? '—',
        leftPct: total > 0 ? (leftNum / total) * 100 : 50,
        rightPct: total > 0 ? (rightNum / total) * 100 : 50,
      } as CompRow;
    }).filter((r): r is CompRow => r !== null);
  }

  private numeric(item: { value: string; won: number | null; total: number | null } | undefined): number {
    if (!item) {
      return 0;
    }
    if (item.won !== null && item.total !== null && item.total > 0) {
      return (item.won / item.total) * 100;
    }
    const parsed = parseFloat(item.value.replace('%', ''));
    return Number.isFinite(parsed) ? parsed : 0;
  }
}
