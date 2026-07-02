import { Routes } from '@angular/router';

/**
 * Lazy routes into the feature libraries. Each feature is a self-contained lib
 * exposing a standalone page component.
 */
export const appRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('@courtpulse/feature-live').then((m) => m.LiveMatchesPageComponent),
  },
  {
    path: 'matches/:id',
    loadComponent: () =>
      import('@courtpulse/feature-match').then((m) => m.MatchDetailPageComponent),
  },
  { path: '**', redirectTo: '' },
];
