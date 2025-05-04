using System.Collections.Generic;
using System;

namespace KERBALISM
{
	/// <summary>Global cache for storing and accessing VesselResources (and ResourceInfo) handlers in all vessels, with shortcut for common methods</summary>
	static class ResourceCache
	{
		// resource cache
		static Dictionary<Guid, VesselResources> entries;

		/// <summary> pseudo-ctor </summary>
		internal static void Init()
		{
			entries = new Dictionary<Guid, VesselResources>();
		}

		/// <summary> clear all resource information for all vessels </summary>
		internal static void Clear()
		{
			entries.Clear();
		}

		/// <summary> Reset the whole resource simulation for the vessel </summary>
		internal static void Purge(Vessel v)
		{
			entries.Remove(v.id);
		}

		/// <summary> Reset the whole resource simulation for the vessel </summary>
		internal static void Purge(ProtoVessel pv)
		{
			entries.Remove(pv.vesselID);
		}

		/// <summary> Return the VesselResources handler for this vessel </summary>
		internal static VesselResources Get(Vessel v)
		{
			// try to get existing entry if any
			VesselResources entry;
			if (entries.TryGetValue(v.id, out entry)) return entry;

			// create new entry
			entry = new VesselResources();

			// remember new entry
			entries.Add(v.id, entry);

			// return new entry
			return entry;
		}

		/// <summary> return a resource handler (shortcut) </summary>
		internal static ResourceInfo GetResource(Vessel v, string resource_name)
		{
			return Get(v).GetResource(v, resource_name);
		}

		/// <summary> record deferred production of a resource (shortcut) </summary>
		/// <param name="brokerName">short ui-friendly name for the producer</param>
		internal static void Produce(Vessel v, string resource_name, double quantity, ResourceBroker broker)
		{
			GetResource(v, resource_name).Produce(quantity, broker);
		}

		/// <summary> record deferred consumption of a resource (shortcut) </summary>
		/// <param name="brokerName">short ui-friendly name for the consumer</param>
		internal static void Consume(Vessel v, string resource_name, double quantity, ResourceBroker broker)
		{
			GetResource(v, resource_name).Consume(quantity, broker);
		}

		/// <summary> register deferred execution of a recipe (shortcut)</summary>
		internal static void AddRecipe(Vessel v, ResourceRecipe recipe)
		{
			Get(v).AddRecipe(recipe);
		}
	}
}
