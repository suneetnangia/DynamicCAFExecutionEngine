# TODO: add more parameters as required.
landingzone = {
  backend_type        = "azurerm"
  global_settings_key = "caf_network_hub"
  level               = "level3"
  key                 = "accl-core"

  # This can only get one level lower tfstate.  
  tfstates = {
    caf_network_hub = {
      level   = "lower"
      tfstate = "caf_network_hub.tfstate"
    }
  }
}

# Max 5 chars long suffix with lowercase letters only e.g. dev, test, uat, prod
azure_resource_suffix = "uat"

caf_aci_location = "northeurope"