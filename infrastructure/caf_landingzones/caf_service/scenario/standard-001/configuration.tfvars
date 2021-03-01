resource_groups = {
  service_rg = {
    name   = "service"
    region = "region1"
  }
}

keyvaults = {
  dsvm_key_vault = {
    name               = "vmsecrets"
    resource_group_key = "service_rg"
    sku_name           = "standard"
  }
}

# TODO: Review this access.
keyvault_access_policies = {
  # A maximum of 16 access policies per keyvault
  dsvm_key_vault = {
    logged_in_user = {
      secret_permissions = ["Set", "Get", "List", "Delete", "Purge", "Recover"]
    }
    logged_in_aad_app = {
      secret_permissions = ["Set", "Get", "List", "Delete", "Purge", "Recover"]
    }
  }
}

public_ip_addresses = {
  dsvmvm_pip = {
    name                    = "dsvm_pip"
    resource_group_key      = "service_rg"
    sku                     = "Standard"
    allocation_method       = "Static"
    ip_version              = "IPv4"
    idle_timeout_in_minutes = "4"
  }
}