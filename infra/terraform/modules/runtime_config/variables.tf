variable "secrets_prefix" {
  description = "Secrets Manager path prefix for application secrets."
  type        = string
}

variable "parameters_prefix" {
  description = "SSM Parameter Store path prefix for application parameters."
  type        = string
}

variable "jwt_issuer" {
  description = "JWT issuer exposed to the application runtime."
  type        = string
}

variable "jwt_audience" {
  description = "JWT audience exposed to the application runtime."
  type        = string
}

variable "jwt_expiration_minutes" {
  description = "JWT expiration in minutes exposed to the application runtime."
  type        = number
  default     = 60
}

variable "secret_recovery_window_days" {
  description = "Secrets Manager recovery window in days."
  type        = number
  default     = 7
}

variable "tags" {
  description = "Tags applied to runtime configuration resources."
  type        = map(string)
  default     = {}
}
