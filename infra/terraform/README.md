# Terraform deployment

This stack uses HCP Terraform as the remote backend defined in [backend.tf](backend.tf) and deploys infrastructure from [infra/terraform](.) to DigitalOcean.

## GitHub Actions

The repository workflow lives in [.github/workflows/terraform-infra.yml](../../.github/workflows/terraform-infra.yml).

### Required GitHub secret

- `TF_API_TOKEN`: HCP Terraform team or user token with access to the `consultores-prod` workspace.

### Recommended GitHub environment

Create a `production` environment and require reviewers before the apply job can continue.

## HCP Terraform workspace variables

Store provider and infrastructure values in the `consultores-prod` workspace instead of GitHub secrets.

### Sensitive

- `do_token`

### Profile, sizing, and pool overrides

- `profile`
- `control_plane_count`
- `worker_count`
- `frontend_worker_count`
- `control_plane_size`
- `worker_size`
- `frontend_worker_size`
- `control_plane_backups_enabled`
- `worker_backups_enabled`
- `frontend_worker_backups_enabled`

### Region and networking

- `region`
- `talos_version`
- `kubernetes_version`
- `existing_vpc_id`
- `vpc_ip_range`
- `ssh_key_ids`
- `trusted_cidrs`
- `cluster_api_host_override`
- `enable_api_loadbalancer`
- `enable_ingress_loadbalancer`

### Platform services

- `enable_managed_database`
- `enable_container_registry`
- `manage_dns`
- `db_engine`
- `db_version`
- `db_size`
- `db_node_count`
- `registry_name`
- `registry_tier`
- `registry_region`
- `domain`
- `letsencrypt_email`

### Local output paths

- `kubeconfig_path`
- `talosconfig_path`

Notes:
- In most cases you only need `profile`, `do_token`, `ssh_key_ids`, `domain`, and any security/network overrides; `01-cluster/profiles.tf` supplies the rest.
- `frontend_worker_count` defaults to `0` in `dev` and `staging`, and `2` in `production`.
- Dedicated frontend workers require at least one core worker; the stack enforces `frontend_worker_count > 0 => worker_count > 0`.

## Deployment profiles

### Lowest-cost dev/test profile

This profile is intended to stay as small as possible and is the default shown in [`01-cluster/terraform.tfvars.example`](01-cluster/terraform.tfvars.example).

- `control_plane_count = 1`
- `worker_count = 0`
- `frontend_worker_count = 0`
- `control_plane_size = "s-1vcpu-2gb"`
- `worker_size = "s-1vcpu-2gb"`
- `frontend_worker_size = "s-1vcpu-2gb"`
- `enable_api_loadbalancer = true`
- `enable_ingress_loadbalancer = false`
- `enable_managed_database = false`
- `enable_container_registry = false`
- `manage_dns = false`

Notes:
- this is suitable for development and smoke testing, not HA production
- workloads run on the control plane automatically when `worker_count = 0`
- ingress uses host ports instead of a paid load balancer
- disable the API load balancer only if you already have a stable public DNS name or reserved IP and set `cluster_api_host_override`
- if you still want a managed registry, set `enable_container_registry = true`

### Small public cluster profile

Use this if you need public ingress with fewer compromises.

- `control_plane_count = 1`
- `worker_count = 1`
- `frontend_worker_count = 0`
- `control_plane_size = "s-2vcpu-4gb"`
- `worker_size = "s-2vcpu-4gb"`
- `frontend_worker_size = "s-2vcpu-4gb"`
- `enable_api_loadbalancer = true`
- `enable_ingress_loadbalancer = true`
- `enable_managed_database = true`
- `enable_container_registry = true`
- `manage_dns = true`

### Higher-availability profile

Use the original topology only when cost is less important.

- `control_plane_count = 3`
- `worker_count = 2` (core workers: API, workers, ingress LB backends, cluster add-ons)
- `frontend_worker_count = 2` (production default: separate Talos pool, tainted `node.kubernetes.io/assignment=frontend`)
- `control_plane_size = "s-2vcpu-4gb"`
- `worker_size = "s-2vcpu-4gb"`
- `frontend_worker_size = "s-2vcpu-4gb"`
- `enable_api_loadbalancer = true`
- `enable_ingress_loadbalancer = true`
- `enable_managed_database = true`
- `enable_container_registry = true`
- `manage_dns = true`
- `control_plane_backups_enabled = true`
- `worker_backups_enabled = true`
- `frontend_worker_backups_enabled = true`

**Node pools (production):** Core workers keep the DigitalOcean tag `worker` so the ingress load balancer only registers nodes where NGINX is expected. Dedicated frontend droplets use `worker-frontend` instead, plus a Talos kubelet registration taint so only workloads that tolerate it (production dashboard and tenant SSR deployments) land there. A Terraform `check` enforces `frontend_worker_count > 0 ⇒ worker_count > 0` so you never end up with only tainted workers and nowhere for the control plane to offload ingress or system pods.

## Local apply flow

1. Copy [`01-cluster/terraform.tfvars.example`](01-cluster/terraform.tfvars.example) to `01-cluster/terraform.tfvars` if you are applying locally.
2. Set at least `do_token` and `ssh_key_ids`, then pick a `profile` and any overrides you need.
3. Run `terraform init` and `terraform apply` from `infra/terraform/01-cluster`.
4. After apply, retrieve outputs from [`01-cluster/outputs.tf`](01-cluster/outputs.tf) such as `kubeconfig`, `kubeconfig_path`, `talosconfig_path`, `k8s_api_endpoint`, `ingress_ip`, and the node IP lists.

## Cost reduction changes in this refactor

This refactor adds flags so that expensive services can be disabled without rewriting the stack:

- optional Kubernetes API load balancer
- optional ingress load balancer
- optional managed PostgreSQL cluster
- optional DigitalOcean Container Registry
- optional Droplet backups
- automatic control-plane scheduling when `worker_count = 0`

These flags make it possible to run a single-node Talos cluster for development while keeping the same Terraform codebase for larger environments.
