variable "resource_group_location" {
    type = string
    default = "eastus2"
    description   = "Location of the resource group." 
}

variable "sql_admin_password" {
    type = string
    description = "Password for SQL Server Admin Login"
}