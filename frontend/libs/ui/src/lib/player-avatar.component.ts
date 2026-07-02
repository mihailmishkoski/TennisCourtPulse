import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

/**
 * Player avatar: shows the feed photo when one exists, otherwise a colored
 * initials circle (the api-tennis livescore feed usually omits photos, so the
 * fallback is the common case). Coloured by side to match the rest of the UI.
 */
@Component({
  selector: 'cp-player-avatar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-avatar.component.html',
  styleUrl: './player-avatar.component.scss',
})
export class PlayerAvatarComponent {
  @Input({ required: true }) name!: string;
  @Input() logoUrl: string | null = null;
  @Input() side: 'first' | 'second' = 'first';

  /** Up to two initials from the (possibly doubles) player name. */
  get initials(): string {
    const cleaned = this.name.replace(/\//g, ' ').trim();
    const parts = cleaned.split(/\s+/).filter(Boolean);
    if (parts.length === 0) {
      return '?';
    }
    if (parts.length === 1) {
      return parts[0].substring(0, 2).toUpperCase();
    }
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
}
