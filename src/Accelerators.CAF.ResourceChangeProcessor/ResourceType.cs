namespace Accelerators.CAF.ResourceChangeProcessor
{
	/// <summary>
	/// Defines resource types.
	/// Ordering of resource types here is used to define the order in which resources are created/update/deleted, please do not change this order.
	/// </summary>
	public enum ResourceType
	{
		/// <summary>
		/// Workspace resource type.
		/// </summary>
		Workspace,

		/// <summary>
		/// Service resource type.
		/// </summary>
		Service,

		/// <summary>
		/// Action resource type.
		/// </summary>
		Action,
	}
}