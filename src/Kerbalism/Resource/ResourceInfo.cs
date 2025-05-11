using System.Collections.Generic;

namespace KERBALISM
{
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
			if (System.Math.Abs(quantity) < 1e-10) return;

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
			if (System.Math.Abs(quantity) < 1e-10) return;

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
			if (System.Math.Abs(unsupportedBrokersRate) < 1e-05) unsupportedBrokersRate = 0.0;
			// Calculate the resulting rate
			unsupportedBrokersRate /= elapsed_s;

			// Detect flow state changes
			bool flowStateChanged = Capacity - oldCapacity > 1e-05;

			// clamp consumption/production to vessel amount/capacity
			// - if deferred is negative, then amount is guaranteed to be greater than zero
			// - if deferred is positive, then capacity - amount is guaranteed to be greater than zero
			Deferred = Math.Clamp(Deferred, -Amount, Capacity - Amount);

			// apply deferred consumption/production to all parts
			// If the resource has a flowmode that respects priority, we'll be doing this in
			// order. If it doesn't, there will only be one priority.
			// - avoid very small values in deferred consumption/production
			if (System.Math.Abs(Deferred) > 1e-10)
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

			//Logging.Log("RESOURCE UPDATE : " + v);
			//foreach (var rb in vd.Supply(ResourceName).ResourceBrokers)
			//	Logging.Log(String.BuildString(ResourceName, " : ", rb.rate.ToString("+0.000000;-0.000000;+0.000000"), "/s (", rb.name, ")"));
			//Logging.Log("RESOURCE UPDATE END");

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
				  String.BuildString
				  (
					!v.isActiveVessel ? String.BuildString("On <b>", v.vesselName, "</b>\na ") : "A ",
					"producer of <b>", ResourceName, "</b> has\n",
					"incoherent behavior at high warp speed.\n",
					"<i>Unload the vessel before warping</i>"
				  )
				);
				Time.StopWarp(1000.0);
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
}
