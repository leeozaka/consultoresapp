# Terraform Migration Guide

This document describes how to migrate from the old flat Terraform config
(`infra/terraform/`) to the new split configuration (`01-cluster/` + `02-platform/`).

## What changed

| Before | After |
|--------|-------|
| Single config, two-phase apply (`enable_k8s_addons = false` → `true`) | Two independent configs, each a single `apply` |
| 15+ boolean toggles | `profile = "dev\|staging\|production"` with per-variable overrides |
| All DO resources flat in root | Extracted into `modules/digitalocean/{networking,compute,database,registry,dns}` |
| kubernetes/helm providers in same config as cluster | `02-platform` reads cluster state via `terraform_remote_state` |
| DO CCM fetched from GitHub at plan time | Vendored locally at `01-cluster/manifests/do-ccm-v0.1.54.yaml` |
| Talos/K8s API open to `0.0.0.0/0` | Controlled by `trusted_cidrs` variable |
| `talos_machine_secrets` had no `prevent_destroy` | `prevent_destroy = true` added |
| `local` backend inconsistency with README | Both configs use `local` backend, README updated |

## New workflow

```bash
# Step 1: provision cluster infrastructure
cd infra/terraform/01-cluster
cp terraform.tfvars.example terraform.tfvars
# edit terraform.tfvars
terraform init && terraform apply

# Step 2: install Kubernetes addons
cd infra/terraform/02-platform
cp terraform.tfvars.example terraform.tfvars
# edit terraform.tfvars
terraform init && terraform apply

# Or both in one command:
make tf-apply-all
```

## Migrating existing state

If you already have a running cluster provisioned with the old flat config,
do NOT run `terraform apply` in the new configs without migrating state first
— Terraform would try to recreate all resources.

### Option A: State migration (preferred for active clusters)

```bash
# 1. Init the new configs so their state files exist
cd infra/terraform/01-cluster && terraform init
cd infra/terraform/02-platform && terraform init

# 2. Copy the old state as a starting point for 01-cluster
cp infra/terraform/terraform.tfstate infra/terraform/01-cluster/terraform.tfstate

# 3. Remove k8s_addons module resources from 01-cluster state
#    (they belong in 02-platform)
cd infra/terraform/01-cluster
terraform state list | grep "module.k8s_addons" | while read r; do
  terraform state rm "$r"
done

# 4. Import or move k8s_addons resources into 02-platform
#    The easiest path is to run terraform apply in 02-platform
#    and let it adopt the existing Helm releases (they are idempotent).
cd infra/terraform/02-platform
terraform apply  # will detect existing Helm releases and import them

# 5. Once both configs are verified, delete the old flat state
# rm infra/terraform/terraform.tfstate
```

### Option B: Fresh cluster (simplest)

Tear down the old cluster with the old config, then provision fresh with the
new split configs. Only viable if you have no production data.

```bash
cd infra/terraform && terraform destroy
cd infra/terraform/01-cluster && terraform apply
cd infra/terraform/02-platform && terraform apply
```

## Old files to delete after migration

Once the new configs are verified and working, delete these from
`infra/terraform/` (the old flat config):

```
infra/terraform/main.tf
infra/terraform/variables.tf
infra/terraform/versions.tf
infra/terraform/backend.tf
infra/terraform/outputs.tf
infra/terraform/networking.tf
infra/terraform/droplets.tf
infra/terraform/talos.tf
infra/terraform/database.tf
infra/terraform/registry.tf
infra/terraform/dns.tf
infra/terraform/k8s_addons.tf
infra/terraform/modules/k8s_addons/
infra/terraform/terraform.tfstate       (after migrating to new state files)
infra/terraform/terraform.tfstate.backup
```

Keep: `infra/terraform/modules/digitalocean/` (new modules), `infra/terraform/01-cluster/`,
`infra/terraform/02-platform/`, `infra/terraform/MIGRATION.md`.
