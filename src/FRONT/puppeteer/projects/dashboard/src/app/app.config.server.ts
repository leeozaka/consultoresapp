import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { SSR_API_ORIGIN } from '@consultores/core';

const serverConfig: ApplicationConfig = {
  providers: [
    {
      provide: SSR_API_ORIGIN,
      useFactory: () => process.env['SSR_API_ORIGIN'] ?? '',
    },
    provideServerRendering(withRoutes(serverRoutes)),
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
