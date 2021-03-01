namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.Json;
	using System.Threading.Tasks;

	using Azure.Storage.Files.Shares;
	using Azure.Storage.Files.Shares.Models;

	using DotLiquid;

	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Prepares file structure for the landing zone execution via CAF framework.
	/// </summary>
	public class LandingZoneConfigurationProcessor
	{
		private readonly Dictionary<ResourceType, string> _genericLandingZoneFolderNames;
		private readonly string _parametersFileName;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="LandingZoneConfigurationProcessor"/> class.
		/// </summary>
		/// <param name="genericLandingZoneFolderNames">The folder names where the landing zone tempates are stored.</param>
		/// <param name="parametersFileName">The name of the parameters file.</param>
		/// <param name="logger">A logging object.</param>
		public LandingZoneConfigurationProcessor(
			Dictionary<ResourceType, string> genericLandingZoneFolderNames,
			string parametersFileName,
			ILogger logger)
		{
			_genericLandingZoneFolderNames = genericLandingZoneFolderNames ?? throw new ArgumentNullException(nameof(genericLandingZoneFolderNames));
			_parametersFileName = parametersFileName ?? throw new ArgumentNullException(nameof(parametersFileName));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Copy landing zone template file structure to resource specific folder and inline update the specific json variable file.
		/// </summary>
		/// <param name="receivedDocument">Received <see cref="JsonDocument"/> instance.</param>
		/// <param name="sourceDirectoryClient">Source template <see cref="ShareDirectoryClient"/> instance.</param>
		/// <param name="destinationDirectoryClient">Destination template <see cref="ShareDirectoryClient"/> instance.</param>
		/// <param name="resourceType"><see cref="ResourceType"/> instance.</param>
		/// <param name="landingZoneFolderName">Folder name where template files are present.</param>
		/// <returns>Instance of <see cref="Task"/> that represents the asynchronous file processing operation.</returns>
		public async Task ProcessFilesAsync(
		   byte[] receivedDocument,
		   ShareDirectoryClient sourceDirectoryClient,
		   ShareDirectoryClient destinationDirectoryClient,
		   ResourceType resourceType,
		   string landingZoneFolderName)
		{
			if (receivedDocument is null)
			{
				throw new ArgumentNullException(nameof(receivedDocument));
			}

			if (sourceDirectoryClient is null)
			{
				throw new ArgumentNullException(nameof(sourceDirectoryClient));
			}

			if (destinationDirectoryClient is null)
			{
				throw new ArgumentNullException(nameof(destinationDirectoryClient));
			}

			if (string.IsNullOrEmpty(landingZoneFolderName))
			{
				throw new ArgumentNullException(nameof(landingZoneFolderName));
			}

			foreach (var fileItem in sourceDirectoryClient.GetFilesAndDirectories())
			{
				if (fileItem.IsDirectory)
				{
					_logger.LogInformation($"Creating directory {fileItem.Name} in {destinationDirectoryClient.ShareName}/{destinationDirectoryClient.Name}...");
					var shareSubDirectoryClient = await destinationDirectoryClient.CreateSubdirectoryAsync(
						fileItem.Name.Equals(_genericLandingZoneFolderNames[resourceType], StringComparison.Ordinal) ? landingZoneFolderName : fileItem.Name);

					// TODO: Currently we copy all folders including caf_service and caf_workspace but only one of them is need.
					await ProcessFilesAsync(receivedDocument, sourceDirectoryClient.GetSubdirectoryClient(fileItem.Name), shareSubDirectoryClient.Value, resourceType, landingZoneFolderName);
				}
				else
				{
					var fileClient = sourceDirectoryClient.GetFileClient(fileItem.Name);
					using (var fileStream = await fileClient.OpenReadAsync(new ShareFileOpenReadOptions(false)))
					{
						_logger.LogInformation($"Copying file {fileItem.Name} in {destinationDirectoryClient.ShareName}/{destinationDirectoryClient.Name}...");

						if (fileItem.Name.Equals(_parametersFileName, StringComparison.Ordinal))
						{
							using (var updatedStream = await UpdateParametersAsync(fileStream, receivedDocument))
							{
								var file = await destinationDirectoryClient.CreateFileAsync(fileItem.Name, updatedStream.Length);
								await file.Value.UploadAsync(updatedStream);
							}
						}
						else
						{
							var file = await destinationDirectoryClient.CreateFileAsync(fileItem.Name, fileStream.Length);
							await file.Value.UploadAsync(fileStream);
						}
					}
				}
			}
		}

		/// <summary>
		/// Updates templated json parameter file with the parameters present in receivedDocument.
		/// </summary>
		/// <param name="landingZoneParameterTemplate">Json template <see cref="Stream"/> instance.</param>
		/// <param name="receivedDocument"><see cref="byte[]"/> array of json to copy parameters values from.</param>
		/// <returns>Instance of <see cref="Task"/> that represents a <see cref="Stream"/> of transformed json.</returns>
		protected virtual async Task<Stream> UpdateParametersAsync(Stream landingZoneParameterTemplate, byte[] receivedDocument)
		{
			_logger.LogInformation("Updating landing zone template parameters...");

			using (var templateStreamReader = new StreamReader(landingZoneParameterTemplate))
			{
				var options = new JsonSerializerOptions();
				options.Converters.Add(new JsonElementObjectConverter());
				var metadataString = Encoding.UTF8.GetString(receivedDocument);
				var metadataCollection = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataString, options);

				if (metadataCollection.ContainsKey("data"))
				{
					var dataCollection = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataCollection["data"].ToString(), options);
					metadataCollection["data"] = dataCollection;
				}

				var template = Template.Parse(await templateStreamReader.ReadToEndAsync());
				var transformedTemplate = template.Render(Hash.FromDictionary(metadataCollection));

				var transformedJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(transformedTemplate));
				return transformedJsonStream;
			}
		}
	}
}