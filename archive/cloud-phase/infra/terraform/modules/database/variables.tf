variable "name_prefix" {
  description = "Canonical environment resource-name prefix."
  type        = string
}

variable "data_subnet_ids" {
  description = "Private data subnet IDs used by RDS."
  type        = list(string)
}

variable "rds_security_group_id" {
  description = "Security group ID attached to the RDS instance."
  type        = string
}

variable "database_name" {
  description = "Initial PostgreSQL database name."
  type        = string
  default     = "transitops"
}

variable "database_username" {
  description = "Master database username."
  type        = string
  sensitive   = true
}

variable "database_password" {
  description = "Master database password."
  type        = string
  sensitive   = true
}

variable "engine_version" {
  description = "PostgreSQL engine version."
  type        = string
  default     = "16.13"
}

variable "instance_class" {
  description = "RDS instance class."
  type        = string
  default     = "db.t4g.micro"
}

variable "allocated_storage_gb" {
  description = "Initial allocated storage in GiB."
  type        = number
  default     = 20
}

variable "max_allocated_storage_gb" {
  description = "Maximum storage autoscaling limit in GiB."
  type        = number
  default     = 100
}

variable "backup_retention_days" {
  description = "RDS backup retention in days."
  type        = number
  default     = 7
}

variable "deletion_protection" {
  description = "Whether deletion protection is enabled."
  type        = bool
  default     = false
}

variable "skip_final_snapshot" {
  description = "Whether final snapshot is skipped on destroy."
  type        = bool
  default     = true
}

variable "multi_az" {
  description = "Whether RDS runs in Multi-AZ mode."
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags applied to database resources."
  type        = map(string)
  default     = {}
}
