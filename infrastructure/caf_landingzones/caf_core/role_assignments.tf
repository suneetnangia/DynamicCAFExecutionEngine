# Role assignments for MSIs requires owner level access by Rover when they are changed/created.
resource "azurerm_role_assignment" "accl-role-assignment-rcp-aci-storage" {
  scope                = azurerm_storage_account.accl-aci-storage-account.id
  role_definition_name = "Storage Account Key Operator Service Role"
  principal_id         = azurerm_function_app.accl-function-app-rcp.identity[0].principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-rcp-aci-resource-group" {
  scope                = azurerm_resource_group.accl-resource-group.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_function_app.accl-function-app-rcp.identity[0].principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-rcp-aci-msi" {
  scope                = azurerm_user_assigned_identity.accl-aci-msi.id
  role_definition_name = "Managed Identity Operator"
  principal_id         = azurerm_function_app.accl-function-app-rcp.identity[0].principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-aci-subscription" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.accl-aci-msi.principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-aci-caf-level_2_storage" {
  scope                = data.azurerm_storage_account.level_2_tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.accl-aci-msi.principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-aci-caf-level_3_storage" {
  scope                = data.azurerm_storage_account.level_3_tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.accl-aci-msi.principal_id
}

resource "azurerm_role_assignment" "accl-role-assignment-aci-caf-level_4_storage" {
  scope                = data.azurerm_storage_account.level_4_tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.accl-aci-msi.principal_id
}

resource "azurerm_key_vault_access_policy" "accl-access-policy-aci-caf-level_2_keyvault" {
  key_vault_id = data.azurerm_key_vault.level_2_keyvault.id
  tenant_id    = var.tenant_id
  object_id    = azurerm_user_assigned_identity.accl-aci-msi.principal_id

  secret_permissions = [
    "get" 
  ]
}

resource "azurerm_key_vault_access_policy" "accl-access-policy-aci-caf-level_3_keyvault" {
  key_vault_id = data.azurerm_key_vault.level_3_keyvault.id
  tenant_id    = var.tenant_id
  object_id    = azurerm_user_assigned_identity.accl-aci-msi.principal_id

  secret_permissions = [
    "get" 
  ]
}

resource "azurerm_key_vault_access_policy" "accl-access-policy-aci-caf-level_4_keyvault" {
  key_vault_id = data.azurerm_key_vault.level_4_keyvault.id
  tenant_id    = var.tenant_id
  object_id    = azurerm_user_assigned_identity.accl-aci-msi.principal_id

  secret_permissions = [
    "get" 
  ]
}