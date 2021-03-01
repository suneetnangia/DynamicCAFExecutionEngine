namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	using System;

	/// <summary>
	/// Represents a landing zone runner error.
	/// </summary>
	public class LandingZoneRunnerException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LandingZoneRunnerException"/> class.
		/// </summary>
		public LandingZoneRunnerException()
			: base()
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="LandingZoneRunnerException"/> class.
		/// </summary>
		/// <param name="message">Exception message.</param>
		public LandingZoneRunnerException(string message)
			: base(message)
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="LandingZoneRunnerException"/> class.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="inner">Inner exception.</param>
		public LandingZoneRunnerException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}