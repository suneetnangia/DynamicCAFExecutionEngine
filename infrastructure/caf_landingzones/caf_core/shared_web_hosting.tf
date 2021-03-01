resource "azurerm_app_service_plan" "accl-plan" {
  name                = "accl-${var.azure_resource_suffix}"
  location            = azurerm_resource_group.accl-resource-group.location
  resource_group_name = azurerm_resource_group.accl-resource-group.name

  sku {
    tier = "Basic"
    size = "B3"
  }

  kind = "Linux"
  reserved = true
}