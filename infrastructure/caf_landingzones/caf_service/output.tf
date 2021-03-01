output tfstates {
  value     = local.tfstates
  sensitive = true
}

output global_settings {
  value     = local.global_settings
  sensitive = true
}

output virtual_machines {
  value = map(
    var.landingzone.key, module.service.virtual_machines
  )
  sensitive = true
}

# This is a special output that will be used to store all the output data you wish to return
# from the API. This should not include any sensitive data but can reference secrets stored
# in a restricted location.
output deployment_output_data {
  value = {
    # TODO: Once the dsvm public ip is removed also remove these public outputs.
    virtual_machine_public_ip = module.service.public_ip_addresses.dsvmvm_pip.ip_address
    virtual_machine_public_fqdn = module.service.public_ip_addresses.dsvmvm_pip.fqdn

    virtual_machine_private_fqdns = module.service.virtual_machines.dsvm.internal_fqdns
    virtual_machine_admin_username = module.service.virtual_machines.dsvm.admin_username
     
    virtual_machine_admin_password_secret_ref = module.service.virtual_machines.dsvm.admin_password_secret_id
  }
  sensitive = true
}