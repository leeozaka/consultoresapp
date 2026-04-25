# Infrastructure Overview

This folder is the entry point for local Kubernetes manifests and the DigitalOcean Terraform stack.

## Structure

- `infra/terraform`: DigitalOcean + Talos provisioning and cluster add-ons.
- `infra/k8s`: Base manifests and overlays for app deployment.
- `infra/k3d-config.yaml`: Local k3d config for development clusters.
- `infra/localstack-init.sh`: LocalStack bootstrap for dev dependencies (used by Docker Compose).
- Local dev: `make setup`, `make k8s-dev-up`, then `tilt up`.

## Terraform flow

The Terraform stack runs in two phases so the Kubernetes provider has a real kubeconfig before add-ons are applied:

1. Apply with `enable_k8s_addons = false` to create the Talos cluster and write `kubeconfig`/`talosconfig`.
2. Apply with `enable_k8s_addons = true` to install ingress, cert-manager, Argo CD, and optional add-ons.

See `infra/terraform/README.md` for the exact steps and variables.

## Missing points to address

- Secret management: Terraform writes kubeconfig/talosconfig locally. In CI, these should be stored securely and rotated.
- TLS challenge fallback: If `cloudflare_api_token` is empty, no ClusterIssuer is created. Add HTTP-01 issuer if needed.
- Argo CD app definitions: The chart is installed but no Application objects are created to sync this repo.
- Registry namespace lifecycle: The imagePullSecret assumes a namespace exists unless `create_registry_namespace` is true.
- Observability: No metrics/logging stack or alerts are provisioned.
- Backup policy: Droplet/DB backups are optional; verify they are enabled for production.
