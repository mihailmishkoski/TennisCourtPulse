import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';
import { MomentumPoint } from '@courtpulse/data-access';

/**
 * Momentum graph as inline SVG (no chart library). Plots the cumulative
 * differential (First − Second) as a filled area — above the midline means the
 * First player is ahead — and overlays the EWMA "surge meter" as a dashed line.
 */
@Component({
  selector: 'cp-momentum-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './momentum-chart.component.html',
  styleUrl: './momentum-chart.component.scss',
})
export class MomentumChartComponent implements OnChanges {
  @Input() points: MomentumPoint[] = [];
  @Input() firstName = 'First';
  @Input() secondName = 'Second';

  readonly width = 600;
  readonly height = 200;
  readonly mid = 100;

  cumulativeLine = '';
  ewmaLine = '';
  areaPath = '';

  ngOnChanges(): void {
    const n = this.points.length;
    if (n < 2) {
      return;
    }

    const cumulative = this.points.map((p) => p.firstCumulative - p.secondCumulative);
    const ewma = this.points.map((p) => p.firstEwma - p.secondEwma);
    const maxAbs = Math.max(1, ...cumulative.map(Math.abs), ...ewma.map(Math.abs));
    const padY = 12;

    const x = (i: number): number => (i / (n - 1)) * this.width;
    const y = (v: number): number => this.mid - (v / maxAbs) * (this.height / 2 - padY);

    this.cumulativeLine = cumulative.map((v, i) => `${x(i).toFixed(1)},${y(v).toFixed(1)}`).join(' ');
    this.ewmaLine = ewma.map((v, i) => `${x(i).toFixed(1)},${y(v).toFixed(1)}`).join(' ');

    const top = cumulative.map((v, i) => `${x(i).toFixed(1)},${y(v).toFixed(1)}`).join(' L ');
    this.areaPath = `M 0,${this.mid} L ${top} L ${this.width},${this.mid} Z`;
  }
}
