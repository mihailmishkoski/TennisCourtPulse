import { InjectionToken } from '@angular/core';

/** Base URL of the CourtPulse .NET API. Provided in app.config. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
