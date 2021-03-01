# Workspace Landing Zone.
module "workspace" {
  source  = "aztfmod/caf/azurerm"
  version = "~>5.1.2"

  current_landingzone_key  = var.landingzone.key
  tags                     = local.tags
  global_settings          = local.global_settings
  tenant_id                = var.tenant_id
  logged_user_objectId     = var.logged_user_objectId
  logged_aad_app_objectId  = var.logged_aad_app_objectId
  resource_groups          = var.resource_groups
  keyvaults                = var.keyvaults
  keyvault_access_policies = var.keyvault_access_policies
  networking = {    
    vnets                  = var.vnets
    # Uncomment this line below to allow peering from workspace to hub network.
    # vnet_peerings          = var.vnet_peerings
    network_security_group_definition  = var.network_security_group_definition
  }

  remote_objects = {    
    vnets                  = local.remote.vnets    
  }
}