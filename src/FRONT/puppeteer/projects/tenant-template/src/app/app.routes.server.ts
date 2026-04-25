import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: '**', renderMode: RenderMode.Server, headers: { 'Cache-Control': 'public, max-age=60, s-maxage=300', 'Vary': 'Host, Cookie' } },
];
