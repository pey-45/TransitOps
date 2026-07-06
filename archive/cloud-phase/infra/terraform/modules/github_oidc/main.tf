locals {
  github_oidc_url      = "https://token.actions.githubusercontent.com"
  github_oidc_host     = "token.actions.githubusercontent.com"
  deploy_role_name     = "${var.name_prefix}-github-actions-deploy-role"
  allowed_subject      = "repo:${var.repository}:ref:refs/heads/${var.branch}"
  oidc_provider_arn    = var.create_oidc_provider ? aws_iam_openid_connect_provider.github[0].arn : var.existing_oidc_provider_arn
  state_bucket_arn     = "arn:aws:s3:::${var.terraform_state_bucket_name}"
  state_bucket_objects = "${local.state_bucket_arn}/*"
  lock_table_arn       = "arn:aws:dynamodb:${var.aws_region}:${var.aws_account_id}:table/${var.terraform_lock_table_name}"
}

resource "aws_iam_openid_connect_provider" "github" {
  count = var.create_oidc_provider ? 1 : 0

  url             = local.github_oidc_url
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = var.github_oidc_thumbprints

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-github-oidc"
  })
}

data "aws_iam_policy_document" "assume_role" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [local.oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.github_oidc_host}:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.github_oidc_host}:sub"
      values   = [local.allowed_subject]
    }
  }
}

resource "aws_iam_role" "deploy" {
  name               = local.deploy_role_name
  assume_role_policy = data.aws_iam_policy_document.assume_role.json

  tags = merge(var.tags, {
    Name = local.deploy_role_name
  })
}

data "aws_iam_policy_document" "deploy" {
  statement {
    sid = "TerraformRemoteState"
    actions = [
      "s3:GetBucketVersioning",
      "s3:GetObject",
      "s3:ListBucket",
      "s3:PutObject",
      "dynamodb:DeleteItem",
      "dynamodb:DescribeTable",
      "dynamodb:GetItem",
      "dynamodb:PutItem",
      "dynamodb:UpdateItem"
    ]
    resources = [
      local.state_bucket_arn,
      local.state_bucket_objects,
      local.lock_table_arn
    ]
  }

  statement {
    sid = "ManageTransitOpsInfrastructure"
    actions = [
      "acm:*",
      "application-autoscaling:*",
      "cloudwatch:*",
      "ec2:*",
      "ecr:*",
      "ecs:*",
      "elasticloadbalancing:*",
      "logs:*",
      "rds:*",
      "route53:*",
      "secretsmanager:*",
      "ssm:*",
      "sts:GetCallerIdentity"
    ]
    resources = ["*"]
  }

  statement {
    sid = "ManageTransitOpsIam"
    actions = [
      "iam:AttachRolePolicy",
      "iam:CreateOpenIDConnectProvider",
      "iam:CreatePolicy",
      "iam:CreateRole",
      "iam:DeleteOpenIDConnectProvider",
      "iam:DeletePolicy",
      "iam:DeleteRole",
      "iam:DeleteRolePolicy",
      "iam:DetachRolePolicy",
      "iam:GetOpenIDConnectProvider",
      "iam:GetPolicy",
      "iam:GetPolicyVersion",
      "iam:GetRole",
      "iam:GetRolePolicy",
      "iam:ListAttachedRolePolicies",
      "iam:ListInstanceProfilesForRole",
      "iam:ListPolicyVersions",
      "iam:ListRolePolicies",
      "iam:PassRole",
      "iam:PutRolePolicy",
      "iam:TagOpenIDConnectProvider",
      "iam:TagPolicy",
      "iam:TagRole",
      "iam:UpdateAssumeRolePolicy"
    ]
    resources = [
      "arn:aws:iam::${var.aws_account_id}:oidc-provider/${local.github_oidc_host}",
      "arn:aws:iam::${var.aws_account_id}:policy/${var.name_prefix}-*",
      "arn:aws:iam::${var.aws_account_id}:role/${var.name_prefix}-*"
    ]
  }
}

resource "aws_iam_role_policy" "deploy" {
  name   = "${var.name_prefix}-github-actions-deploy"
  role   = aws_iam_role.deploy.id
  policy = data.aws_iam_policy_document.deploy.json
}
