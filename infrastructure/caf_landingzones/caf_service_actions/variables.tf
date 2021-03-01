# Map of the remote data state for lower level
variable lower_storage_account_name {}
variable lower_container_name {}
variable lower_resource_group_name {}

variable tfstate_storage_account_name {}
variable tfstate_container_name {}
variable tfstate_key {}
variable tfstate_resource_group_name {}

variable landingzone {}
variable tenant_id {}

variable global_settings {
  default = {}
}

variable rover_version {}

variable logged_user_objectId {
  default = null
}
variable logged_aad_app_objectId {
  default = null
}
variable service_action {
  default = {}
}