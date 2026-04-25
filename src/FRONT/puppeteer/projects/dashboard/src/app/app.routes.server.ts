import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server, headers: { 'Cache-Control': 'public, max-age=60, s-maxage=300', 'Vary': 'Host, Cookie' } },
  { path: 'auth/callback', renderMode: RenderMode.Client },
  { path: 'auth/**', renderMode: RenderMode.Server, headers: { 'Cache-Control': 'no-store, private' } },
  { path: 'dashboard/**', renderMode: RenderMode.Client },
  { path: 'admin/**', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Server, headers: { 'Cache-Control': 'public, max-age=60, s-maxage=300', 'Vary': 'Host, Cookie' } },
];
