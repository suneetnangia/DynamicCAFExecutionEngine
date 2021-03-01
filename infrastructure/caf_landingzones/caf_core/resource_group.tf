resource "azurerm_resource_group" "accl-resource-group" {
  name     = "Accl-${title(var.azure_resource_suffix)}"
  location = local.global_settings.regions.region1
}
