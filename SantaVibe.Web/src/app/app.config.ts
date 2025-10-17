import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { Configuration } from '@api/configuration';
import { BASE_PATH } from '@api/variables';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: BASE_PATH,
      useValue: 'https://localhost:5001'
    },
    {
      provide: Configuration,
      useFactory: () => new Configuration({
        basePath: 'https://localhost:5001'
      })
    }
  ]
};
