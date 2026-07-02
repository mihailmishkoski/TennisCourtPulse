import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { API_BASE_URL } from '@courtpulse/data-access';
import { appRoutes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(appRoutes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    // The .NET API dev URL. CORS is already open to http://localhost:4200.
    { provide: API_BASE_URL, useValue: 'http://localhost:60493' },
  ],
};
