output tfstates {
  value     = local.tfstates
  sensitive = true
}

output global_settings {
  value     = local.global_settings
  sensitive = true
}

# This is a special output that will be used to store all the output data you wish to return
# from the API. This should not include any sensitive data but can reference secrets stored
# in a restricted location.
output deployment_output_data {
  value = {}
  sensitive = true
}