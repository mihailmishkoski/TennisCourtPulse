import { Injectable } from '@angular/core';
import { Observable, timer } from 'rxjs';
import { catchError, map, shareReplay, startWith, switchMap } from 'rxjs/operators';
import { MatchesApiService } from './matches-api.service';
import { LiveMatchSummary } from './models';

interface LiveMatchesState {
  matches: LiveMatchSummary[];
  loading: boolean;
  error: string | null;
}

const POLL_INTERVAL_MS = 15_000;

/**
 * RxJS-first store for the live matches list. A single polling stream refreshes
 * every 15s and is shared across all subscribers (shareReplay), so the list
 * stays live without any component owning a timer.
 */
@Injectable({ providedIn: 'root' })
export class LiveMatchesStore {
  readonly state$: Observable<LiveMatchesState>;

  constructor(private readonly api: MatchesApiService) {
    this.state$ = timer(0, POLL_INTERVAL_MS).pipe(
      switchMap(() =>
        this.api.getLive().pipe(
          map((matches): LiveMatchesState => ({ matches, loading: false, error: null })),
          catchError((err) =>
            [
              {
                matches: [],
                loading: false,
                error: this.describe(err),
              } as LiveMatchesState,
            ]
          )
        )
      ),
      startWith<LiveMatchesState>({ matches: [], loading: true, error: null }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  private describe(err: unknown): string {
    if (err && typeof err === 'object' && 'status' in err && (err as { status: number }).status === 0) {
      return 'Cannot reach the API — is it running on http://localhost:60493?';
    }
    return 'Failed to load live matches.';
  }
}
