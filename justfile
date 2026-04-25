set dotenv-load
set shell := ["bash", "-uc"]

registry := env_var_or_default("REGISTRY", "registry.digitalocean.com/consultores")
tag := env_var_or_default("TAG", "latest")
slugs := env_var_or_default("SLUGS", "")
overlay := env_var_or_default("OVERLAY", "dev")
namespace := env_var_or_default("NAMESPACE", "consultores")
secrets_file := env_var_or_default("SECRETS_FILE", "")
app := env_var_or_default("APP", "")
kind := env_var_or_default("KIND", "")
name := env_var_or_default("NAME", "")
frontend_specs := "dashboard/consultores-dashboard tenant-demo/consultores-tenant-demo tenant-acme/consultores-tenant-acme"

alias up := k8s-dev-up
alias stop := k8s-dev-stop
alias down := k8s-dev-stop
alias destroy := k8s-dev-down
alias status := k8s-dev-status
alias logs := k8s-dev-logs
alias build := docker-build-prod

# List available tasks
default:
  @just --list

# List available tasks
help:
  @just --list

# Add base local domains to /etc/hosts
setup:
  #!/usr/bin/env bash
  set -euo pipefail
  echo "Setting up /etc/hosts for local dev..."
  for host in consultor.localhost demo.consultor.localhost acme.consultor.localhost; do
    if grep -qE "^127\.0\.0\.1[[:space:]]+$host$" /etc/hosts 2>/dev/null; then
      echo "  = $host already present"
    else
      echo "127.0.0.1  $host" | sudo tee -a /etc/hosts >/dev/null
      echo "  + Added $host"
    fi
  done
  echo "Done. App: http://consultor.localhost (after just k8s-dev-up && just dev)"

# Add extra tenant subdomains: SLUGS="acme beta" just setup-hosts
setup-hosts: setup
  #!/usr/bin/env bash
  set -euo pipefail
  slugs="{{slugs}}"
  if [ -z "$slugs" ]; then
    echo "No extra SLUGS provided."
    exit 0
  fi
  for raw in $slugs; do
    slug="$(printf '%s' "$raw" | tr '[:upper:]' '[:lower:]' | tr -d ' ')"
    test -z "$slug" && continue
    host="$slug.consultor.localhost"
    if grep -qE "^127\.0\.0\.1[[:space:]]+$host$" /etc/hosts 2>/dev/null; then
      echo "  = $host already present"
    else
      echo "127.0.0.1  $host" | sudo tee -a /etc/hosts >/dev/null
      echo "  + Added $host"
    fi
  done

# Forward Stripe webhooks to the local API
stripe-webhook:
  stripe listen --forward-to http://consultor.localhost/api/webhooks/stripe

# Start Tilt hot-reload loop
dev:
  tilt up

# Create or reuse local k3d cluster and install NGINX Ingress
k8s-dev-up:
  #!/usr/bin/env bash
  set -euo pipefail
  for cmd in docker k3d kubectl helm; do
    command -v "$cmd" >/dev/null 2>&1 || { echo "ERROR: '$cmd' is required."; exit 1; }
  done
  if k3d cluster list 2>/dev/null | grep -q consultores-dev; then
    echo "[k8s-dev] Cluster 'consultores-dev' already exists - reusing."
  else
    echo "[k8s-dev] Creating k3d cluster..."
    k3d cluster create --config infra/k3d-config.yaml
  fi
  kubectl config use-context k3d-consultores-dev
  if helm list -n ingress-nginx 2>/dev/null | grep -q ingress-nginx; then
    echo "[k8s-dev] NGINX Ingress already installed - skipping."
  else
    helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 2>/dev/null || true
    helm repo update ingress-nginx
    helm install ingress-nginx ingress-nginx/ingress-nginx \
      --namespace ingress-nginx \
      --create-namespace \
      --set controller.hostPort.enabled=true \
      --set controller.service.type=ClusterIP \
      --set controller.watchIngressWithoutClass=true \
      --set controller.allowSnippetAnnotations=true \
      --wait --timeout 120s
  fi
  kubectl apply -f infra/k8s/base/namespace.yaml
  kubectl wait --namespace ingress-nginx \
    --for=condition=ready pod \
    --selector=app.kubernetes.io/component=controller \
    --timeout=120s
  echo "[k8s-dev] Ready. Use 'just dev' and, when needed, 'just stripe-webhook'."

# Start an existing k3d dev cluster
k8s-dev-start:
  k3d cluster start consultores-dev
  kubectl config use-context k3d-consultores-dev

# Stop local k3d dev cluster
k8s-dev-stop:
  k3d cluster stop consultores-dev

# Delete local k3d dev cluster
k8s-dev-down:
  k3d cluster delete consultores-dev

# Show local cluster and consultores pod status
k8s-dev-status:
  @k3d cluster list 2>/dev/null || echo "No k3d clusters found"
  @echo ""
  @kubectl get pods -n consultores -o wide 2>/dev/null || echo "Cluster not running or namespace does not exist"

# Follow all consultores workload logs
k8s-dev-logs:
  kubectl logs -n consultores -l app.kubernetes.io/part-of=consultores -f --all-containers --prefix

# Render dev overlay
k8s-dev-preview:
  kubectl kustomize infra/k8s/overlays/dev

# Apply dev overlay manually
k8s-dev-apply:
  kubectl apply -k infra/k8s/overlays/dev

# Apply overlay from OVERLAY, default dev
k8s-apply:
  kubectl apply -k infra/k8s/overlays/{{overlay}}

# Apply staging overlay
k8s-apply-staging:
  @just deploy staging

# Apply production overlay
k8s-apply-prod:
  @just deploy production

# Apply a named overlay
deploy target:
  kubectl apply -k infra/k8s/overlays/{{target}}

# Diff a named overlay
deploy-diff target:
  #!/usr/bin/env bash
  set -uo pipefail
  kubectl diff -k infra/k8s/overlays/{{target}}
  status=$?
  if [ "$status" -eq 1 ]; then exit 0; fi
  exit "$status"

# Diff overlay from OVERLAY, default dev
k8s-diff:
  @just deploy-diff {{overlay}}

# Apply Kubernetes secrets from SECRETS_FILE
k8s-secrets:
  #!/usr/bin/env bash
  set -euo pipefail
  file="{{secrets_file}}"
  if [ -z "$file" ]; then
    echo "ERROR: provide SECRETS_FILE=infra/k8s/overlays/production/secrets.yaml"
    exit 2
  fi
  kubectl apply -f "$file"

# Show common resources in NAMESPACE
k8s-resources:
  kubectl get deploy,pod,svc,ingress,configmap,job,hpa,pdb -n {{namespace}} 2>/dev/null || kubectl get all -n {{namespace}}

# Show recent events in NAMESPACE
k8s-events:
  kubectl get events -n {{namespace}} --sort-by=.lastTimestamp

# Describe resource from KIND, NAME, and NAMESPACE
k8s-describe:
  #!/usr/bin/env bash
  set -euo pipefail
  if [ -z "{{kind}}" ] || [ -z "{{name}}" ]; then
    echo "ERROR: provide KIND and NAME, e.g. KIND=pod NAME=api-... just k8s-describe"
    exit 2
  fi
  kubectl describe "{{kind}}" "{{name}}" -n "{{namespace}}"

# Follow logs for APP label in NAMESPACE
k8s-logs:
  #!/usr/bin/env bash
  set -euo pipefail
  if [ -z "{{app}}" ]; then
    echo "ERROR: provide APP, e.g. APP=consultores-api just k8s-logs"
    exit 2
  fi
  kubectl logs -n "{{namespace}}" -l "app.kubernetes.io/name={{app}}" -f --all-containers --prefix

# Port-forward Redis
k8s-redis-port-forward:
  kubectl -n consultores port-forward svc/redis 6379:6379

# Install Headlamp locally
k8s-headlamp-install:
  helm repo add headlamp https://kubernetes-sigs.github.io/headlamp/ || true
  helm repo update
  kubectl create namespace headlamp --dry-run=client -o yaml | kubectl apply -f -
  helm upgrade --install headlamp headlamp/headlamp --namespace headlamp
  @echo "Production Headlamp RBAC is managed by Terraform in infra/terraform/02-platform."

# Port-forward Headlamp
k8s-headlamp-port-forward:
  kubectl -n headlamp port-forward svc/headlamp 8080:80

# Print Headlamp viewer token
k8s-headlamp-token:
  kubectl create token headlamp-viewer -n headlamp

# Port-forward RedisInsight
k8s-redisinsight-port-forward:
  kubectl -n consultores port-forward svc/redisinsight 5540:5540

tf stack action:
  cd infra/terraform/{{stack}} && terraform {{action}}

tf-cluster-init:
  @just tf 01-cluster init

tf-cluster-plan:
  @just tf 01-cluster plan

tf-cluster-apply:
  @just tf 01-cluster apply

tf-cluster-destroy:
  @just tf 01-cluster destroy

tf-cluster-output:
  @just tf 01-cluster output

tf-platform-init:
  @just tf 02-platform init

tf-platform-plan:
  @just tf 02-platform plan

tf-platform-apply:
  @just tf 02-platform apply

tf-platform-destroy:
  @just tf 02-platform destroy

# Apply cluster first, then platform
tf-apply-all:
  @just tf-cluster-apply
  @just tf-platform-apply

# Destroy platform first, then cluster
tf-destroy-all:
  @just tf-platform-destroy
  @just tf-cluster-destroy

tf-init:
  @just tf-cluster-init
  @just tf-platform-init

tf-plan:
  @just tf-cluster-plan
  @just tf-platform-plan

tf-apply:
  @just tf-apply-all

tf-destroy:
  @just tf-destroy-all

tf-output:
  @just tf-cluster-output

# Export kubeconfig from Terraform output
talos-kubeconfig:
  mkdir -p ~/.kube
  cd infra/terraform/01-cluster && terraform output -raw kubeconfig > ~/.kube/config-consultores
  @echo "Run: export KUBECONFIG=~/.kube/config-consultores"

# Export talosconfig from Terraform output
talosconfig:
  mkdir -p ~/.talos
  cd infra/terraform/01-cluster && terraform output -raw talosconfig > ~/.talos/config

# Build all deployable Docker images
image-build configuration='production' image_tag='latest':
  #!/usr/bin/env bash
  set -euo pipefail
  registry="{{registry}}"
  for image in consultores-api consultores-worker; do
    dockerfile="Dockerfile"
    if [ "$image" = "consultores-worker" ]; then dockerfile="src/WORKERS/Homeless.Worker/Dockerfile"; fi
    docker build -f "$dockerfile" --build-arg BUILD_CONFIGURATION=Release -t "$registry/$image:{{image_tag}}" .
  done
  for spec in {{frontend_specs}}; do
    proj="${spec%%/*}"
    image="${spec##*/}"
    docker build -f src/FRONT/puppeteer/Dockerfile \
      --build-arg PROJECT="$proj" \
      --build-arg CONFIGURATION="{{configuration}}" \
      -t "$registry/$image:{{image_tag}}" \
      src/FRONT/puppeteer
  done

docker-build-prod:
  @just image-build production {{tag}}

docker-build-staging:
  @just image-build staging staging

# Push all deployable Docker images
image-push image_tag='latest':
  #!/usr/bin/env bash
  set -euo pipefail
  registry="{{registry}}"
  docker push "$registry/consultores-api:{{image_tag}}"
  docker push "$registry/consultores-worker:{{image_tag}}"
  for spec in {{frontend_specs}}; do
    docker push "$registry/${spec##*/}:{{image_tag}}"
  done

docker-push:
  @just image-push {{tag}}

docker-push-staging:
  @just image-push staging
