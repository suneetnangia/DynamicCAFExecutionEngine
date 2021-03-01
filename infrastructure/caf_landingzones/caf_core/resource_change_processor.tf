resource "azurerm_key_vault" "accl-func-app-rcp-key-vault" {
  name                       = "accl-rcp-${var.azure_resource_suffix}"
  location                   = azurerm_resource_group.accl-resource-group.location
  resource_group_name        = azurerm_resource_group.accl-resource-group.name
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"  
  soft_delete_retention_days = 7
}

resource "azurerm_key_vault_access_policy" "accl-func-app-rcp-key-vault-policy" {
  key_vault_id = azurerm_key_vault.accl-func-app-rcp-key-vault.id
  tenant_id    = var.tenant_id
  object_id    = azurerm_function_app.accl-function-app-rcp.identity[0].principal_id

  secret_permissions = [
    "get",
  ]

  # This is required as we want to ensure rover has access policy to manage secrets.
  depends_on = [azurerm_key_vault_access_policy.accl-func-app-rcp-key-vault-system-policy]
}

resource "azurerm_key_vault_access_policy" "accl-func-app-rcp-key-vault-system-policy" {
  key_vault_id = azurerm_key_vault.accl-func-app-rcp-key-vault.id
  tenant_id    = var.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "get",
    "set",
    "list",
    "delete",
    "purge"
  ]
}

resource "azurerm_user_assigned_identity" "accl-aci-msi" {  
  name = "accl-aci-msi-${var.azure_resource_suffix}"
  location            = azurerm_resource_group.accl-resource-group.location
  resource_group_name = azurerm_resource_group.accl-resource-group.name  
}

resource "azurerm_storage_account" "accl-aci-storage-account" {
  name                     = "acclaci${var.azure_resource_suffix}"
  location                 = azurerm_resource_group.accl-resource-group.location
  resource_group_name      = azurerm_resource_group.accl-resource-group.name
  account_tier             = "Standard"
  account_kind             = "StorageV2"
  account_replication_type = "LRS"
}

resource "azurerm_storage_share" "accl-aci-caf-file-share" {
  name                 = "landingzones"
  storage_account_name = azurerm_storage_account.accl-aci-storage-account.name
  quota                = 1
}

resource "null_resource" "upload-caf-templates" {
  # Always triggered.
  triggers = {
    random_id = uuid()
  }
  
  #Upload caf template files here.
  provisioner "local-exec" {
    when    = create
    command = <<-EOT
    
    az storage directory create \
    --name caf_landingzones \
    --share-name ${azurerm_storage_share.accl-aci-caf-file-share.name} \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage directory create \
    --name caf_landingzones/caf_workspace \
    --share-name ${azurerm_storage_share.accl-aci-caf-file-share.name} \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage directory create \
    --name caf_landingzones/caf_service \
    --share-name ${azurerm_storage_share.accl-aci-caf-file-share.name} \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage directory create \
    --name caf_landingzones/caf_service_actions \
    --share-name ${azurerm_storage_share.accl-aci-caf-file-share.name} \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage file upload-batch \
    --source "../caf_workspace" \
    --destination ${azurerm_storage_share.accl-aci-caf-file-share.name}/caf_landingzones/caf_workspace \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage file upload-batch \
    --source "../caf_service" \
    --destination ${azurerm_storage_share.accl-aci-caf-file-share.name}/caf_landingzones/caf_service \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    az storage file upload-batch \
    --source "../caf_service_actions" \
    --destination ${azurerm_storage_share.accl-aci-caf-file-share.name}/caf_landingzones/caf_service_actions \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

     az storage directory create \
    --name .devcontainer \
    --share-name ${azurerm_storage_share.accl-aci-caf-file-share.name} \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

     az storage file upload-batch \
    --source "../../.devcontainer" \
    --destination ${azurerm_storage_share.accl-aci-caf-file-share.name}/.devcontainer \
    --account-key ${azurerm_storage_account.accl-aci-storage-account.primary_access_key} \
    --account-name ${azurerm_storage_account.accl-aci-storage-account.name}

    EOT
  }
}

resource "azurerm_storage_account" "accl-func-app-rcp-storage-account" {
  name                     = "acclfuncapprcp${var.azure_resource_suffix}"
  location                 = azurerm_resource_group.accl-resource-group.location
  resource_group_name      = azurerm_resource_group.accl-resource-group.name
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_function_app" "accl-function-app-rcp" {
  name                       = "accl-rcp-func-app-${var.azure_resource_suffix}"
  location                   = azurerm_resource_group.accl-resource-group.location
  resource_group_name        = azurerm_resource_group.accl-resource-group.name
  app_service_plan_id        = azurerm_app_service_plan.accl-plan.id
  os_type                    = "linux"  
  version                    = "~3"
  storage_account_name       = azurerm_storage_account.accl-func-app-rcp-storage-account.name
  storage_account_access_key = azurerm_storage_account.accl-func-app-rcp-storage-account.primary_access_key
  identity {
    type = "SystemAssigned"
  }
  site_config {    
    always_on = true
  }
  app_settings = {    
    "FUNCTIONS_WORKER_RUNTIME"                           = "dotnet"    
    "AzureFunctionsJobHost__logging__LogLevel__Default"  = "Trace"    
    "AppConfig__UserAssignedMSIId"                       = azurerm_user_assigned_identity.accl-aci-msi.id
    "AppConfig__DeploymentCheckDelayInSeconds"           = "1"
    "AppConfig__AzureADTenantId"                         = var.tenant_id
    "AppConfig__AzureSubscriptionId"                     = data.azurerm_subscription.current.subscription_id
    "AppConfig__AzureCAFStorageAccountResourceGroupName" = azurerm_resource_group.accl-resource-group.name
    "AppConfig__AzureCAFStorageAccountName"              = azurerm_storage_account.accl-aci-storage-account.name
    "AppConfig__ExistingACIResourceGroupName"            = azurerm_resource_group.accl-resource-group.name
    "AppConfig__ACILocation"                             = var.caf_aci_location
    "AppConfig__ACIImage"                                = "aztfmod/rover:2012.1109"    
  }
}