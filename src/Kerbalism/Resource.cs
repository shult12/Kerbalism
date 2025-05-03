using KSP.Localization;
using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class ResourceBroker
	{
		internal enum BrokerCategory
		{
			Unknown,
			Generator,
			Converter,
			SolarPanel,
			Harvester,
			RTG,
			FuelCell,
			ECLSS,
			VesselSystem,
			Kerbal,
			Comms,
			Science
		}

		static Dictionary<string, ResourceBroker> brokersDict = new Dictionary<string, ResourceBroker>();
		static List<ResourceBroker> brokersList = new List<ResourceBroker>();

		internal static ResourceBroker Generic = GetOrCreate("Others", BrokerCategory.Unknown, Local.Brokers_Others);
		internal static ResourceBroker SolarPanel = GetOrCreate("SolarPanel", BrokerCategory.SolarPanel, Local.Brokers_SolarPanel);
		internal static ResourceBroker KSPIEGenerator = GetOrCreate("KSPIEGenerator", BrokerCategory.Generator, Local.Brokers_KSPIEGenerator);
		internal static ResourceBroker FissionReactor = GetOrCreate("FissionReactor", BrokerCategory.Converter, Local.Brokers_FissionReactor);
		internal static ResourceBroker RTG = GetOrCreate("RTG", BrokerCategory.RTG, Local.Brokers_RTG);
		internal static ResourceBroker ScienceLab = GetOrCreate("ScienceLab", BrokerCategory.Science, Local.Brokers_ScienceLab);
		internal static ResourceBroker Light = GetOrCreate("Light", BrokerCategory.VesselSystem, Local.Brokers_Light);
		internal static ResourceBroker Boiloff = GetOrCreate("Boiloff", BrokerCategory.VesselSystem, Local.Brokers_Boiloff);
		internal static ResourceBroker Cryotank = GetOrCreate("Cryotank", BrokerCategory.VesselSystem, Local.Brokers_Cryotank);
		internal static ResourceBroker Greenhouse = GetOrCreate("Greenhouse", BrokerCategory.VesselSystem, Local.Brokers_Greenhouse);
		internal static ResourceBroker Deploy = GetOrCreate("Deploy", BrokerCategory.VesselSystem, Local.Brokers_Deploy);
		internal static ResourceBroker Experiment = GetOrCreate("Experiment", BrokerCategory.Science, Local.Brokers_Experiment);
		internal static ResourceBroker Command = GetOrCreate("Command", BrokerCategory.VesselSystem, Local.Brokers_Command);
		internal static ResourceBroker GravityRing = GetOrCreate("GravityRing", BrokerCategory.RTG, Local.Brokers_GravityRing);
		internal static ResourceBroker Scanner = GetOrCreate("Scanner", BrokerCategory.VesselSystem, Local.Brokers_Scanner);
		internal static ResourceBroker Laboratory = GetOrCreate("Laboratory", BrokerCategory.Science, Local.Brokers_Laboratory);
		internal static ResourceBroker CommsIdle = GetOrCreate("CommsIdle", BrokerCategory.Comms, Local.Brokers_CommsIdle);
		internal static ResourceBroker CommsXmit = GetOrCreate("CommsXmit", BrokerCategory.Comms, Local.Brokers_CommsXmit);
		internal static ResourceBroker StockConverter = GetOrCreate("StockConverter", BrokerCategory.Converter, Local.Brokers_StockConverter);
		internal static ResourceBroker StockDrill = GetOrCreate("Converter", BrokerCategory.Harvester, Local.Brokers_StockDrill);
		internal static ResourceBroker Harvester = GetOrCreate("Harvester", BrokerCategory.Harvester, Local.Brokers_Harvester);

		internal string Id { get; private set; }
		internal BrokerCategory Category { get; private set; }
		internal string Title { get; private set; }
		internal string[] BrokerInfo { get; private set; }

		public override int GetHashCode() => hashcode;
		int hashcode;

		ResourceBroker(string id, BrokerCategory category = BrokerCategory.Unknown, string title = null)
		{
			Id = id;
			Category = category;

			if (string.IsNullOrEmpty(title))
				Title = id;
			else
				Title = title;

			BrokerInfo = new string[] { Category.ToString(), Id, Title};

			hashcode = id.GetHashCode();

			brokersDict.Add(id, this);
			brokersList.Add(this);
		}

		static IEnumerator<ResourceBroker> List()
		{
			return brokersList.GetEnumerator();
		}

		internal static ResourceBroker GetOrCreate(string id)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb;

			return new ResourceBroker(id, BrokerCategory.Unknown, id);
		}

		internal static ResourceBroker GetOrCreate(string id, BrokerCategory type, string title)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb;

			return new ResourceBroker(id, type, title);
		}

		static string GetTitle(string id)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb.Title;
			return null;
		}
	}

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

	/// <summary>
	/// Handler for a single resource on a vessel. Expose vessel-wide information about amounts, rates and brokers (consumers/producers).
	/// Responsible for synchronization between the resource simulator and the actual resources present on each part. 
	/// </summary>
	sealed class ResourceInfo
	{
		#region Sync Set classes etc

		/// <summary>
		/// This lets us use CachedObject<T> to
		/// avoid retyping the caching code
		/// </summary>
		interface IResettable
		{
			void Reset();
		}


		/// <summary>
		/// A simple object cache, will support any class
		/// with a parameterless constructor that implements IResettable
		/// </summary>
		/// <typeparam name="T"></typeparam>
		class CachedObject<T> where T : class, IResettable, new()
		{
			List<T> objects = new List<T>();
			int active = 0;

			/// <summary>
			/// Returns a new T from the cache or, if there aren't
			/// any free, creates a new one and adds it to the cache
			/// </summary>
			/// <returns></returns>
			internal T Next()
			{
				if (active < objects.Count)
					return objects[active++];

				var next = new T();
				++active;
				objects.Add(next);
				return next;
			}

			/// <summary>
			/// Frees an object, resetting it and returning
			/// it to the cache, compacting the active set
			/// </summary>
			/// <param name="obj"></param>
			void Free(T obj)
			{
				for (int i = active; i-- > 0;)
				{
					var o = objects[i];
					if (o == obj)
					{
						--active;
						objects[i] = objects[active];
						objects[active] = obj;
						obj.Reset();
						break;
					}
				}
			}

			/// <summary>
			/// Resets all objects in the cache and
			/// makes them all available
			/// </summary>
			internal void Reset()
			{
				for (int i = active; i-- > 0;)
					objects[i].Reset();

				active = 0;
			}

			/// <summary>
			/// Fully clears the cache. This will expose the objects to GC!
			/// </summary>
			void Clear()
			{
				objects.Clear();
				active = 0;
			}
		}

		/// <summary>
		/// Since stock has two separate resource classes (PartResource
		/// and ProtoPartResourceSnapshot), but they share the same attributes,
		/// we create a baseclass wrapper for them
		/// </summary>
		internal abstract class Wrap : IResettable
		{
			internal abstract double amount { get; set; }
			internal abstract double maxAmount { get; set; }

			public abstract void Reset();
		}

		class WrapPR : Wrap
		{
			PartResource res;

			internal override double amount { get => res.amount; set => res.amount = value; }
			internal override double maxAmount { get => res.maxAmount; set => res.maxAmount = value; }

			internal void Link(PartResource r) { res = r; }

			public override void Reset() { res = null; }
		}

		class WrapPPRS : Wrap
		{
			ProtoPartResourceSnapshot res;

			internal override double amount { get => res.amount; set => res.amount = value; }
			internal override double maxAmount { get => res.maxAmount; set => res.maxAmount = value; }

			internal void Link(ProtoPartResourceSnapshot r) { res = r; }

			public override void Reset() { res = null; }
		}

		/// <summary>
		/// A holder for a series of tanksets, stored in priority order.
		/// The tanksets contain resource wrappers rather than resources directly.
		/// The total amount and maxAmount for all tanks is stored.
		/// </summary>
		class PriorityTankSets : IResettable
		{
			List<TankSet> sets = new List<TankSet>(5);
			internal double amount;
			internal double maxAmount;

			internal void Add(Wrap rw, int pri)
			{
				amount += rw.amount;
				maxAmount += rw.maxAmount;

				int low = 0;
				int high = sets.Count - 1;
				TankSet ts;
				while (low <= high)
				{
					int mid = low + (high - low) / 2;
					ts = sets[mid];
					if (ts.priority == pri)
					{
						ts.Add(rw);
						return;
					}

					if (ts.priority < pri)
						low = mid + 1;
					else
						high = mid - 1;
				}
				ts = cachedTS.Next();
				ts.priority = pri;
				ts.Add(rw);
				sets.Insert(low, ts);
			}

			/// <summary>
			/// Applies a resource delta, in priority order.
			/// Note we pull from highest-priority tanks first,
			/// and push to lowest-priority tanks first.
			/// NOTE: This delta will already have been clamped to
			/// [-amount, maxAmount]
			/// </summary>
			/// <param name="delta"></param>
			internal void ApplyDelta(double delta)
			{
				// pulling
				if (delta < 0)
				{
					// remaining delta is made positive for easy
					// comparison
					double remD = -delta;
					// start at the back (highest priority first)
					for (int i = sets.Count; i-- > 0;)
					{
						var ts = sets[i];

						// If the set has nothing for us, skip
						if (ts.amount == 0)
							continue;

						// If the set has less resource than we want,
						// empty it and continue
						if (ts.amount < remD)
						{
							remD -= ts.amount;
							ts.Empty();
							continue;
						}

						// If we get here, we know we have
						// enough resource to cover the delta.
						// No need to update remD.
						ts.ApplyDelta(-remD);
						break;
					}
				}
				else
				{
					double remD = delta;
					// start at the front (lowest priority first)
					for (int i = 0, iC = sets.Count; i < iC; ++i)
					{
						var ts = sets[i];
						double free = ts.maxAmount - ts.amount;

						// if the set has no free space, skip
						if (free == 0)
							continue;

						// if the set has less headroom than we need,
						// fill it completely and continue
						if (free < remD)
						{
							remD -= free;
							ts.Fill();
							continue;
						}

						// if we get here, we know we have the headroom
						// to cover the delta
						ts.ApplyDelta(remD);
						break;
					}
				}

				amount += delta;
			}

			public void Reset()
			{
				amount = 0;
				maxAmount = 0;
				sets.Clear();
			}
		}

		internal class TankSet : IResettable
		{
			internal int priority;
			List<Wrap> tanks = new List<Wrap>(20);
			internal double amount = 0;
			internal double maxAmount = 0;

			public void Reset()
			{
				amount = 0;
				maxAmount = 0;
				tanks.Clear();
			}

			internal void Add(Wrap r)
			{
				amount += r.amount;
				maxAmount += r.maxAmount;
				tanks.Add(r);
			}

			internal void Fill()
			{
				amount = maxAmount;
				for (int i = tanks.Count; i-- > 0;)
				{
					var t = tanks[i];
					t.amount = t.maxAmount;
				}
			}

			internal void Empty()
			{
				amount = 0;
				for (int i = tanks.Count; i-- > 0;)
					tanks[i].amount = 0;
			}

			internal void ApplyDelta(double delta)
			{
				// If we're pulling, then the ratio per tank is
				// its amount / [total amount of all tanks].
				// If we're pushing, then the ratio is
				// [maxAmount - amount] / [total maxAmount - total amount of all tanks]
				// Also, because the tank wrappers have properties, cache the values.
				if (delta > 0)
				{
					double recip = maxAmount - amount;
					if (recip > 0)
						recip = 1d / recip;

					for (int i = tanks.Count; i-- > 0;)
					{
						var t = tanks[i];
						double a = t.amount;
						double m = t.maxAmount;
						t.amount = a + delta * (m - a) * recip;
					}
				}
				else
				{
					double recip = amount > 0 ? 1d / amount : 0;

					for (int i = tanks.Count; i-- > 0;)
					{
						var t = tanks[i];
						double a = t.amount;
						t.amount = a + delta * a * recip;
					}
				}
				amount += delta;
			}
		}

		PriorityTankSets pts = new PriorityTankSets();
		static CachedObject<TankSet> cachedTS = new CachedObject<TankSet>();
		static CachedObject<WrapPR> cachedPR = new CachedObject<WrapPR>();
		static CachedObject<WrapPPRS> cachedPPRS = new CachedObject<WrapPPRS>();

		internal void AddToSyncSet(PartResource r, int pri)
		{
			var wrap = cachedPR.Next();
			wrap.Link(r);
			pts.Add(wrap, pri);
		}

		internal void AddToSyncSet(ProtoPartResourceSnapshot r, int pri)
		{
			var wrap = cachedPPRS.Next();
			wrap.Link(r);
			pts.Add(wrap, pri);
		}

		internal void ClearSyncSet()
		{
			pts.Reset();
		}

		internal static void ResetSyncCaches()
		{
			cachedTS.Reset();
			cachedPR.Reset();
			cachedPPRS.Reset();
		}

		#endregion

		string resourceName;
		/// <summary> Associated resource name</summary>
		internal string ResourceName
		{
			get { return resourceName; }
			set
			{
				resourceName = value;
				resourceID = value.GetHashCode();
			}
		}

		int resourceID;
		int ResourceID => resourceID;

		/// <summary> Rate of change in amount per-second, this is purely for visualization</summary>
		internal double Rate { get; private set; }

		/// <summary> Rate of change in amount per-second, including average rate for interval-based rules</summary>
		internal double AverageRate { get; private set; }

		/// <summary> Amount vs capacity, or 0 if there is no capacity</summary>
		internal double Level { get; private set; }

		/// <summary> True if an interval-based rule consumption/production was processed in the last simulation step</summary>
		bool IntervalRuleHappened { get; set; }

		/// <summary> Not yet consumed or produced amount that will be synchronized to the vessel parts in Sync()</summary>
		internal double Deferred { get; private set; }

		/// <summary> Amount of resource</summary>
		internal double Amount { get; private set; }

		/// <summary> Storage capacity of resource</summary>
		internal double Capacity { get; private set; }

		/// <summary> Simulated average rate of interval-based rules in amount per-second. This is for information only, the resource is not consumed</summary>
		double intervalRulesRate;

		/// <summary> Amount consumed/produced by interval-based rules in this simulation step</summary>
		double intervalRuleAmount;

		/// <summary>Dictionary of all consumers and producers (key) and how much amount they did add/remove (value).</summary>
		Dictionary<ResourceBroker, double> brokersResourceAmounts;

		/// <summary>Dictionary of all interval-based rules (key) and their simulated average rate (value). This is for information only, the resource is not consumed</summary>
		Dictionary<ResourceBroker, double> intervalRuleBrokersRates;

		/// <summary>Ctor</summary>
		internal ResourceInfo(Vessel v, string res_name)
		{
			// remember resource name
			ResourceName = res_name;

			Deferred = 0;
			Amount = 0;
			Capacity = 0;

			brokersResourceAmounts = new Dictionary<ResourceBroker, double>();
			intervalRuleBrokersRates = new Dictionary<ResourceBroker, double>();

			// get amount & capacity
			if (v.loaded)
			{
				foreach (Part p in v.Parts)
				{
					foreach (PartResource r in p.Resources.dict.Values)
					{
						if (r.info.id == resourceID)
						{
							if (r.flowState) // has the user chosen to make a flowable resource flow
							{
								Amount += r.amount;
								Capacity += r.maxAmount;
							}
						}
#if DEBUG_RESOURCES
						// Force view all resource in Debug Mode
						r.isVisible = true;
#endif
					}
				}
			}
			else
			{
				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
				{
					foreach (ProtoPartResourceSnapshot r in p.resources)
					{
						if (r.flowState && r.definition.id == resourceID)
						{
							if (r.flowState) // has the user chosen to make a flowable resource flow
							{
								Amount += r.amount;
								Capacity += r.maxAmount;
							}
						}
					}
				}
			}

			// calculate level
			Level = Capacity > double.Epsilon ? Amount / Capacity : 0.0;
		}

		/// <summary>Record a production, it will be stored in "Deferred" and later synchronized to the vessel in Sync()</summary>
		/// <param name="brokerName">origin of the production, will be available in the UI</param>
		internal void Produce(double quantity, ResourceBroker broker)
		{
			Deferred += quantity;

			// keep track of every producer contribution for UI/debug purposes
			if (Math.Abs(quantity) < 1e-10) return;

			if (brokersResourceAmounts.ContainsKey(broker))
				brokersResourceAmounts[broker] += quantity;
			else
				brokersResourceAmounts.Add(broker, quantity);
		}

		/// <summary>Record a consumption, it will be stored in "Deferred" and later synchronized to the vessel in Sync()</summary>
		/// <param name="brokerName">origin of the consumption, will be available in the UI</param>
		internal void Consume(double quantity, ResourceBroker broker)
		{
			Deferred -= quantity;

			// keep track of every consumer contribution for UI/debug purposes
			if (Math.Abs(quantity) < 1e-10) return;

			if (brokersResourceAmounts.ContainsKey(broker))
				brokersResourceAmounts[broker] -= quantity;
			else
				brokersResourceAmounts.Add(broker, -quantity);
		}

		/// <summary>synchronize resources from cache to vessel</summary>
		/// <remarks>
		/// this function will also sync from vessel to cache so you can always use the
		/// ResourceInfo interface to get information about resources
		/// </remarks>
		internal void Sync(Vessel v, VesselData vd, double elapsed_s)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.Resource.Sync");
			// # OVERVIEW
			// - consumption/production is accumulated in "Deferred", then this function called
			// - VesselResources.Sync will fill our sync set (and collect current amount/capacity) prior to call
			// - on call, save previous step amount/capacity
			// - if amount has changed, this mean there is non-Kerbalism producers/consumers on the vessel
			// - if non-Kerbalism producers are detected on a loaded vessel, prevent high timewarp rates
			// - clamp "Deferred" to amount/capacity
			// - apply "Deferred" to all parts
			// - apply "Deferred" to amount
			// - calculate change rate per-second
			// - calculate resource level
			// - reset deferred

			// # NOTE
			// It is impossible to guarantee coherency in resource simulation of loaded vessels,
			// if consumers/producers external to the resource cache exist in the vessel (#96).
			// Such is the case for example on loaded vessels with stock solar panels.
			// The effect is that the whole resource simulation become dependent on timestep again.
			// From the user point-of-view, there are two cases:
			// - (A) the timestep-dependent error is smaller than capacity
			// - (B) the timestep-dependent error is bigger than capacity
			// In case [A], there are no consequences except a slightly wrong computed level and rate.
			// In case [B], the simulation became incoherent and from that point anything can happen,
			// like for example insta-death by co2 poisoning or climatization.
			// To avoid the consequences of [B]:
			// - we hacked the solar panels to use the resource cache (SolarPanelFixer)
			// - we detect incoherency on loaded vessels, and forbid the two highest warp speeds

			// remember vessel-wide amount currently known, to calculate rate and detect non-Kerbalism brokers
			double oldAmount = Amount;

			// remember vessel-wide capacity currently known, to detect flow state changes
			double oldCapacity = Capacity;

			// Use detected amount/capacity
			// - this detect production/consumption from stock and third-party mods
			//   that by-pass the resource cache, and flow state changes in general
			Amount = pts.amount;
			Capacity = pts.maxAmount;

			// As we haven't yet synchronized anything, changes to amount can only come from non-Kerbalism producers or consumers
			double unsupportedBrokersRate = Amount - oldAmount;
			// Avoid false detection due to precision errors
			if (Math.Abs(unsupportedBrokersRate) < 1e-05) unsupportedBrokersRate = 0.0;
			// Calculate the resulting rate
			unsupportedBrokersRate /= elapsed_s;

			// Detect flow state changes
			bool flowStateChanged = Capacity - oldCapacity > 1e-05;

			// clamp consumption/production to vessel amount/capacity
			// - if deferred is negative, then amount is guaranteed to be greater than zero
			// - if deferred is positive, then capacity - amount is guaranteed to be greater than zero
			Deferred = Lib.Clamp(Deferred, -Amount, Capacity - Amount);

			// apply deferred consumption/production to all parts
			// If the resource has a flowmode that respects priority, we'll be doing this in
			// order. If it doesn't, there will only be one priority.
			// - avoid very small values in deferred consumption/production
			if (Math.Abs(Deferred) > 1e-10)
				pts.ApplyDelta(Deferred);

			// update amount, to get correct rate and levels at all times
			Amount = pts.amount;

			// reset deferred production/consumption
			Deferred = 0.0;

			// recalculate level
			Level = Capacity > 0.0 ? Amount / Capacity : 0.0;

			// calculate rate of change per-second
			// - don't update rate during warp blending (stock modules have instabilities during warp blending) 
			// - ignore interval-based rules consumption/production
			if (!v.loaded || !Kerbalism.WarpBlending) Rate = (Amount - oldAmount - intervalRuleAmount) / elapsed_s;

			// calculate average rate of change per-second from interval-based rules
			intervalRulesRate = 0.0;
			foreach (var rb in intervalRuleBrokersRates)
			{
				intervalRulesRate += rb.Value;
			}

			// AverageRate is the exposed property that include simulated rate from interval-based rules.
			// For consistency with how "Rate" is calculated, we only add the simulated rate if there is some capacity or amount for it to have an effect
			AverageRate = Rate;
			if ((intervalRulesRate > 0.0 && Level < 1.0) || (intervalRulesRate < 0.0 && Level > 0.0)) AverageRate += intervalRulesRate;

			// For visualization purpose, update the VesselData.supplies brokers list, merging all detected sources :
			// - normal brokers that use Consume() or Produce()
			// - "virtual" brokers from interval-based rules
			// - non-Kerbalism brokers (aggregated rate)
			vd.Supply(ResourceName).UpdateResourceBrokers(brokersResourceAmounts, intervalRuleBrokersRates, unsupportedBrokersRate, elapsed_s);

			//Lib.Log("RESOURCE UPDATE : " + v);
			//foreach (var rb in vd.Supply(ResourceName).ResourceBrokers)
			//	Lib.Log(Lib.BuildString(ResourceName, " : ", rb.rate.ToString("+0.000000;-0.000000;+0.000000"), "/s (", rb.name, ")"));
			//Lib.Log("RESOURCE UPDATE END");

			// reset amount added/removed from interval-based rules
			IntervalRuleHappened = intervalRuleAmount > 0.0;
			intervalRuleAmount = 0.0;

			// if incoherent producers are detected, do not allow high timewarp speed
			// - can be disabled in settings
			// - unloaded vessels can't be incoherent, we are in full control there
			// - ignore incoherent consumers (no negative consequences for player)
			// - ignore flow state changes (avoid issue with process controllers and other things) 
			if (Settings.EnforceCoherency && v.loaded && TimeWarp.CurrentRate > 1000.0 && unsupportedBrokersRate > 0.0 && !flowStateChanged)
			{
				Message.Post
				(
				  Severity.warning,
				  Lib.BuildString
				  (
					!v.isActiveVessel ? Lib.BuildString("On <b>", v.vesselName, "</b>\na ") : "A ",
					"producer of <b>", ResourceName, "</b> has\n",
					"incoherent behavior at high warp speed.\n",
					"<i>Unload the vessel before warping</i>"
				  )
				);
				Lib.StopWarp(1000.0);
			}

			// reset brokers
			brokersResourceAmounts.Clear();
			intervalRuleBrokersRates.Clear();

			// reset amount added/removed from interval-based rules
			intervalRuleAmount = 0.0;
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <summary>estimate time until depletion, including the simulated rate from interval-based rules</summary>
		internal double DepletionTime()
		{
			// return depletion
			return Amount <= 1e-10 ? 0.0 : AverageRate >= -1e-10 ? double.NaN : Amount / -AverageRate;
		}

		/// <summary>Inform that meal has happened in this simulation step</summary>
		/// <remarks>A simulation step can cover many physics ticks, especially for unloaded vessels</remarks>
		internal void UpdateIntervalRule(double amount, double averageRate, ResourceBroker broker)
		{
			intervalRuleAmount += amount;
			intervalRulesRate += averageRate;

			if (intervalRuleBrokersRates.ContainsKey(broker))
				intervalRuleBrokersRates[broker] += averageRate;
			else
				intervalRuleBrokersRates.Add(broker, averageRate);
		}
	}

	/// <summary>
	/// ResourceRecipe is a mean of converting inputs to outputs.
	/// It does so in relation with the rest of the resource simulation to detect available amounts for inputs and available capacity for outputs.
	/// Outputs can be defined a "dumpeable" to avoid this last limitation.
	/// </summary>

	// TODO : (GOTMACHINE) the "combined" feature (ability for an input to substitute another if not available) added a lot of complexity to the Recipe code,
	// all in the purpose of fixes for the habitat resource-based atmosphere system.
	// If at some point we rewrite habitat and get ride of said resources, "combined" will not be needed anymore,
	// so it would be a good idea to revert the changes made in this commit :
	// https://github.com/Kerbalism/Kerbalism/commit/91a154b0eeda8443d9dd888c2e40ca511c5adfa3#diff-ffbaadfd7e682c9dcb3912d5f8c5cabb

	// TODO : (GOTMACHINE) At some point, we want to use "virtual" resources in recipes.
	// Their purpose would be to give the ability to scale the non-resource output of a pure consumer.
	// Example : to scale antenna data rate by EC availability, define an "antennaOutput" virtual resource and a recipe that convert EC to antennaOutput
	// then check "antennaOutput" availability to scale the amount of data sent
	// This would also allow removing the "Cures" thing.

	sealed class ResourceRecipe
	{
		internal struct Entry
		{
			internal Entry(string name, double quantity, bool dump = true, string combined = null)
			{
				this.name = name;
				this.combined = combined;
				this.quantity = quantity;
				this.inv_quantity = 1.0 / quantity;
				this.dump = dump;
			}
			internal string name;
			internal string combined;    // if entry is the primary to be combined, then the secondary resource is named here. secondary entry has its combined set to "" not null
			internal double quantity;
			internal double inv_quantity;
			internal bool dump;
		}

		List<Entry> inputs;   // set of input resources
		List<Entry> outputs;  // set of output resources
		List<Entry> cures;    // set of cures
		double left;     // what proportion of the recipe is left to execute

		ResourceBroker broker;

		internal ResourceRecipe(ResourceBroker broker)
		{
			this.inputs = new List<Entry>();
			this.outputs = new List<Entry>();
			this.cures = new List<Entry>();
			this.left = 1.0;
			this.broker = broker;
		}

		/// <summary>add an input to the recipe</summary>
		internal void AddInput(string resource_name, double quantity)
		{
			if (quantity > double.Epsilon) //< avoid division by zero
			{
				inputs.Add(new Entry(resource_name, quantity));
			}
		}

		/// <summary>add a combined input to the recipe</summary>
		internal void AddInput(string resource_name, double quantity, string combined)
		{
			if (quantity > double.Epsilon) //< avoid division by zero
			{
				inputs.Add(new Entry(resource_name, quantity, true, combined));
			}
		}

		/// <summary>add an output to the recipe</summary>
		internal void AddOutput(string resource_name, double quantity, bool dump)
		{
			if (quantity > double.Epsilon) //< avoid division by zero
			{
				outputs.Add(new Entry(resource_name, quantity, dump));
			}
		}

		/// <summary>add a cure to the recipe</summary>
		internal void AddCure(string cure, double quantity, string resource_name)
		{
			if (quantity > double.Epsilon) //< avoid division by zero
			{
				cures.Add(new Entry(cure, quantity, true, resource_name));
			}
		}

		/// <summary>Execute all recipes and record deferred consumption/production for inputs/ouputs</summary>
		internal static void ExecuteRecipes(Vessel v, VesselResources resources, List<ResourceRecipe> recipes)
		{
			bool executing = true;
			while (executing)
			{
				executing = false;
				for (int i = 0; i < recipes.Count; ++i)
				{
					ResourceRecipe recipe = recipes[i];
					if (recipe.left > double.Epsilon)
					{
						executing |= recipe.ExecuteRecipeStep(v, resources);
					}
				}
			}
		}

		/// <summary>
		/// Execute the recipe and record deferred consumption/production for inputs/ouputs.
		/// This need to be called multiple times until left &lt;= 0.0 for complete execution of the recipe.
		/// return true if recipe execution is completed, false otherwise
		/// </summary>
		bool ExecuteRecipeStep(Vessel v, VesselResources resources)
		{
			// determine worst input ratio
			// - pure input recipes can just underflow
			double worst_input = left;
			if (outputs.Count > 0)
			{
				for (int i = 0; i < inputs.Count; ++i)
				{
					Entry e = inputs[i];
					ResourceInfo res = resources.GetResource(v, e.name);

					// handle combined inputs
					if (e.combined != null)
					{
						// is combined resource the primary
						if (e.combined != "")
						{
							Entry sec_e = inputs.Find(x => x.name.Contains(e.combined));
							ResourceInfo sec = resources.GetResource(v, sec_e.name);
							double pri_worst = Lib.Clamp((res.Amount + res.Deferred) * e.inv_quantity, 0.0, worst_input);
							if (pri_worst > 0.0)
							{
								worst_input = pri_worst;
							}
							else
							{
								worst_input = Lib.Clamp((sec.Amount + sec.Deferred) * sec_e.inv_quantity, 0.0, worst_input);
							}
						}
					}
					else
					{
						worst_input = Lib.Clamp((res.Amount + res.Deferred) * e.inv_quantity, 0.0, worst_input);
					}
				}
			}

			// determine worst output ratio
			// - pure output recipes can just overflow
			double worst_output = left;
			if (inputs.Count > 0)
			{
				for (int i = 0; i < outputs.Count; ++i)
				{
					Entry e = outputs[i];
					if (!e.dump) // ignore outputs that can dump overboard
					{
						ResourceInfo res = resources.GetResource(v, e.name);
						worst_output = Lib.Clamp((res.Capacity - (res.Amount + res.Deferred)) * e.inv_quantity, 0.0, worst_output);
					}
				}
			}

			// determine worst-io
			double worst_io = Math.Min(worst_input, worst_output);

			// consume inputs
			for (int i = 0; i < inputs.Count; ++i)
			{
				Entry e = inputs[i];
				ResourceInfo res = resources.GetResource(v, e.name);
				// handle combined inputs
				if (e.combined != null)
				{
					// is combined resource the primary
					if (e.combined != "")
					{
						Entry sec_e = inputs.Find(x => x.name.Contains(e.combined));
						ResourceInfo sec = resources.GetResource(v, sec_e.name);
						double need = (e.quantity * worst_io) + (sec_e.quantity * worst_io);
						// do we have enough primary to satisfy needs, if so don't consume secondary
						if (res.Amount + res.Deferred >= need) resources.Consume(v, e.name, need, broker);
						// consume primary if any available and secondary
						else
						{
							need -= res.Amount + res.Deferred;
							res.Consume(res.Amount + res.Deferred, broker);
							sec.Consume(need, broker);
						}
					}
				}
				else 
				{
					res.Consume(e.quantity * worst_io, broker);
				}
			}

			// produce outputs
			for (int i = 0; i < outputs.Count; ++i)
			{
				Entry e = outputs[i];
				ResourceInfo res = resources.GetResource(v, e.name);
				res.Produce(e.quantity * worst_io, broker);
			}

			// produce cures
			for (int i = 0; i < cures.Count; ++i)
			{
				Entry entry = cures[i];
				List<RuleData> curingRules = new List<RuleData>();
				foreach(ProtoCrewMember crew in v.GetVesselCrew()) {
					KerbalData kd = DB.Kerbal(crew.name);
					if(kd.sickbay.IndexOf(entry.combined + ",", StringComparison.Ordinal) >= 0) {
						curingRules.Add(kd.Rule(entry.name));
					}
				}

				foreach(RuleData rd in curingRules)
				{
					rd.problem -= entry.quantity * worst_io / curingRules.Count;
					rd.problem = Math.Max(rd.problem, 0);
				}
			}

			// update amount left to execute
			left -= worst_io;

			// the recipe was executed, at least partially
			return worst_io > double.Epsilon;
		}


	}

	// equalize/vent a vessel
	static class ResourceBalance
	{
		// This Method has a lot of "For\Foreach" because it was design for multi resources
		// Method don't count disabled habitats
		internal static void Equalizer(Vessel v)
		{
			// get resource level in habitats
			double[] res_level = new double[resourceName.Length];                   // Don't count Manned or Depressiong habitats

			// Total resource in parts not disabled
			double[] totalAmount = new double[resourceName.Length];
			double[] maxAmount = new double[resourceName.Length];

			// Total resource in Enabled parts (No crew)
			double[] totalE = new double[resourceName.Length];
			double[] maxE = new double[resourceName.Length];

			// Total resource in Manned parts (Priority!)
			double[] totalP = new double[resourceName.Length];
			double[] maxP = new double[resourceName.Length];

			// Total resource in Depressurizing
			double[] totalD = new double[resourceName.Length];
			double[] maxD = new double[resourceName.Length];

			// amount to equalize speed
			double[] amount = new double[resourceName.Length];

			// Can be positive or negative, controlling the resource flow
			double flowController;

			bool[] mannedisPriority = new bool[resourceName.Length];                // The resource is priority
			bool equalize = false;                                                  // Has any resource that needs to be equalized

			// intial value
			for (int i = 0; i < resourceName.Length; i++)
			{
				totalAmount[i] = new ResourceInfo(v, resourceName[i]).Rate;        // Get generate rate for each resource
				maxAmount[i] = 0;

				totalE[i] = 0;
				maxE[i] = 0;

				totalP[i] = 0;
				maxP[i] = 0;

				totalD[i] = 0;
				maxD[i] = 0;

				mannedisPriority[i] = false;
			}

			foreach (Habitat partHabitat in v.FindPartModulesImplementing<Habitat>())
			{
				// Skip disabled habitats
				if (partHabitat.state != Habitat.State.disabled)
				{
					// Has flag to be Equalized?
					equalize |= partHabitat.needEqualize;

					PartResource[] resources = new PartResource[resourceName.Length];
					for (int i = 0; i < resourceName.Length; i++)
					{
						if (partHabitat.part.Resources.Contains(resourceName[i]))
						{
							PartResource t = partHabitat.part.Resources[resourceName[i]];

							// Manned Amounts
							if (Lib.IsCrewed(partHabitat.part))
							{
								totalP[i] += t.amount;
								maxP[i] += t.maxAmount;
							}
							// Amount for Depressurizing
							else if (partHabitat.state == Habitat.State.depressurizing)
							{
								totalD[i] += t.amount;
								maxD[i] += t.maxAmount;
							}
							else
							{
								totalE[i] += t.amount;
								maxE[i] += t.maxAmount;
							}
							totalAmount[i] += t.amount;
							maxAmount[i] += t.maxAmount;
						}
					}
				}
			}

			if (!equalize) return;

			for (int i = 0; i < resourceName.Length; i++)
			{
				// resource level for Enabled habitats no Manned
				res_level[i] = totalE[i] / (maxAmount[i] - maxP[i]);

				// Manned is priority?
				// If resource amount is less then maxAmount in manned habitat and it's flagged to equalize, define as priority
				// Using Atmosphere, N2, O2 as Priority trigger (we don't want to use CO2 as a trigger)
				if (resourceName[i] != "WasteAtmosphere" && equalize)
				{
					mannedisPriority[i] = maxP[i] - totalP[i] > 0;
				}

				// determine generic equalization speed	per resource
				if (mannedisPriority[i])
					amount[i] = maxAmount[i] * equalize_speed * Kerbalism.elapsed_s;
				else
					amount[i] = (maxE[i] + maxD[i]) * equalize_speed * Kerbalism.elapsed_s;
			}

			if (equalize)
			{
				foreach (Habitat partHabitat in v.FindPartModulesImplementing<Habitat>())
				{
					bool stillNeed = false;
					if (partHabitat.state != Habitat.State.disabled)
					{
						for (int i = 0; i < resourceName.Length; i++)
						{
							if (partHabitat.part.Resources.Contains(resourceName[i]))
							{
								PartResource t = partHabitat.part.Resources[resourceName[i]];
								flowController = 0;

								// Conditions in order
								// If perctToMax = 0 (means Habitat will have 0% of amount:
								//	1 case: modules still needs to be equalized
								//	2 case: has depressurizing habitat
								//	3 case: dropping everything into the priority habitats

								if ((Math.Abs(res_level[i] - (t.amount / t.maxAmount)) > precision && !Lib.IsCrewed(partHabitat.part))
									|| ((partHabitat.state == Habitat.State.depressurizing
									|| mannedisPriority[i]) && t.amount > double.Epsilon))
								{
									double perctToAll;              // Percent of resource for this habitat related
									double perctRest;               // Percent to fill priority

									perctToAll = t.amount / maxAmount[i];

									double perctToType;
									double perctToMaxType;

									// Percts per Types
									if (Lib.IsCrewed(partHabitat.part))
									{
										perctToType = t.amount / totalP[i];
										perctToMaxType = t.maxAmount / maxP[i];
									}
									else if (partHabitat.state == Habitat.State.depressurizing)
									{
										perctToType = t.amount / totalD[i];
										perctToMaxType = t.maxAmount / maxD[i];
									}
									else
									{
										perctToType = t.amount / totalE[i];
										perctToMaxType = t.maxAmount / maxE[i];
									}

									// Perct from the left resource
									if (totalAmount[i] - maxP[i] <= 0 || partHabitat.state == Habitat.State.depressurizing)
									{
										perctRest = 0;
									}
									else
									{
										perctRest = (((totalAmount[i] - maxP[i]) * perctToMaxType) - t.amount) / totalE[i];
									}

									// perctToMax < perctToAll ? habitat will send resource : otherwise will receive, flowController == 0 means no flow
									if ((partHabitat.state == Habitat.State.depressurizing || totalAmount[i] - maxP[i] <= 0) && !Lib.IsCrewed(partHabitat.part))
									{
										flowController = 0 - perctToType;
									}
									else if (mannedisPriority[i] && !Lib.IsCrewed(partHabitat.part))
									{
										flowController = Math.Min(perctToMaxType - perctToAll, (t.maxAmount - t.amount) / totalAmount[i]);
									}
									else
									{
										flowController = perctRest;
									}

									// clamp amount to what's available in the hab and what can fit in the part
									double amountAffected;
									if (partHabitat.state == Habitat.State.depressurizing)
									{
										amountAffected = flowController * totalD[i];
									}
									else if (!mannedisPriority[i] || !Lib.IsCrewed(partHabitat.part))
									{
										amountAffected = flowController * totalE[i];
									}
									else
									{
										amountAffected = flowController * totalP[i];
									}

									amountAffected *= equalize_speed;

									amountAffected = Math.Sign(amountAffected) >= 0 ? Math.Max(Math.Sign(amountAffected) * precision, amountAffected) : Math.Min(Math.Sign(amountAffected) * precision, amountAffected);

									double va = amountAffected < 0.0
										? Math.Abs(amountAffected) > t.amount                // If negative, habitat can't send more than it has
										? t.amount * (-1)
										: amountAffected
										: Math.Min(amountAffected, t.maxAmount - t.amount);  // if positive, habitat can't receive more than max

									va = Double.IsNaN(va) ? 0.0 : va;

									// consume relative percent of this part
									t.amount += va;

									if (va < double.Epsilon) stillNeed = false;
									else stillNeed = true;
								}
							}
						}
					}

					partHabitat.needEqualize = stillNeed;
				}


			}
		}

		// constants
		const double equalize_speed = 0.01;  // equalization/venting mutiple speed per-second, in proportion to amount

		// Resources to equalize
		internal static string[] resourceName = new string[2] { "Atmosphere", "WasteAtmosphere" };

		// Resources to equalize
		internal static double precision = 0.00001;
	}
}
