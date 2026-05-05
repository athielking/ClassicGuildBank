terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.71.0"
    }

    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }

    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "5.19.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth     = true
    storage_account_name = "fbiterraformbackend"
    container_name       = "terraform"
    key                  = "terraform.tfstate"
    resource_group_name  = "rg-fbi-terraform"
  }
}

provider "cloudflare" {
}

provider "azurerm" {
  features {
  }
}

provider "aws" {
  region = "us-east-1"
}


locals {
  resource_group_location = var.resource_group_location
  sql_admin_password      = var.sql_admin_password
  dns_name                = "classicguildbank.thielking.dev"
  sqlcmd_base             = "sqlcmd -S ${azurerm_mssql_server.db_server.fully_qualified_domain_name} -d ${azurerm_mssql_database.db.name} -U terraformadmin -P '${var.sql_admin_password}'"
  # IP Addresses allowed to access Sql Server
  allowed_ips = [
    {
      start = "23.28.20.0"
      end   = "23.28.20.255"
    }
  ]
}

# ------------------------------------------------------------------------------
# Deploy API and DB to Azure
# ------------------------------------------------------------------------------

# Create the resource group to hold everything
resource "azurerm_resource_group" "rg" {
  name     = "ClassicGuildBank"
  location = local.resource_group_location
}

# Create SQL Server
resource "azurerm_mssql_server" "db_server" {
  name                          = lower("${azurerm_resource_group.rg.name}-sql")
  resource_group_name           = azurerm_resource_group.rg.name
  location                      = azurerm_resource_group.rg.location
  version                       = "12.0"
  administrator_login           = "terraformadmin"
  administrator_login_password  = local.sql_admin_password
  public_network_access_enabled = true

  azuread_administrator {
    login_username = "athielking@gmail.com"
    object_id      = "0084b44c-4311-46a1-973c-9f40b8e65ad1"
  }
}

# Create a firewall rule to allow azure resources to access the SQL Server
resource "azurerm_mssql_firewall_rule" "db_allow_azure" {
  name             = "AllowAzureResources"
  server_id        = azurerm_mssql_server.db_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
  depends_on = [ azurerm_mssql_server.db_server ]
}

# Create a firewall rule to allow local access to sql
resource "azurerm_mssql_firewall_rule" "allow_ip_block" {
  count            = length(local.allowed_ips)
  name             = "Allow ${local.allowed_ips[count.index].start}"
  server_id        = azurerm_mssql_server.db_server.id
  start_ip_address = local.allowed_ips[count.index].start
  end_ip_address   = local.allowed_ips[count.index].end
  depends_on = [ azurerm_mssql_server.db_server ]
}

# Create the SQL Database
resource "azurerm_mssql_database" "db" {
  name      = "${azurerm_resource_group.rg.name}-db"
  server_id = azurerm_mssql_server.db_server.id
  sku_name  = "Basic"
  depends_on = [ azurerm_mssql_server.db_server ]
}

resource "null_resource" "db_schema" {
  triggers = {
    sql_hash = filemd5("./ClassicGuildBankData/SQL/schema.sql")
  }
  provisioner "local-exec" {
    command = "${local.sqlcmd_base} -i ./ClassicGuildBankData/SQL/schema.sql"
  }
  depends_on = [azurerm_mssql_database.db]
}

resource "null_resource" "seed_items" {
  triggers = {
    sql_hash = filemd5("./ClassicGuildBankData/SQL/seed_items.sql")
  }
  provisioner "local-exec" {
    command = "${local.sqlcmd_base} -i ./ClassicGuildBankData/SQL/seed_items.sql"
  }
  depends_on = [null_resource.db_schema]
}

resource "null_resource" "seed_users" {
  triggers = {
    sql_hash = filemd5("./ClassicGuildBankData/SQL/seed_users.sql")
  }
  provisioner "local-exec" {
    command = "${local.sqlcmd_base} -i ./ClassicGuildBankData/SQL/seed_users.sql"
  }
  depends_on = [null_resource.seed_items]
}

# Create the Azure App Service Plan
resource "azurerm_service_plan" "app_sp" {
  name                = "${azurerm_resource_group.rg.name}-sp"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Windows"
  sku_name            = "B1"
}

# Create the Azure Web App
resource "azurerm_windows_web_app" "app_svc" {
  name                = "${azurerm_resource_group.rg.name}-api"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.app_sp.id

  connection_string {
    name  = "ClassicGuildBankDb"
    type  = "SQLServer"
    value = "Server=${azurerm_mssql_server.db_server.fully_qualified_domain_name};Initial Catalog=${azurerm_mssql_database.db.name};User ID=terraformadmin;Password=${local.sql_admin_password}"
  }

  app_settings = {
    WEBSITE_RUN_FROM_PACKAGE = "1"
    ASPNETCORE_ENVIRONMENT   = "Production"
    ClientUrl                = "https://${local.dns_name}/#"
  }

  site_config {
    cors {
      allowed_origins = [
        "https://${local.dns_name}"
      ]
      support_credentials = true
    }

    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v6.0"
    }
  }
}

# ------------------------------------------------------------------------------
# DEPLOY STATIC WEBSITE AWS
# ------------------------------------------------------------------------------

resource "aws_s3_bucket" "b" {
  bucket = local.dns_name
  force_destroy = true
}

resource "aws_s3_bucket_website_configuration" "bucket" {
  bucket = aws_s3_bucket.b.id
  index_document {
    suffix = "index.html"
  }
  error_document {
    key = "index.html"
  }
}

resource "aws_s3_bucket_public_access_block" "b" {
  bucket = aws_s3_bucket_website_configuration.bucket.id

  block_public_policy = false
  block_public_acls   = true
  ignore_public_acls  = true
}

data "aws_iam_policy_document" "policy" {
  statement {
    sid = "1"
    actions = [
      "s3:GetObject"
    ]
    resources = [
      "${aws_s3_bucket.b.arn}/*"
    ]
    principals {
      type        = "*"
      identifiers = ["*"]
    }
  }
}

resource "aws_s3_bucket_policy" "bucket_policy" {
  bucket = aws_s3_bucket_website_configuration.bucket.id
  policy = data.aws_iam_policy_document.policy.json
}

data "aws_acm_certificate" "cert" {
  domain   = "*.thielking.dev"
  statuses = ["ISSUED"]
}

resource "aws_cloudfront_distribution" "s3_distribution" {

  origin {
    domain_name = aws_s3_bucket.b.bucket_regional_domain_name
    origin_id   = "s3-${local.dns_name}"
  }

  default_cache_behavior {
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "s3-${local.dns_name}"
    viewer_protocol_policy = "redirect-to-https"
    min_ttl                = 0
    default_ttl            = 86400
    max_ttl                = 31536000

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }
  }

  aliases = [ local.dns_name ]

  viewer_certificate {
    acm_certificate_arn = data.aws_acm_certificate.cert.arn
    ssl_support_method  = "sni-only"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  default_root_object = "index.html"
  price_class         = "PriceClass_200"
  enabled             = true
  is_ipv6_enabled     = true
}


# ------------------------------------------------------------------------------
# Create DNS Entries to point domain to cloudfront
# ------------------------------------------------------------------------------

# Import existing DNS Zone as Data.  This is not managed by this terraform deployment
data "cloudflare_zone" "dns_zone" {
  zone_id = "d7e91e43d2066585660e2ae39248896d"
}

# Create a DNS record to point to our CDN Enpoint
resource "cloudflare_dns_record" "example_dns_record" {
  zone_id = data.cloudflare_zone.dns_zone.id
  name = "classicguildbank"
  ttl = 1
  type = "CNAME"
  comment = "Classic Guild Bank"
  content = aws_cloudfront_distribution.s3_distribution.domain_name
  proxied = true
}

