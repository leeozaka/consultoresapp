import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';
import { getSsrAllowedHosts } from '@consultores/core';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine({
  allowedHosts: getSsrAllowedHosts(process.env),
});

// ── K8s probes ───────────────────────────────────────────────────────
// Placed before all other middleware so they respond even when the SSR
// engine or static serving is unhealthy.
app.get('/healthz', (_req, res) => res.status(200).send('ok'));
app.get('/readyz', (_req, res) => res.status(200).send('ok'));

// ── Security headers ─────────────────────────────────────────────────
app.use((_req, res, next) => {
  res.setHeader('X-Frame-Options', 'DENY');
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  res.setHeader('X-XSS-Protection', '0'); // disabled in favour of CSP
  if (process.env['NODE_ENV'] === 'production') {
    res.setHeader('Strict-Transport-Security', 'max-age=31536000; includeSubDomains');
  }
  next();
});

// ── Static assets ────────────────────────────────────────────────────
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Static asset requests that weren't resolved by express.static must not
 * fall through to the Angular SSR handler — otherwise the SSR engine renders
 * an HTML page for them, and the browser rejects the response because the
 * MIME type is text/html instead of the expected JS/CSS/etc.
 */
const STATIC_EXT = /\.(?:js|mjs|css|map|ico|json|png|jpe?g|gif|svg|webp|woff2?|ttf|eot)$/i;
app.all(STATIC_EXT, (_req, res) => {
  res.status(404).end();
});

// ── Angular SSR handler ──────────────────────────────────────────────
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

// ── Server startup + graceful shutdown ───────────────────────────────
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  const server = app.listen(port, (error) => {
    if (error) {
      throw error;
    }
    console.log(`Dashboard server listening on http://localhost:${port}`);
  });

  const SHUTDOWN_TIMEOUT_MS = 30_000;

  function shutdown(signal: string) {
    console.log(`Received ${signal}, draining connections…`);
    server.close(() => {
      console.log('All connections drained — exiting.');
      process.exit(0);
    });

    setTimeout(() => {
      console.error('Graceful shutdown timed out — forcing exit.');
      process.exit(1);
    }, SHUTDOWN_TIMEOUT_MS).unref();
  }

  process.on('SIGTERM', () => shutdown('SIGTERM'));
  process.on('SIGINT', () => shutdown('SIGINT'));
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build)
 * or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
