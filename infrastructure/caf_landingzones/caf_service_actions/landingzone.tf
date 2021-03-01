# Operations Landing Zone.
module "operation" {
  source  = "aztfmod/caf/azurerm"
  version = "~>5.1.2"

  current_landingzone_key  = var.landingzone.key  
  global_settings          = local.global_settings
  tenant_id                = var.tenant_id
  logged_user_objectId     = var.logged_user_objectId
  logged_aad_app_objectId  = var.logged_aad_app_objectId  
}