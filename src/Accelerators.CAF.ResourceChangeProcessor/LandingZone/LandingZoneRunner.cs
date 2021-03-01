namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
	using Microsoft.Azure.Management.Fluent;
	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Runs CAF template to deploy a landing zone via Azure Container Instance.
	/// </summary>
	public class LandingZoneRunner
	{
		private const string AciVolumeName = "caflandingzones";
		private const string AciMountPath = "/tf/caf";
		private const string AciContainerName = "rover";
		private const string AciContainerGroupStatusFailed = "Failed";
		private const string AciContainerGroupStatusSucceeded = "Succeeded";

		private readonly IAzure _azure;
		private readonly string _resourceGroupName;
		private readonly string _location;
		private readonly string _storageAccountName;
		private readonly string _storageAccountKey;
		private readonly string _userAssignedMSIId;
		private readonly string _containerImageName;
		private readonly int _deploymentCheckDelayInSeconds;
		private readonly Dictionary<CafLandingZoneLevel, string> _cafLandingZoneLevels;
		private readonly ILogger<LandingZoneRunner> _logger;

		public LandingZoneRunner(
			IAzure azure,
			string existingResourceGroupName,
			string location,
			string storageAccountName,
			string storageAccountKey,
			string userAssignedMSIId,
			string containerImageName,
			int deploymentCheckDelayInSeconds,
			Dictionary<CafLandingZoneLevel, string> cafLandingZoneLevels,
			ILogger<LandingZoneRunner> logger)
		{
			_azure = azure ?? throw new ArgumentNullException(nameof(azure));
			_resourceGroupName = existingResourceGroupName ?? throw new ArgumentNullException(nameof(existingResourceGroupName));
			_location = location ?? throw new ArgumentNullException(nameof(location));
			_storageAccountName = storageAccountName ?? throw new ArgumentNullException(nameof(storageAccountName));
			_storageAccountKey = storageAccountKey ?? throw new ArgumentNullException(nameof(storageAccountKey));
			_userAssignedMSIId = userAssignedMSIId ?? throw new ArgumentNullException(nameof(userAssignedMSIId));
			_containerImageName = containerImageName ?? throw new ArgumentNullException(nameof(containerImageName));
			_deploymentCheckDelayInSeconds = deploymentCheckDelayInSeconds == 0 ? 1 : deploymentCheckDelayInSeconds;
			_cafLandingZoneLevels = cafLandingZoneLevels ?? throw new ArgumentNullException(nameof(cafLandingZoneLevels));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Configures and run Azure Container Instance to deploy CAF landing zone.
		/// </summary>
		/// <param name="cafLandingZoneLevel"><see cref="CafLandingZoneLevel"/> instance to specify landing zone level.</param>
		/// <param name="cafExecutionMode"><see cref="CafExecutionMode"/> instance to specify CAF execution mode.</param>
		/// <param name="landingzoneName">Name of landing zone which is unique within the same CAF level.</param>
		/// <param name="fileShareName">Name of file share where processed CAF template available.</param>
		/// <param name="outputFileName">The output file name.</param>
		/// <returns>Instance of <see cref="Task"/> that represents a run of deployment operation and return logs.</returns>
		public virtual async Task<string> RunAsync(CafLandingZoneLevel cafLandingZoneLevel, CafExecutionMode cafExecutionMode, string landingzoneName, string fileShareName, string outputFileName)
		{
			var containerGroupName = Guid.NewGuid().ToString();

			var cafExecutionModeString = cafExecutionMode == CafExecutionMode.Destroy ? "destroy -auto-approve" : "apply";
			var cafTerraformOutputName = cafExecutionMode == CafExecutionMode.Apply ? " deployment_output_data" : string.Empty;

			_logger.LogInformation($"Creating Azure Container Instance group {containerGroupName} to run CAF Rover container...");

			var containerGroup = _azure.ContainerGroups.Define(containerGroupName)
					.WithRegion(_location)
					.WithExistingResourceGroup(_resourceGroupName)
					.WithLinux()
					.WithPublicImageRegistryOnly()
					.DefineVolume(AciVolumeName)
						.WithExistingReadWriteAzureFileShare(fileShareName)
						.WithStorageAccountName(_storageAccountName)
						.WithStorageAccountKey(_storageAccountKey)
						.Attach()
					.DefineContainerInstance(AciContainerName)
						.WithImage(_containerImageName)
						.WithoutPorts()
						.WithVolumeMountSetting(AciVolumeName, AciMountPath)
						.WithStartingCommandLine(
							"/bin/bash",
							new string[]
							{
								"-c",
								@$"az login --identity -u {_userAssignedMSIId};
								cp -r /tf/caf/caf_landingzones/{landingzoneName} {landingzoneName};								
								./rover.sh -lz /tf/rover/{landingzoneName} -level {_cafLandingZoneLevels[cafLandingZoneLevel]} -var-folder /tf/rover/{landingzoneName}/scenario/standard-001 -a {cafExecutionModeString};								
								(cd /tf/rover/{landingzoneName}; terraform output -json {cafTerraformOutputName} > /tf/caf/{outputFileName};)"
							}) // TODO: consider setting variables folder dynamically.
						.WithEnvironmentVariables(new Dictionary<string, string> { { "ARM_USE_MSI", "true" } })
						.Attach()
					.WithExistingUserAssignedManagedServiceIdentity(await _azure.Identities.GetByIdAsync(_userAssignedMSIId))
					.WithRestartPolicy(ContainerGroupRestartPolicy.Never)
					.Create();

			containerGroup = _azure.ContainerGroups.GetById(containerGroup.Id);
			string aciLogs;

			try
			{
				while (!(containerGroup.State.Equals(AciContainerGroupStatusSucceeded, StringComparison.Ordinal)
					|| containerGroup.State.Equals(AciContainerGroupStatusFailed, StringComparison.Ordinal)))
				{
					var containerStatus = containerGroup.Containers.ContainsKey(AciContainerName) ? containerGroup.Containers[AciContainerName].InstanceView.CurrentState.State : "not found";
					_logger.LogInformation($"Container group {containerGroupName} is {containerGroup.State}... container status is {containerStatus}.");

					await Task.Delay(TimeSpan.FromSeconds(_deploymentCheckDelayInSeconds));

					containerGroup = _azure.ContainerGroups.GetById(containerGroup.Id);
				}

				aciLogs = await containerGroup.GetLogContentAsync(AciContainerName);

				if (containerGroup.State.Equals(AciContainerGroupStatusFailed, StringComparison.Ordinal))
				{
					throw new LandingZoneRunnerException($"CAF container execution failed in ACI, ACI log: {aciLogs}");
				}
				else
				{
					return aciLogs;
				}
			}
			finally
			{
				// Delete container group.
				_azure.ContainerGroups.DeleteById(containerGroup.Id);
			}
		}
	}
}