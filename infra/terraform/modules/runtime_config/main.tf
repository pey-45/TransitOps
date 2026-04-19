resource "aws_secretsmanager_secret" "db_connection_string" {
  name                    = "${var.secrets_prefix}/db-connection-string"
  description             = "TransitOps database connection string. Value is provided during deployment/bootstrap."
  recovery_window_in_days = var.secret_recovery_window_days

  tags = merge(var.tags, {
    Name = "${var.secrets_prefix}/db-connection-string"
  })
}

resource "aws_secretsmanager_secret" "jwt_signing_key" {
  name                    = "${var.secrets_prefix}/jwt-signing-key"
  description             = "TransitOps JWT signing key. Value is provided during deployment/bootstrap."
  recovery_window_in_days = var.secret_recovery_window_days

  tags = merge(var.tags, {
    Name = "${var.secrets_prefix}/jwt-signing-key"
  })
}

resource "aws_secretsmanager_secret" "bootstrap_first_admin_token" {
  name                    = "${var.secrets_prefix}/bootstrap-first-admin-token"
  description             = "TransitOps first-admin bootstrap token. Value is provided during deployment/bootstrap."
  recovery_window_in_days = var.secret_recovery_window_days

  tags = merge(var.tags, {
    Name = "${var.secrets_prefix}/bootstrap-first-admin-token"
  })
}

resource "aws_ssm_parameter" "jwt_issuer" {
  name  = "${var.parameters_prefix}/jwt-issuer"
  type  = "String"
  value = var.jwt_issuer

  tags = merge(var.tags, {
    Name = "${var.parameters_prefix}/jwt-issuer"
  })
}

resource "aws_ssm_parameter" "jwt_audience" {
  name  = "${var.parameters_prefix}/jwt-audience"
  type  = "String"
  value = var.jwt_audience

  tags = merge(var.tags, {
    Name = "${var.parameters_prefix}/jwt-audience"
  })
}

resource "aws_ssm_parameter" "jwt_expiration_minutes" {
  name  = "${var.parameters_prefix}/jwt-expiration-minutes"
  type  = "String"
  value = tostring(var.jwt_expiration_minutes)

  tags = merge(var.tags, {
    Name = "${var.parameters_prefix}/jwt-expiration-minutes"
  })
}
