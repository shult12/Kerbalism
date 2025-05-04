using System.Collections.Generic;

namespace KERBALISM
{
	/// <summary>
	/// Handler for the vessel resources simulator.
	/// Allow access to the resource handler (ResourceInfo) for all resources on the vessel
	/// and also stores of all recorded recipes (ResourceRecipe)
	/// </summary>
	sealed class VesselResources
	{
		Dictionary<int, ResourceInfo> resources = new Dictionary<int, ResourceInfo>(32);
		ResourceInfo GetResInfo(string resName) { resources.TryGetValue(resName.GetHashCode(), out var ri); return ri; }
		List<ResourceRecipe> recipes = new List<ResourceRecipe>(4);

		/// <summary> return a VesselResources handler </summary>
		internal ResourceInfo GetResource(Vessel v, string resource_name)
		{
			// try to get existing entry if any
			ResourceInfo res = GetResInfo(resource_name);
			if (res != null) return res;

			// create new entry
			res = new ResourceInfo(v, resource_name);

			// remember new entry
			resources.Add(resource_name.GetHashCode(), res);

			// return new entry
			return res;
		}


		/// <summary>
		/// Main vessel resource simulation update method.
		/// Execute all recipes to get final deferred amounts, then for each resource apply deferred requests, 
		/// synchronize the new amount in all parts and update ResourceInfo information properties (rates, brokers...)
		/// </summary>
		internal void Sync(Vessel v, VesselData vd, double elapsed_s)
		{
			// execute all recorded recipes
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.Resource.ExecuteRecipes");
			ResourceRecipe.ExecuteRecipes(v, this, recipes);
			UnityEngine.Profiling.Profiler.EndSample();

			// forget the recipes
			recipes.Clear();

			// apply all deferred requests and synchronize to vessel
			// PartResourceList is slow and VERY garbagey to iterate over (because it's a dictionary disguised as a list),
			// so acquiring a full list of all resources in a single loop is faster and less ram consuming than a
			// "n ResourceInfo" * "n parts" * "n PartResource" loop (can easily result in 1000+ calls to p.Resources.dict.Values)
			// It's also faster for unloaded vessels in the case of the ProtoPartResourceSnapshot lists
			// Note: there is a static setup cost since the PR and PPRS wrappers (and the tanksets) have to be instantiated
			// the first time they're used, but all created objects are stored and reused each execution
			// so there shouldn't be much garbage creation at all (indeed there will only be new garbage when the cache
			// is insufficient).
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.Resource.SyncAll");

			// Create sync sets for each resource, based on whether the vessel is loaded (wrappers for PartResource)
			// or unloaded (wrappers for ProtoPartResourceSnapshot). Store priority as well for resources that use a
			// priority-aware flow mode (and when the setting is enabled).
			if (v.loaded)
			{
				foreach (Part p in v.Parts)
				{
					int partPri = Settings.UseResourcePriority ? p.GetResourcePriority() : 1;
					foreach (PartResource r in p.Resources.dict.Values)
					{
						if (r.flowState && resources.TryGetValue(r.info.id, out var resInfo))
						{
							int pri = r.info.resourceFlowMode == ResourceFlowMode.ALL_VESSEL_BALANCE || r.info.resourceFlowMode == ResourceFlowMode.ALL_VESSEL ? 1 : partPri;
							resInfo.AddToSyncSet(r, pri);
						}
					}
				}
			}
			else
			{
				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
				{
					int partPri = Settings.UseResourcePriority
						? ((p.partInfo.partPrefab.resourcePriorityUseParentInverseStage ? p.parent.inverseStageIndex : p.inverseStageIndex) * 10 + p.resourcePriorityOffset)
						: 1;
					foreach (ProtoPartResourceSnapshot r in p.resources)
					{
						if (r.flowState && resources.TryGetValue(r.definition.id, out var resInfo))
						{
							int pri = r.definition.resourceFlowMode == ResourceFlowMode.ALL_VESSEL_BALANCE || r.definition.resourceFlowMode == ResourceFlowMode.ALL_VESSEL ? 1 : partPri;
							resInfo.AddToSyncSet(r, pri);
						}
					}
				}
			}

			// Now sync each resource
			foreach (ResourceInfo resInfo in resources.Values)
			{
				resInfo.Sync(v, vd, elapsed_s);
				resInfo.ClearSyncSet();
			}

			ResourceInfo.ResetSyncCaches();

			UnityEngine.Profiling.Profiler.EndSample();
		}

		internal List<ResourceInfo> GetAllResources(Vessel v)
		{
			List<string> knownResources = new List<string>();
			List<ResourceInfo> result = new List<ResourceInfo>();

			if (v.loaded)
			{
				foreach (Part p in v.Parts)
				{
					foreach (PartResource r in p.Resources)
					{
						if (knownResources.Contains(r.resourceName)) continue;
						knownResources.Add(r.resourceName);
						result.Add(GetResource(v, r.resourceName));
					}
				}
			}
			else
			{
				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
				{
					foreach (ProtoPartResourceSnapshot r in p.resources)
					{
						if (knownResources.Contains(r.resourceName)) continue;
						knownResources.Add(r.resourceName);
						result.Add(GetResource(v, r.resourceName));
					}
				}
			}

			return result;
		}

		/// <summary> record deferred production of a resource (shortcut) </summary>
		/// <param name="brokerName">short ui-friendly name for the producer</param>
		internal void Produce(Vessel v, string resource_name, double quantity, ResourceBroker broker)
		{
			GetResource(v, resource_name).Produce(quantity, broker);
		}

		/// <summary> record deferred consumption of a resource (shortcut) </summary>
		/// <param name="tag">short ui-friendly name for the consumer</param>
		internal void Consume(Vessel v, string resource_name, double quantity, ResourceBroker broker)
		{
			GetResource(v, resource_name).Consume(quantity, broker);
		}

		/// <summary> record deferred execution of a recipe (shortcut) </summary>
		internal void AddRecipe(ResourceRecipe recipe)
		{
			recipes.Add(recipe);
		}
	}
}
