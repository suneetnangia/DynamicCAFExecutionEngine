namespace Accelerators.CAF.ResourceChangeProcessor
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Text;
	using System.Text.Json;
	using System.Threading.Tasks;

	using Accelerators.CAF.ResourceChangeProcessor.LandingZone;

	using Azure.Storage.Files.Shares;
	using Azure.Storage.Files.Shares.Models;

	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Azure.WebJobs;
	using Microsoft.Azure.WebJobs.Extensions.Http;
	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Receives resource change messages and run CAF landing zones to deploy workspaces and services.
	/// </summary>
	public class LandingZoneResourceChangeProcessor
	{
		private const string LandingZoneTemplateShareName = "landingzones";
		private const string OutputFileName = "output.json";

		private readonly LandingZoneConfigurationProcessor _landingZoneProcessor;
		private readonly LandingZoneRunner _containerRunner;
		private readonly ShareServiceClient _shareServiceClient;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="LandingZoneResourceChangeProcessor"/> class.
		/// </summary>
		/// <param name="landingZoneProcessor">A reference to the <see cref="LandingZoneConfigurationProcessor"/> object.</param>
		/// <param name="containerRunner">A reference to the <see cref="LandingZoneRunner"/> object.</param>
		/// <param name="shareServiceClient">A reference to the <see cref="ShareServiceClient"/> object.</param>
		/// <param name="logger">An instance of <see cref="ILogger{T}"/>.</param>
		public LandingZoneResourceChangeProcessor(
			LandingZoneConfigurationProcessor landingZoneProcessor,
			LandingZoneRunner containerRunner,
			ShareServiceClient shareServiceClient,
			ILogger<LandingZoneResourceChangeProcessor> logger)
		{
			_landingZoneProcessor = landingZoneProcessor ?? throw new ArgumentNullException(nameof(landingZoneProcessor));
			_containerRunner = containerRunner ?? throw new ArgumentNullException(nameof(containerRunner));
			_shareServiceClient = shareServiceClient ?? throw new ArgumentNullException(nameof(shareServiceClient));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Recieves message from service bus and process CAF TF template to deploy landing zone for that resource.
		/// Service Bus sessions implement sequential convoy pattern here i.e. allow messages for same resources
		/// to be handled by a same instance of function at any point in time.
		/// </summary>
		/// <param name="request">Instance of <see cref="HttpRequest"/> containing a request message.</param>
		/// <returns>Instance of <see cref="Task"/> that represents the asynchronous function processing.</returns>
		/// <remarks>
		/// Sessions implement sequential convoy pattern here i.e. allow messages for same resources
		/// to be handled by a same instance of function at any point in time.
		/// </remarks>
		[FunctionName("ResourceChangeProcessor")]
		public async Task<AcceptedResult> RunAsync(
			 [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
			 HttpRequest request)
		{
			string containerGroupLog = null;

			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			byte[] messageBytes;
			string messageString;
			using (var streamReader = new StreamReader(request.Body))
			{
				messageString = await streamReader.ReadToEndAsync();
				messageBytes = Encoding.UTF8.GetBytes(messageString);
			}

			var requestJson = JsonDocument.Parse(messageString);
			var resourceId = requestJson.RootElement.GetProperty("id").GetString();
			var resourceTypeString = requestJson.RootElement.GetProperty("resourceType").GetString();
			var isDeleted = requestJson.RootElement.GetProperty("isDeleted").GetBoolean();

			// Start the task to process the request and return immediately.
			_ = Task.Run(() => ProcessRequestAsync(containerGroupLog, messageBytes, messageString, resourceId, Enum.Parse<ResourceType>(resourceTypeString), isDeleted));

			return new AcceptedResult(string.Empty, "Request accepted by the system.");
		}

		private async Task ProcessRequestAsync(string containerGroupLog, byte[] messageBytes, string messageString, string resourceId, ResourceType resourceType, bool isDeleted)
		{
			try
			{
				_logger.LogTrace($"Message received: {messageString}");

				var landingZoneName = resourceId;

				// File share name can be created in a bit more readable form later.
				var fileShareName = Guid.NewGuid().ToString();

				// Copy files and replace parameters' file to file share mounted by ACI container.
				_logger.LogInformation($"Creating Azure storage file share {fileShareName} to for storing landing zone configs with parameters...");

				var fileShare = await _shareServiceClient.CreateShareAsync(fileShareName, new ShareCreateOptions { QuotaInGB = 1 });
				var fileShareClient = fileShare.Value.GetRootDirectoryClient();

				var templateShare = _shareServiceClient.GetShareClient(LandingZoneTemplateShareName);
				var templateDirectoryClient = templateShare.GetRootDirectoryClient();

				await _landingZoneProcessor.ProcessFilesAsync(
					messageBytes,
					templateDirectoryClient,
					fileShare.Value.GetRootDirectoryClient(),
					resourceType,
					landingZoneName);

				containerGroupLog = await _containerRunner
				.RunAsync(
				resourceType == ResourceType.Workspace ? CafLandingZoneLevel.Level3 : resourceType == ResourceType.Service
				? CafLandingZoneLevel.Level4 : resourceType == ResourceType.Action ? CafLandingZoneLevel.Level4 : throw new InvalidEnumArgumentException(nameof(resourceType)),
				isDeleted ? CafExecutionMode.Destroy : CafExecutionMode.Apply,
				landingZoneName,
				fileShareName,
				OutputFileName);

				// Get the deployment outputs.
				_logger.LogInformation($"Getting the outputs from file {OutputFileName} in Azure storage file share {fileShareName}...");
				string outputData = await ReadDeploymentOutputData(fileShareClient, OutputFileName);
				_logger.LogInformation($"Output: {outputData}");

				// Delete transformed file share.
				await _shareServiceClient.DeleteShareAsync(fileShareName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				throw;
			}
			finally
			{
				// Log output from container run.
				_logger.LogInformation(containerGroupLog);
			}
		}

		private async Task<string> ReadDeploymentOutputData(ShareDirectoryClient fileShareClient, string outputFileName)
		{
			string outputData = null;
			var file = fileShareClient.GetFileClient(outputFileName);

			var download = file.Download();

			using (var streamReader = new StreamReader(download.Value.Content))
			{
				outputData = await streamReader.ReadToEndAsync();
			}

			return outputData;
		}
	}
}