locals {
  landingzone = {
    current = {
      storage_account_name = var.tfstate_storage_account_name
      container_name       = var.tfstate_container_name
      resource_group_name  = var.tfstate_resource_group_name
    }
    lower = {
      storage_account_name = var.lower_storage_account_name
      container_name       = var.lower_container_name
      resource_group_name  = var.lower_resource_group_name
    }
  }
}

data "terraform_remote_state" "remote" {
  for_each = try(var.landingzone.tfstates, {})

  backend = var.landingzone.backend_type
  config = {
    storage_account_name = local.landingzone[try(each.value.level, "current")].storage_account_name
    container_name       = local.landingzone[try(each.value.level, "current")].container_name
    resource_group_name  = local.landingzone[try(each.value.level, "current")].resource_group_name
    key                  = each.value.tfstate
  }
}

locals {
  landingzone_tag = {
    "landingzone" = var.landingzone.key
  }

  tags = merge(var.tags, local.landingzone_tag, { "level" = var.landingzone.level }, { "rover_version" = var.rover_version })
  
  global_settings = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.global_settings
  
  level_2_keyvault = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.keyvaults.launchpad.level2
  level_3_keyvault = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.keyvaults.launchpad.level3  
  level_4_keyvault = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.keyvaults.launchpad.level4  
  
  level_2_storageaccount = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.storageaccounts.launchpad.level2
  level_3_storageaccount = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.storageaccounts.launchpad.level3  
  level_4_storageaccount = data.terraform_remote_state.remote[var.landingzone.global_settings_key].outputs.storageaccounts.launchpad.level4  
}