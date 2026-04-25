# ──────────────────────────────────────────────────────────────────────────────
# Tiltfile — local Kubernetes development with hot-reload
#
# Prerequisites:
#   brew install k3d tilt kubectl
#   make k8s-dev-up          # creates k3d cluster + installs NGINX Ingress
#   tilt up                  # starts dev loop (open http://localhost:10350)
#
# This file:
#   1. Applies Kustomize dev overlay (K8s manifests)
#   2. Builds Docker images targeting the SDK/deps stages (not production)
#   3. Syncs source code changes into running pods (no rebuild needed)
#   4. dotnet watch / Angular SSR watch builds inside the pods provide fast feedback
# ──────────────────────────────────────────────────────────────────────────────

# ── Load Kustomize manifests ─────────────────────────────────────────────────
k8s_yaml(kustomize('infra/k8s/overlays/dev'))

# ── API (backend .NET) ───────────────────────────────────────────────────────
docker_build(
    'consultores-api',
    context='.',
    dockerfile='Dockerfile',
    target='build',
    build_args={'BUILD_CONFIGURATION': 'Debug'},
    live_update=[
        sync('src/API/', '/src/API/'),
        run(
            'cd /src && dotnet restore API/Homeless.API/Homeless.API.csproj',
            trigger=['src/API/Homeless.API/Homeless.API.csproj',
                     'src/API/Homeless.Application/Homeless.Application.csproj',
                     'src/API/Homeless.Domain/Homeless.Domain.csproj',
                     'src/API/Homeless.Infrastructure/Homeless.Infrastructure.csproj'],
        ),
    ],
)

# ── Maintenance Worker (.NET) ────────────────────────────────────────────────
docker_build(
    'consultores-worker',
    context='.',
    dockerfile='src/WORKERS/Homeless.Worker/Dockerfile',
    target='build',
    build_args={'BUILD_CONFIGURATION': 'Debug'},
    live_update=[
        sync('src/API/', '/src/API/'),
        sync('src/WORKERS/Homeless.Worker/', '/src/WORKERS/Homeless.Worker/'),
        run(
            'cd /src && dotnet restore WORKERS/Homeless.Worker/Homeless.Worker.csproj',
            trigger=['src/WORKERS/Homeless.Worker/Homeless.Worker.csproj',
                     'src/API/Homeless.Application/Homeless.Application.csproj',
                     'src/API/Homeless.Domain/Homeless.Domain.csproj',
                     'src/API/Homeless.Infrastructure/Homeless.Infrastructure.csproj'],
        ),
    ],
)

# ── Frontend: shared image build, per-app sync ──────────────────────────────
# All frontend apps share the same Dockerfile and node_modules. Each app syncs
# the full workspace so that @consultores/core changes propagate to all apps.
#
# Images stop at Dockerfile stage `dev` (deps + sources, no `ng build`). The dev
# overlay runs one foreground `ng build`, then `ng build --watch` (watch may clear dist/ briefly),
# waits for server.mjs, then `exec node --watch-path …` (see dashboard patch).

def frontend_app(name):
    """Register a frontend app: Docker image build + Tilt resource."""
    docker_build(
        'consultores-' + name,
        context='src/FRONT/puppeteer',
        dockerfile='src/FRONT/puppeteer/Dockerfile',
        target='dev',
        live_update=[
            sync('src/FRONT/puppeteer/src/', '/app/src/'),
            sync('src/FRONT/puppeteer/projects/', '/app/projects/'),
            sync('src/FRONT/puppeteer/public/', '/app/public/'),
            run(
                'cd /app && npm ci --ignore-scripts',
                trigger=['src/FRONT/puppeteer/package.json',
                         'src/FRONT/puppeteer/package-lock.json'],
            ),
        ],
    )

frontend_app('dashboard')
frontend_app('tenant-demo')
frontend_app('tenant-acme')

# ── Resource grouping in Tilt UI ─────────────────────────────────────────────
k8s_resource('api',                labels=['app'],      port_forwards=['5001:8080'])
k8s_resource('worker',             labels=['app'])
k8s_resource('dashboard',          labels=['frontend'], port_forwards=['4000:4000'])
k8s_resource('tenant-demo',        labels=['frontend'], port_forwards=['4001:4000'])
k8s_resource('tenant-acme',        labels=['frontend'], port_forwards=['4002:4000'])
k8s_resource('postgres',           labels=['infra'],    port_forwards=['5432:5432'])
k8s_resource('seq',                labels=['infra'],    port_forwards=['8888:80'])
k8s_resource('localstack',         labels=['infra'],    port_forwards=['4566:4566'])

# ── Resource dependencies (startup order) ────────────────────────────────────
k8s_resource('api', resource_deps=['postgres'])
k8s_resource('worker', resource_deps=['postgres'])
k8s_resource('dashboard', resource_deps=['api'])
k8s_resource('tenant-demo', resource_deps=['api'])
k8s_resource('tenant-acme', resource_deps=['api'])
