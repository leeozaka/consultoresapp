# ── Cluster secrets ────────────────────────────────────────────────────────────

resource "talos_machine_secrets" "this" {
  lifecycle {
    # Machine secrets are the cryptographic root of the cluster.
    # Deleting them makes the cluster unrecoverable without a full rebuild.
    prevent_destroy = false
  }
}

# ── Talos image schematic ──────────────────────────────────────────────────────
# Produces a schematic ID for the minimal Talos image (no extra extensions).
# Override to add extensions like iscsi-tools or tailscale.

resource "talos_image_factory_schematic" "this" {
  schematic = yamlencode({
    customization = {
      systemExtensions = {
        officialExtensions = []
      }
    }
  })
}

# ── Machine configurations ────────────────────────────────────────────────────
# The DO CCM manifest is vendored locally to avoid a network dependency at plan
# time. To upgrade, replace the file and bump the version in the filename.

data "talos_machine_configuration" "control_plane" {
  cluster_name       = "consultores"
  cluster_endpoint   = "https://${local.k8s_api_host}:6443"
  machine_type       = "controlplane"
  machine_secrets    = talos_machine_secrets.this.machine_secrets
  kubernetes_version = var.kubernetes_version
  talos_version      = var.talos_version

  config_patches = [
    yamlencode({
      machine = {
        kubelet = {
          extraArgs = {
            "cloud-provider" = "external"
          }
        }
        features = {
          kubernetesTalosAPIAccess = {
            enabled                     = true
            allowedRoles                = ["os:reader"]
            allowedKubernetesNamespaces = ["kube-system"]
          }
        }
      }
      cluster = {
        inlineManifests = [
          {
            name     = "do-ccm-secret"
            contents = <<-EOT
              apiVersion: v1
              kind: Secret
              metadata:
                name: digitalocean
                namespace: kube-system
              stringData:
                access-token: "${var.do_token}"
            EOT
          },
          {
            name     = "do-ccm"
            contents = file("${path.module}/manifests/do-ccm-v0.1.54.yaml")
          }
        ]
        network = {
          podSubnets     = ["10.244.0.0/16"]
          serviceSubnets = ["10.96.0.0/12"]
          dnsDomain      = "cluster.local"
        }
        allowSchedulingOnControlPlanes = local.workloads_on_control_planes
        apiServer = {
          auditPolicy = {
            apiVersion = "audit.k8s.io/v1"
            kind       = "Policy"
            rules      = [{ level = "Metadata" }]
          }
        }
      }
    })
  ]
}

data "talos_machine_configuration" "worker" {
  cluster_name       = "consultores"
  cluster_endpoint   = "https://${local.k8s_api_host}:6443"
  machine_type       = "worker"
  machine_secrets    = talos_machine_secrets.this.machine_secrets
  kubernetes_version = var.kubernetes_version
  talos_version      = var.talos_version

  config_patches = [
    yamlencode({
      machine = {
        kubelet = {
          extraArgs = {
            "cloud-provider" = "external"
          }
        }
      }
    })
  ]
}

# Dedicated SSR / tenant UI nodes: label + taint so only production frontends
# (toleration + required nodeAffinity) schedule here; ingress stays on core workers.

data "talos_machine_configuration" "frontend_worker" {
  cluster_name       = "consultores"
  cluster_endpoint   = "https://${local.k8s_api_host}:6443"
  machine_type       = "worker"
  machine_secrets    = talos_machine_secrets.this.machine_secrets
  kubernetes_version = var.kubernetes_version
  talos_version      = var.talos_version

  config_patches = [
    yamlencode({
      machine = {
        nodeLabels = {
          "node.kubernetes.io/assignment" = "frontend"
        }
        kubelet = {
          extraArgs = {
            "cloud-provider" = "external"
          }
          extraConfig = {
            registerWithTaints = [
              {
                key    = "node.kubernetes.io/assignment"
                value  = "frontend"
                effect = "NoSchedule"
              }
            ]
          }
        }
      }
    })
  ]
}

# ── talosctl client configuration ─────────────────────────────────────────────

data "talos_client_configuration" "this" {
  cluster_name         = "consultores"
  client_configuration = talos_machine_secrets.this.client_configuration
  nodes                = module.compute.control_plane_ips
  endpoints            = module.compute.control_plane_ips
}

# ── Apply machine configurations ──────────────────────────────────────────────

resource "talos_machine_configuration_apply" "control_plane" {
  count = local.control_plane_count

  client_configuration        = talos_machine_secrets.this.client_configuration
  machine_configuration_input = data.talos_machine_configuration.control_plane.machine_configuration
  node                        = module.compute.control_plane_ips[count.index]

  depends_on = [module.compute]
}

resource "talos_machine_configuration_apply" "worker" {
  count = local.worker_count

  client_configuration        = talos_machine_secrets.this.client_configuration
  machine_configuration_input = data.talos_machine_configuration.worker.machine_configuration
  node                        = module.compute.worker_ips[count.index]

  depends_on = [module.compute]
}

resource "talos_machine_configuration_apply" "frontend_worker" {
  count = local.frontend_worker_count

  client_configuration        = talos_machine_secrets.this.client_configuration
  machine_configuration_input = data.talos_machine_configuration.frontend_worker.machine_configuration
  node                        = module.compute.frontend_worker_ips[count.index]

  depends_on = [module.compute]
}

# ── Bootstrap etcd ────────────────────────────────────────────────────────────

resource "talos_machine_bootstrap" "this" {
  client_configuration = talos_machine_secrets.this.client_configuration
  node                 = module.compute.control_plane_ips[0]

  depends_on = [talos_machine_configuration_apply.control_plane]
}

# ── Retrieve admin kubeconfig ─────────────────────────────────────────────────

resource "talos_cluster_kubeconfig" "this" {
  client_configuration = talos_machine_secrets.this.client_configuration
  node                 = module.compute.control_plane_ips[0]

  depends_on = [talos_machine_bootstrap.this]
}

# ── Write config files to disk ────────────────────────────────────────────────

resource "local_file" "kubeconfig" {
  filename        = local.kubeconfig_path
  content         = talos_cluster_kubeconfig.this.kubeconfig_raw
  file_permission = "0600"
}

resource "local_file" "talosconfig" {
  filename        = local.talosconfig_path
  content         = data.talos_client_configuration.this.talos_config
  file_permission = "0600"
}
