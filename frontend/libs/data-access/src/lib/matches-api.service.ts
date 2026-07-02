import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import {
  LiveMatchSummary,
  MatchDetail,
  MatchSummary,
  MatchTimeline,
  MomentumPoint,
  PlayerStatistic,
  WinProbability,
} from './models';

/**
 * Thin typed wrapper over the CourtPulse read API. Every method maps 1:1 to one
 * of the seven endpoints — all of which are our own derived data over the single
 * upstream get_livescore feed.
 */
@Injectable({ providedIn: 'root' })
export class MatchesApiService {
  private readonly base: string;

  constructor(private readonly http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.base = `${baseUrl.replace(/\/$/, '')}/api/matches`;
  }

  getLive(): Observable<LiveMatchSummary[]> {
    return this.http.get<LiveMatchSummary[]>(`${this.base}/live`);
  }

  getById(id: string): Observable<MatchDetail> {
    return this.http.get<MatchDetail>(`${this.base}/${id}`);
  }

  getTimeline(id: string): Observable<MatchTimeline> {
    return this.http.get<MatchTimeline>(`${this.base}/${id}/timeline`);
  }

  getMomentum(id: string): Observable<MomentumPoint[]> {
    return this.http.get<MomentumPoint[]>(`${this.base}/${id}/momentum`);
  }

  getStatistics(id: string): Observable<PlayerStatistic[]> {
    return this.http.get<PlayerStatistic[]>(`${this.base}/${id}/statistics`);
  }

  getSummary(id: string): Observable<MatchSummary> {
    return this.http.get<MatchSummary>(`${this.base}/${id}/summary`);
  }

  getWinProbability(id: string): Observable<WinProbability> {
    return this.http.get<WinProbability>(`${this.base}/${id}/win-probability`);
  }
}
