output "secret_arns" {
  description = "Secrets Manager ARNs keyed by .NET configuration key."
  value = {
    ConnectionStrings__DefaultConnection = aws_secretsmanager_secret.db_connection_string.arn
    Jwt__SigningKey                      = aws_secretsmanager_secret.jwt_signing_key.arn
    Bootstrap__FirstAdminToken           = aws_secretsmanager_secret.bootstrap_first_admin_token.arn
  }
}

output "parameter_names" {
  description = "SSM parameter names keyed by .NET configuration key."
  value = {
    Jwt__Issuer            = aws_ssm_parameter.jwt_issuer.name
    Jwt__Audience          = aws_ssm_parameter.jwt_audience.name
    Jwt__ExpirationMinutes = aws_ssm_parameter.jwt_expiration_minutes.name
  }
}

output "parameter_arns" {
  description = "SSM parameter ARNs keyed by .NET configuration key."
  value = {
    Jwt__Issuer            = aws_ssm_parameter.jwt_issuer.arn
    Jwt__Audience          = aws_ssm_parameter.jwt_audience.arn
    Jwt__ExpirationMinutes = aws_ssm_parameter.jwt_expiration_minutes.arn
  }
}
