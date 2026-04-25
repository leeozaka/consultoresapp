output "private_host" {
  description = "Private hostname (only reachable from within the VPC)"
  value       = digitalocean_database_cluster.main.private_host
  sensitive   = true
}

output "host" {
  description = "Public hostname"
  value       = digitalocean_database_cluster.main.host
  sensitive   = true
}

output "port" {
  description = "Database port"
  value       = digitalocean_database_cluster.main.port
}

output "user" {
  description = "Application database username"
  value       = digitalocean_database_user.app.name
}

output "password" {
  description = "Application database password"
  value       = digitalocean_database_user.app.password
  sensitive   = true
}

output "db_name" {
  description = "Name of the application database"
  value       = digitalocean_database_db.app.name
}

output "write_connection_string" {
  description = "Npgsql write connection string for ConnectionStrings__DefaultConnection"
  value       = "Host=${digitalocean_database_cluster.main.private_host};Port=${digitalocean_database_cluster.main.port};Database=${digitalocean_database_db.app.name};Username=${digitalocean_database_user.app.name};Password=${digitalocean_database_user.app.password};sslmode=require;Pooling=true;Minimum Pool Size=10;Maximum Pool Size=150;Connection Idle Lifetime=300;Connection Pruning Interval=10;Timeout=15"
  sensitive   = true
}

output "read_connection_string" {
  description = "Npgsql read connection string for ConnectionStrings__ReadConnection"
  value       = "Host=${digitalocean_database_cluster.main.private_host};Port=${digitalocean_database_cluster.main.port};Database=${digitalocean_database_db.app.name};Username=${digitalocean_database_user.app.name};Password=${digitalocean_database_user.app.password};sslmode=require;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Idle Lifetime=300;Connection Pruning Interval=10;Timeout=10"
  sensitive   = true
}
