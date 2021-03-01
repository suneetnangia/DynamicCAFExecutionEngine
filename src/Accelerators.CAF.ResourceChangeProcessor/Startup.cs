using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Accelerators.CAF.ResourceChangeProcessor;
using Accelerators.CAF.ResourceChangeProcessor.Configurations;
using Accelerators.CAF.ResourceChangeProcessor.LandingZone;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Accelerators.CAF.ResourceChangeProcessor
{
	/// <summary>
	/// Startup class used to configure the application.
	/// </summary>
	public class Startup : FunctionsStartup
    {
		public override void Configure(IFunctionsHostBuilder builder)
		{
			if (builder is null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			var config = new ConfigurationBuilder()
			   .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "local.settings.json"), optional: true, reloadOnChange: false)
			   .AddEnvironmentVariables()
			   .Build();

			builder.Services.AddLogging();
			builder.Services.Configure<AppConfig>(config.GetSection("AppConfig"));

			// This dependency is needed for Azure Storage File Share client SDK.
			builder.Services.AddSingleton<ShareServiceClient>(s =>
			{
				var appConfig = s.GetRequiredService<IOptions<AppConfig>>().Value;
				var defaultAzureCredential = new DefaultAzureCredential(includeInteractiveCredentials: false);
				var storageKey = GetStorageAccountKeyAsync(
								appConfig.AzureSubscriptionId,
								appConfig.AzureCAFStorageAccountName,
								appConfig.AzureCAFStorageAccountResourceGroupName,
								defaultAzureCredential).GetAwaiter().GetResult();

				return new ShareServiceClient($"DefaultEndpointsProtocol=https;AccountName={appConfig.AzureCAFStorageAccountName};AccountKey={storageKey.Value};EndpointSuffix=core.windows.net;");
			});

			// Inject landing zone processor implementation as per configuration i.e. workspace, service or something else in future, if any.
			builder.Services.AddSingleton<LandingZoneConfigurationProcessor>(s =>
			{
				var appConfig = s.GetRequiredService<IOptions<AppConfig>>().Value;
				var landingZoneFolders = new Dictionary<ResourceType, string>()
					{
                        { ResourceType.Workspace, "caf_workspace" },
                        { ResourceType.Service, "caf_service" },
                        { ResourceType.Action, "caf_service_actions" }
					};

				// Some of these parameters e.g. terraform.tfvars.json can be exposed by app settings if needed.
				return new LandingZoneConfigurationProcessor(
						landingZoneFolders,
						"terraform.tfvars.json",
						s.GetRequiredService<ILogger<LandingZoneConfigurationProcessor>>());
			});

			// Configure landingzone container runner.
			builder.Services.AddTransient<LandingZoneRunner>(s =>
			{
				var appConfig = s.GetRequiredService<IOptions<AppConfig>>().Value;

				// Please see documentation on order of auth attempts, here-
				// https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
				var defaultAzureCredential = new DefaultAzureCredential(includeInteractiveCredentials: false);
				var armToken = defaultAzureCredential.GetToken(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" }, parentRequestId: null), default).Token;
				var armCreds = new Microsoft.Rest.TokenCredentials(armToken);

				var graphToken = defaultAzureCredential.GetToken(new TokenRequestContext(scopes: new[] { "https://graph.windows.net/.default" }, parentRequestId: null), default).Token;
				var graphCreds = new Microsoft.Rest.TokenCredentials(graphToken);

				var azureCredentials = new AzureCredentials(armCreds, graphCreds, appConfig.AzureADTenantId, AzureEnvironment.AzureGlobalCloud);

				var azure = Microsoft.Azure.Management.Fluent.Azure
					.Authenticate(azureCredentials)
					.WithSubscription(appConfig.AzureSubscriptionId);

				var storageKey = GetStorageAccountKeyAsync(
					appConfig.AzureSubscriptionId,
					appConfig.AzureCAFStorageAccountName,
					appConfig.AzureCAFStorageAccountResourceGroupName,
					defaultAzureCredential).GetAwaiter().GetResult();

				var cafLevels = new Dictionary<CafLandingZoneLevel, string>() { { CafLandingZoneLevel.Level3, "level3" }, { CafLandingZoneLevel.Level4, "level4" } };

				return new LandingZoneRunner(
						azure,
						appConfig.ExistingACIResourceGroupName,
						appConfig.ACILocation,
						appConfig.AzureCAFStorageAccountName,
						storageKey.Value,
						appConfig.UserAssignedMSIId,
						appConfig.ACIImage,
						appConfig.DeploymentCheckDelayInSeconds,
						cafLevels,
						s.GetRequiredService<ILogger<LandingZoneRunner>>());
			});
		}

		/// <summary>
		/// Retrieves storage account key.
		/// </summary>
		/// <param name="azureSubscriptionId">Azure subscription id.</param>
		/// <param name="azureCAFStorageAccountName">Azure CAF storage account name.</param>
		/// <param name="azureCAFStorageAccountResourceGroupName">Azure CAF storage account resource group.</param>
		/// <param name="defaultAzureCredential">Instance of <see cref="DefaultAzureCredential"/> for auth.</param>
		/// <returns>Instance of <see cref="Task"/> that represents a retrieved praimary <see cref="StorageAccountKey"/>.</returns>
		private async Task<StorageAccountKey> GetStorageAccountKeyAsync(
        string azureSubscriptionId,
        string azureCAFStorageAccountName,
        string azureCAFStorageAccountResourceGroupName,
        DefaultAzureCredential defaultAzureCredential)
        {
            var storageManagementClient = new StorageManagementClient(azureSubscriptionId, defaultAzureCredential);

            var keysResponse = await storageManagementClient
            .StorageAccounts
            .ListKeysAsync(azureCAFStorageAccountResourceGroupName, azureCAFStorageAccountName);

            return keysResponse.Value.Keys[0];
        }
    }
}