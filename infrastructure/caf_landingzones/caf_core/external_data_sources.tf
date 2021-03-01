data "azurerm_subscription" "current" {
}

data "azurerm_storage_account" "level_2_tfstate"{
  name                     = local.level_2_storageaccount.name
  resource_group_name      = local.level_2_storageaccount.resource_group_name
}

data "azurerm_storage_account" "level_3_tfstate"{
  name                     = local.level_3_storageaccount.name
  resource_group_name      = local.level_3_storageaccount.resource_group_name
}

data "azurerm_storage_account" "level_4_tfstate"{
  name                     = local.level_4_storageaccount.name
  resource_group_name      = local.level_4_storageaccount.resource_group_name
}

data "azurerm_key_vault" "level_2_keyvault"{
  name = local.level_2_keyvault.name
  resource_group_name = split("/", local.level_2_keyvault.id)[4]
}

data "azurerm_key_vault" "level_3_keyvault"{
  name = local.level_3_keyvault.name
  resource_group_name = split("/", local.level_3_keyvault.id)[4]
}

data "azurerm_key_vault" "level_4_keyvault"{
  name = local.level_4_keyvault.name
  resource_group_name = split("/", local.level_4_keyvault.id)[4]
}

data "azurerm_key_vault_secret" "accl-aad-b2c-basicauth-username" {
  name         = "aad-b2c-basicauth-username"
  key_vault_id = data.azurerm_key_vault.level_3_keyvault.id
}

data "azurerm_key_vault_secret" "accl-aad-b2c-basicauth-salt" {
  name         = "aad-b2c-basicauth-salt"
  key_vault_id = data.azurerm_key_vault.level_3_keyvault.id
}

data "azurerm_key_vault_secret" "accl-aad-b2c-basicauth-hash" {
  name         = "aad-b2c-basicauth-hash"
  key_vault_id = data.azurerm_key_vault.level_3_keyvault.id
}

data "azurerm_key_vault_secret" "accl-aad-b2c-client-secret" {
  name         = "aad-b2c-client-secret"
  key_vault_id = data.azurerm_key_vault.level_3_keyvault.id
}