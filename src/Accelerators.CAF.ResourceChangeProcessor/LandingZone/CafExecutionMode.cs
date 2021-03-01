namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	/// <summary>
	/// CAF execution modes.
	/// </summary>
	public enum CafExecutionMode
	{
		/// <summary>
		/// Create or update infrastructure.
		/// </summary>
		Apply,

		/// <summary>
		/// Delete infrastructure.
		/// </summary>
		Destroy
	}
}