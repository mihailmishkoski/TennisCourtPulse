import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { StatInsight } from '@courtpulse/data-access';

/** Renders a labelled group of insight chips (strengths / weaknesses / highlights). */
@Component({
  selector: 'cp-insight-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './insight-list.component.html',
  styleUrl: './insight-list.component.scss',
})
export class InsightListComponent {
  @Input() title = '';
  @Input() insights: StatInsight[] = [];
  @Input() tone: 'good' | 'bad' | 'info' = 'info';
}
