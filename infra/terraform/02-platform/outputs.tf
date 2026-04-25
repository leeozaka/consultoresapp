output "ingress_nginx_namespace" {
  description = "Namespace where NGINX Ingress is installed"
  value       = kubernetes_namespace.ingress_nginx.metadata[0].name
}

output "argocd_namespace" {
  description = "Namespace where ArgoCD is installed (null if not enabled)"
  value       = local.pcfg.enable_argocd ? kubernetes_namespace.argocd[0].metadata[0].name : null
}

output "headlamp_namespace" {
  description = "Namespace where Headlamp is installed (null if not enabled)"
  value       = local.pcfg.enable_headlamp ? kubernetes_namespace.headlamp[0].metadata[0].name : null
}
