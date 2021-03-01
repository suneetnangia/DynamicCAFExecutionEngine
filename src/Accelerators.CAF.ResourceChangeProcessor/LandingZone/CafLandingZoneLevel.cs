namespace Accelerators.CAF.ResourceChangeProcessor.LandingZone
{
	/// <summary>
	/// CAF landing zone levels.
	/// Each level can have multiple landing zones.
	/// </summary>
	public enum CafLandingZoneLevel
	{
		/// <summary>
		/// Level 0 deploys core TF bootstrapping infrastructure e.g. backend state stores for each level.
		/// </summary>
		Level0,

		/// <summary>
		/// Level 1 deploy foundation landing zone.
		/// </summary>
		Level1,

		/// <summary>
		/// Level 2 deploys core infrastructure landing zone e.g. hub networks.
		/// </summary>
		Level2,

		/// <summary>
		/// Level 3 deploys base app platform landing zones e.g. DMI core apps and workspaces.
		/// </summary>
		Level3,

		/// <summary>
		/// Level 4 deploys user app landing zones e.g. DSVM services.
		/// </summary>
		Level4,
	}
}