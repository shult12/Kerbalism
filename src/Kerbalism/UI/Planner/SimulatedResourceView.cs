namespace KERBALISM.Planner
{

	/// <summary> Offers a view on a single resource in the planners simulator,
	/// hides the difference between vessel wide resources that can flow through the entire vessel
	/// and resources that are restricted to a single part </summary>
	abstract class SimulatedResourceView
	{
		protected SimulatedResourceView() { }

		internal abstract double amount { get; }
		internal abstract double capacity { get; }
		internal abstract double storage { get; }

		internal abstract void AddPartResources(Part p);
		internal abstract void Produce(double quantity, string name);
		internal abstract void Consume(double quantity, string name);
		internal abstract void Clamp();
	}


} // KERBALISM
