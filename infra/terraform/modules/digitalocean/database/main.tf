resource "digitalocean_database_cluster" "main" {
  name       = "${var.project_name}-db"
  engine     = var.engine
  version    = var.engine_version
  size       = var.size
  region     = var.region
  node_count = var.node_count

  private_network_uuid = var.vpc_id

  tags = [var.project_name, "database"]

  maintenance_window {
    day  = "sunday"
    hour = "02:00:00"
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "digitalocean_database_db" "app" {
  cluster_id = digitalocean_database_cluster.main.id
  name       = var.db_name
}

resource "digitalocean_database_user" "app" {
  cluster_id = digitalocean_database_cluster.main.id
  name       = var.db_user
}

# Restrict access to nodes tagged with the project tag (VPC only)
resource "digitalocean_database_firewall" "main" {
  cluster_id = digitalocean_database_cluster.main.id

  rule {
    type  = "tag"
    value = var.node_tag
  }
}
