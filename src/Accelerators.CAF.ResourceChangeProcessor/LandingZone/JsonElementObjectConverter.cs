namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	using System;
	using System.Text.Json;
	using System.Text.Json.Serialization;

	/// <summary>
	/// Allows conversion of json value received into a language specific type.
	/// Inherits <see cref="JsonConverter"/> for <see cref="object"/>.
	/// </summary>
	public sealed class JsonElementObjectConverter : JsonConverter<object>
	{
		/// <inheritdoc/>
		public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.True)
			{
				return true;
			}

			if (reader.TokenType == JsonTokenType.False)
			{
				return false;
			}

			if (reader.TokenType == JsonTokenType.String)
			{
				return reader.GetString();
			}

			if (reader.TokenType == JsonTokenType.Number)
			{
				// Support other number types i.e. decimal if needed later.
				return reader.GetInt32();
			}

			// Forward to the JsonElement converter
			if (options?.GetConverter(typeof(JsonElement)) is JsonConverter<JsonElement> converter)
			{
				return converter.Read(ref reader, type, options);
			}

			throw new JsonException("Unsupported json element provided.");
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			throw new InvalidOperationException("Directly writing object not supported.");
		}
	}
}