landingzone = {
  backend_type        = "azurerm"
  global_settings_key = "caf_network_hub"
  level               = "level3"
  key                 = "workspace"

  # This can only get one level lower tfstate.  
  tfstates = {
    caf_network_hub = {
      level   = "lower"
      tfstate = "caf_network_hub.tfstate"
    }
  }
}

resource_groups = {
  vnet_spoke = {
    name   = "workspace"
    region = "region1"
  }
}

# Hub to spoke peering from spoke only works here when hub and spokes are in the same subscription.
vnet_peerings = {
  hub-re1_TO_wpspoke_re1 = {
    name = "hub-re1_TO_wpspoke_re1"
    from = {
      lz_key = "caf_network_hub"
      vnet_key = "hub_re1"
    }
    to = {
      vnet_key = "wpspoke_re1"
    }
    allow_virtual_network_access = true
    allow_forwarded_traffic      = true
    allow_gateway_transit        = false
    use_remote_gateways          = false
  }

  wpspoke_re1_TO_hub-re1 = {
    name = "wpspoke_re1_TO_hub_re1"
    from = {
      vnet_key = "wpspoke_re1"
    }
    to = {
      lz_key = "caf_network_hub"
      vnet_key = "hub_re1"
    }
    allow_virtual_network_access = true
    allow_forwarded_traffic      = false
    allow_gateway_transit        = false
    use_remote_gateways          = false
  }
}