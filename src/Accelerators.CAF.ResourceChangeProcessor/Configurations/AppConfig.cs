namespace Accelerators.CAF.ResourceChangeProcessor.Configurations
{
    public class AppConfig
    {
        // MSI Id used to run ACI container for CAF Rover
        public string UserAssignedMSIId { get; set; }

        // Time interval to check status of ACI container run for CAF Rover
        public int DeploymentCheckDelayInSeconds { get; set; }

        // Azure AD tenant Id for authentication
        public string AzureADTenantId { get; set; }

        // Azure subscription Id for authentication
        public string AzureSubscriptionId { get; set; }

        // Storage account resource group for CAF
        public string AzureCAFStorageAccountResourceGroupName { get; set; }

        // Storage account for CAF templates/outputs
        public string AzureCAFStorageAccountName { get; set; }

        // Resource group for running ACI instances.
        public string ExistingACIResourceGroupName { get; set; }

        // Location for running ACI instances e.g. northeurope
        public string ACILocation { get; set; }

        // Public ACI image to use for running CAF Rover e.g. aztfmod/rover:2012.1109
        public string ACIImage { get; set; }
    }
}