# ConsultoresAPP

Consultores is a multi-tenant white-label SaaS for real estate agencies. Each agency gets a branded public portal, usually at `slug.consultor.app` or a custom domain, and a private dashboard for managing tenants, agents, properties, billing, media, and platform settings.

This repository is Kubernetes-first. Local development, staging, and production all run through Kubernetes manifests and the same deployment shape. Running the application directly with `dotnet run`, `ng serve`, `npm start`, or Docker Compose is not a supported or regularly tested workflow anymore.

## What Is In This Repo

```text
Homeless.sln
├── src/API/
│   ├── Homeless.Domain
│   ├── Homeless.Application
│   ├── Homeless.Infrastructure
│   ├── Homeless.API
│   └── Homeless.Tests/
├── src/FRONT/puppeteer/
│   ├── projects/core/
│   ├── projects/dashboard/
│   ├── projects/tenant-template/
│   ├── projects/tenant-demo/
│   └── projects/tenant-acme/
├── src/WORKERS/
├── infra/
│   ├── k8s/
│   ├── terraform/
│   └── k3d-config.yaml
├── Tiltfile
├── justfile
└── Makefile
```

`justfile` is the main task runner. `Makefile` is only a compatibility wrapper for existing `make ...` muscle memory.

## Core Features

- Multi-tenant real estate SaaS with host-based tenant resolution.
- Dashboard app for platform and agency management.
- Independent tenant portal apps for white-label storefronts.
- Shared Angular library under `@consultores/core` for auth, HTTP, tenant utilities, models, and shared UI.
- CQRS backend with MediatR commands, queries, validators, authorization, audit, feature gates, and resilience behaviors.
- OpenIddict OIDC server using Authorization Code + PKCE.
- HttpOnly-cookie-oriented frontend auth flow.
- PostgreSQL persistence with EF Core and tenant query filters.
- Redis-backed caching for entitlement and platform data.
- Async image processing pipeline using worker services and object storage.
- Stripe webhook integration path for subscription/billing workflows.
- Kubernetes overlays for `dev`, `staging`, and `production`.
- DigitalOcean + Talos Terraform stack for remote clusters.
- Local Kubernetes development through k3d, NGINX Ingress, and Tilt.

## Known Weaknesses

- Kubernetes is required for supported local development, so the first setup is heavier than a direct process runner.
- Some workflows still rely on local CLI tools and kube context state. If `kubectl`, `helm`, `k3d`, `tilt`, `just`, or Docker are missing, the runner will fail early.
- Secrets are intentionally not committed. You must provide overlay-specific `secrets.yaml` files or apply secrets manually.
- Staging is an overlay/namespace scenario, not a completely separate cluster by default.
- Argo CD is provisioned as platform infrastructure, but Application definitions and full GitOps automation are still not the primary deploy path.
- Observability exists through logs, metrics endpoints, and local Seq, but production-grade alerts, dashboards, and backup policies still need hardening.
- Direct `.NET` and Angular commands may still work for narrow debugging or tests, but they are not the supported way to run the application.

## Required Local Tools

Install these before working on the app locally:

```bash
brew install just k3d kubectl helm tilt
```

You also need:

- Docker Desktop or another Docker runtime compatible with k3d.
- Stripe CLI if you need local webhook forwarding.
- Terraform and Talos tooling if you manage remote infrastructure.
- `doctl` if you build/push images to DigitalOcean Container Registry.

Check available repo tasks:

```bash
just --list
```

The legacy wrapper also works:

```bash
make help
```

## Local Development

Local development mirrors deployment: k3d provides the cluster, Kubernetes manifests define the app resources, and Tilt builds/syncs workloads.

1. Add local hostnames:

```bash
just setup
```

Add extra tenant hostnames when needed:

```bash
SLUGS="beta agency-x" just setup-hosts
```

2. Create or reuse the local Kubernetes cluster:

```bash
just k8s-dev-up
```

3. Start the hot-reload loop:

```bash
just dev
```

4. Open the local services:

| Service | URL |
|---|---|
| Dashboard | `http://consultor.localhost` |
| Tenant demo | `http://demo.consultor.localhost` |
| Tenant acme | `http://acme.consultor.localhost` |
| Tilt UI | `http://localhost:10350` |
| Seq | `http://localhost:8888` |
| Postgres | `localhost:5432` |

Local dev uses **plain HTTP** only (`ingress` has no TLS). Open **`http://consultor.localhost`**. If the browser uses **https** or “HTTPS-Only” mode, you can get **PR_END_OF_FILE** / secure connection errors — switch to **http** or add an exception for `*.localhost`.

The **dev** ingress only routes **`consultor.localhost`** and **`*.consultor.localhost`**. Hostnames like **`itcorretor.com`** are for **production**; if you map them to `127.0.0.1` you will connect to nginx but get the **default 404** (no matching rule).

Useful local commands:

```bash
just status
just logs
just k8s-dev-preview
just k8s-resources
just k8s-events
```

Stop or delete the local cluster:

```bash
just stop
just destroy
```

## Stripe Webhooks

Stripe webhook forwarding remains a first-class local workflow:

```bash
just stripe-webhook
```

Compatibility:

```bash
make stripe-webhook
```

This forwards Stripe events to:

```text
http://consultor.localhost/api/webhooks/stripe
```

## Kubernetes Resource Management

Render or apply overlays:

```bash
just k8s-dev-preview
just k8s-apply
OVERLAY=staging just k8s-diff
just k8s-apply-staging
just k8s-apply-prod
```

Inspect resources:

```bash
just k8s-resources
NAMESPACE=consultores-staging just k8s-resources
KIND=pod NAME=<pod-name> just k8s-describe
APP=consultores-api just k8s-logs
```

Apply secrets:

```bash
SECRETS_FILE=infra/k8s/overlays/production/secrets.yaml just k8s-secrets
```

Port-forward local tools:

```bash
just k8s-headlamp-install
just k8s-headlamp-port-forward
just k8s-headlamp-token
just k8s-redisinsight-port-forward
just k8s-redis-port-forward
```

## Deployment

Deployment has three layers:

1. Build and push images.
2. Provision or update infrastructure with Terraform.
3. Apply Kubernetes overlays.

### Images

Production images:

```bash
TAG=<version-or-sha> just docker-build-prod
TAG=<version-or-sha> just docker-push
```

Staging images:

```bash
just docker-build-staging
just docker-push-staging
```

The current frontend image set is:

- `consultores-dashboard`
- `consultores-tenant-demo`
- `consultores-tenant-acme`

### Infrastructure

Terraform is split into two stacks:

- `infra/terraform/01-cluster`: DigitalOcean network, Talos nodes, database, registry, DNS, kubeconfig/talosconfig outputs.
- `infra/terraform/02-platform`: cluster add-ons such as NGINX Ingress, cert-manager, Argo CD, and Headlamp.

Normal order:

```bash
just tf-cluster-init
just tf-cluster-plan
just tf-cluster-apply

just talos-kubeconfig
export KUBECONFIG=~/.kube/config-consultores

just tf-platform-init
just tf-platform-plan
just tf-platform-apply
```

Shortcut:

```bash
just tf-apply-all
```

Destroy order is reversed:

```bash
just tf-destroy-all
```

See `infra/terraform/README.md` for profile sizing, HCP Terraform variables, and DigitalOcean details.

### Kubernetes Overlays

Staging:

```bash
OVERLAY=staging just k8s-diff
just k8s-apply-staging
```

Production:

```bash
OVERLAY=production just k8s-diff
just k8s-apply-prod
```

Make sure `KUBECONFIG` points to the target cluster before applying staging or production.

## Architecture Notes

### Multi-Tenancy

The platform uses a single database with logical isolation:

1. `TenantResolutionMiddleware` resolves the tenant from the `Host` header.
2. `ITenantContext` carries the tenant through the scoped request.
3. EF Core global query filters append tenant constraints.
4. JWT/session claims bind authenticated users to the tenant context.
5. Roles separate `SuperAdmin`, `TenantAdmin`, and `Agent`.

### Backend Flow

Commands flow through the MediatR pipeline:

```text
Logging -> Authorization -> FeatureGate -> Audit -> Validation -> Resilience -> Handler
```

Queries use read-optimized paths where possible. Commands own validation, authorization, auditing, feature entitlement checks, and resilience.

### Frontend Flow

The Angular workspace is split into a shared `core` library, one dashboard app, and tenant app projects. Kubernetes ingress routes the root dashboard domain and tenant subdomains to separate services.

### Image Processing

Image upload is asynchronous:

```text
API receives upload
-> stores raw object
-> publishes work item
-> worker resizes/converts image
-> processed object and property metadata are updated
```

## Tests And Diagnostics

Application runtime is Kubernetes-first. Tests and one-off diagnostics may still use project-native tooling, but they do not replace the supported local runtime path.

Backend tests:

```bash
dotnet test
dotnet test src/API/Homeless.Tests/Homeless.UnitTests
dotnet test src/API/Homeless.Tests/Homeless.IntegrationTests
```

Frontend tests:

```bash
cd src/FRONT/puppeteer
npx ng test
```

Use these for validation, not for running the full application stack.

## Important Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `R2__AccountId` | Cloudflare R2 account ID |
| `R2__AccessKey` / `R2__SecretKey` | Cloudflare R2 credentials |
| `R2__BucketName` | Object storage bucket |
| `R2__PublicBaseUrl` | Public asset base URL |
| `Seq__ServerUrl` | Seq log server URL |
| `SuperAdmin__Email` / `SuperAdmin__Password` | Initial seeded super admin |
| `OpenIddict__Issuer` | OIDC issuer URL |
| `TFE_ADDRESS` / `TFE_TOKEN` | Terraform Cloud/HCP Terraform access |

## What Not To Do

- Do not use Docker Compose as the normal runtime path.
- Do not run the full app through local `dotnet run`, `ng serve`, or `npm start` and assume it matches dev/staging/prod.
- Do not apply staging or production overlays without checking `KUBECONFIG`.
- Do not commit real secrets, kubeconfigs, talosconfigs, Terraform state, or generated credentials.
