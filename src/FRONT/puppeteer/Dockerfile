# ─── Stage 1: Install dependencies ───────────────────────────────────────────
FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# ─── Stage 2a: Dev stage — deps + source, no build (for Tilt) ───────────────
FROM node:24-alpine AS dev
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# ─── Stage 2b: Build the specified Angular project ──────────────────────────
FROM dev AS build
ARG PROJECT=dashboard
ARG CONFIGURATION=production
RUN npx ng build ${PROJECT} --configuration ${CONFIGURATION}

# ─── Stage 3: Production runtime ────────────────────────────────────────────
FROM node:24-alpine AS runtime
WORKDIR /app

ARG PROJECT=dashboard
ENV NODE_ENV=production
ENV PORT=4000

COPY --from=build /app/dist/${PROJECT} ./dist/app

EXPOSE 4000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:4000/healthz || exit 1

CMD ["node", "dist/app/server/server.mjs"]
