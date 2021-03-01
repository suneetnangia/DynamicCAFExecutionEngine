# Service Landing Zone.
module "service" {
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
    public_ip_addresses         = var.public_ip_addresses
  }
  compute = {
    virtual_machines            = var.virtual_machines
  }

  remote_objects = {
    vnets                       = local.remote.vnets    
  }
}