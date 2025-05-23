using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
	class Reliability : PartModule, ISpecifics, IModuleInfo, IPartCostModifier, IPartMassModifier
	{
		// config
		[KSPField(isPersistant = true)] public string type;                 // component name
		[KSPField] public double mtbf = 3600 * 6 * 1000;                    // mean time between failures, in seconds
		[KSPField] public string repair = string.Empty;                     // repair crew specs
		[KSPField] public string title = string.Empty;                      // short description of component
		[KSPField] public string redundancy = string.Empty;                 // redundancy group
		[KSPField] public double extra_cost;                                // extra cost for high-quality, in proportion of part cost
		[KSPField] public double extra_mass;                                // extra mass for high-quality, in proportion of part mass

		[KSPField] public double rated_radiation = 0;                       // rad/h this part can sustain without taking any damage. Only effective with MTBF failures.
		[KSPField] public double radiation_decay_rate = 1;                  // time to next failure is reduced by (rad/h - rated_radiation) * radiation_decay_rate per second

		// engine only features
		[KSPField] public double turnon_failure_probability = -1;           // probability of a failure when turned on or staged
		[KSPField] public double rated_operation_duration = -1;             // failure rate increases dramatically if this is exceeded
		[KSPField] public int rated_ignitions = -1;                         // failure rate increases dramatically if this is exceeded

		// persistence
		[KSPField(isPersistant = true)] public bool broken;                 // true if broken
		[KSPField(isPersistant = true)] public bool critical;               // true if failure can't be repaired
		[KSPField(isPersistant = true)] public bool quality;                // true if the component is high-quality
		[KSPField(isPersistant = true)] public double last = 0.0;           // time of last failure
		[KSPField(isPersistant = true)] public double next = 0.0;           // time of next failure
		[KSPField(isPersistant = true)] public double last_inspection = 0.0;   // time of last service
		[KSPField(isPersistant = true)] public bool needMaintenance = false;// true when component is inspected and about to fail
		[KSPField(isPersistant = true)] public bool enforce_breakdown = false; // true when the next failure is enforced
		[KSPField(isPersistant = true)] public bool running = false;        // true when the next failure is enforced
		[KSPField(isPersistant = true)] public double operation_duration = 0.0; // failure rate increases dramatically if this is exceeded
		[KSPField(isPersistant = true)] public double fail_duration = 0.0;  // fail when operation_duration exceeds this
		[KSPField(isPersistant = true)] public int ignitions = 0;           // accumulated ignitions

		// status ui
		[KSPField(guiActive = true, guiActiveEditor = true, guiName = "_", groupName = "Reliability", groupDisplayName = "#KERBALISM_Group_Reliability")]//Reliability
		public string Status; // show component status

		// data
		List<PartModule> modules;                                           // components cache
		CrewSpecs repair_cs;                                                // crew specs
		bool explode = false;

		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;

			Fields["Status"].guiName = title;
#if DEBUG_RELIABILITY
			Events["Break"].guiName = "Break " + title + " [DEBUG]";
#endif

			// do nothing in the editors and when compiling parts
			if (!GameLogic.IsFlight()) return;

			if (last_inspection <= 0) last_inspection = Planetarium.GetUniversalTime();

			// cache list of modules
			if(type.StartsWith("ModuleEngines", StringComparison.Ordinal))
			{
				// do this generically. there are many different engine types derived from ModuleEngines:
				// ModuleEnginesFX, ModuleEnginesRF, all the SolverEngines, possibly more
				// this will also reduce the amount of configuration overhead, no need to duplicate the same
				// config for stock with ModuleEngines and ModuleEnginesFX
				modules = new List<PartModule>();
				var engines = part.FindModulesImplementing<ModuleEngines>();
                foreach (var engine in engines)
                {
					modules.Add(engine);
                }
            }
			else
			{
				modules = part.FindModulesImplementing<PartModule>().FindAll(k => k.moduleName == type);
			}

			// parse crew specs
			repair_cs = new CrewSpecs(repair);

			// setup ui
			Events["Inspect"].guiName = Local.Reliability_Inspect.Format("<b>"+title+"</b>");//String.BuildString("Inspect <<1>>)
			Events["Repair"].guiName = Local.Reliability_Repair.Format("<b>"+title+"</b>");//String.BuildString("Repair <<1>>)
			
			// sync monobehaviour state with module state
			// - required as the monobehaviour state is not serialized
			if (broken)
			{
				foreach (PartModule m in modules)
				{
					m.enabled = false;
					m.isEnabled = false;
				}
			}

			if(broken) StartCoroutine(DeferredApply());
		}

		IEnumerator DeferredApply()
		{
			// wait until vessel is unpacked. doing this will ensure that module
			// specific hacks are executed after the module itself was OnStart()ed.
			yield return new WaitUntil(() => !vessel.packed);
			if (broken)
			{
				Apply(true);
			}
		}

		/// <summary> Returns true if a failure should be triggered. </summary>
		bool IgnitionCheck()
		{
			if (!PreferencesReliability.Instance.engineFailures)
				return false;

			// don't check for a couple of seconds after the vessel was loaded.
			// when loading a quicksave with the engines running, the engine state
			// is off at first which would cost an ignition and possibly trigger a failure
			if (UnityEngine.Time.time < Kerbalism.gameLoadTime + 3)
				return false;

			ignitions++;
			vessel.KerbalismData().ResetReliabilityStatus();

			bool fail = false;
			bool launchpad = vessel.situation == Vessel.Situations.PRELAUNCH || ignitions <= 1 && vessel.situation == Vessel.Situations.LANDED;

			if (turnon_failure_probability > 0)
			{
				// q = quality indicator
				var q = quality ? Settings.QualityScale : 1.0;
				if (launchpad) q /= 2.5; // the very first ignition is more likely to fail

				q += Math.Clamp(ignitions - 1, 0.0, 6.0) / 20.0; // subsequent ignition failures become less and less likely, reducing by up to 30%

				if (Random.RandomDouble() < (turnon_failure_probability * PreferencesReliability.Instance.ignitionFailureChance) / q)
				{
					fail = true;
#if DEBUG_RELIABILITY
					Logging.Log("Ignition check: " + part.partInfo.title + " ignitions " + ignitions + " turnon failure");
#endif
				}
			}

			// 

			if (rated_ignitions > 0)
			{
				int total_ignitions = EffectiveIgnitions(quality, rated_ignitions);
				if (ignitions >= total_ignitions * 0.9) needMaintenance = true;
				if (ignitions > total_ignitions)
				{
					var q = (quality ? Settings.QualityScale : 1.0) * Random.RandomDouble();
					q /= PreferencesReliability.Instance.ignitionFailureChance;
					q /= (ignitions - total_ignitions); // progressively increase the odds of a failure with every extra ignition

#if DEBUG_RELIABILITY
					Logging.Log("Reliability: ignition exceeded q=" + q + " ignitions=" + ignitions + " total_ignitions=" + total_ignitions);
#endif

					if (q < 0.3)
					{
						fail = true;
					}
				}
			}

			if (fail)
			{
				enforce_breakdown = true;
				explode = Random.RandomDouble() < 0.1;

				next = Planetarium.GetUniversalTime() + Random.RandomDouble() * 2.0;

				var fuelSystemFailureProbability = 0.1;
				if (launchpad) fuelSystemFailureProbability = 0.5;

				if(Random.RandomDouble() < fuelSystemFailureProbability)
				{
					// broken fuel line -> delayed kaboom
					explode = true;
					next += Random.RandomDouble() * 10 + 4;
					FlightLogger.fetch?.LogEvent(Local.FlightLogger_Destruction.Format(part.partInfo.title));// <<1>> fuel system leak caused destruction of the engine"
				}
				else
				{
					FlightLogger.fetch?.LogEvent(Local.FlightLogger_Ignition.Format(part.partInfo.title));// <<1>> failure on ignition"
				}
			}
			return fail;
		}

		void Update()
		{
			if (GameLogic.IsFlight())
			{
				// enforce state
				// - required as things like Configure or AnimationGroup can re-enable broken modules
				if (broken)
				{
					foreach (PartModule m in modules)
					{
						m.enabled = false;
						m.isEnabled = false;
					}
				}

				// update ui
				if (part.IsPAWVisible())
				{
					Status = string.Empty;

					if (broken)
					{
						Status = critical ? String.Color(Local.Reliability_criticalfailure, String.Kolor.Red) : String.Color(Local.Reliability_malfunction, String.Kolor.Yellow);//"critical failure""malfunction"
					}
					else
					{
						if (PreferencesReliability.Instance.engineFailures && (rated_operation_duration > 0 || rated_ignitions > 0))
						{
							if (rated_operation_duration > 0)
							{
								double effective_duration = EffectiveDuration(quality, rated_operation_duration);
								Status = String.BuildString(Local.Reliability_burnremaining, " ", HumanReadable.Duration(System.Math.Max(0, effective_duration - operation_duration)));//"remaining burn:"
							}
							if (rated_ignitions > 0)
							{
								int effective_ignitions = EffectiveIgnitions(quality, rated_ignitions);
								Status = String.BuildString(Status,
									(string.IsNullOrEmpty(Status) ? "" : ", "),
									Local.Reliability_ignitions, " ", System.Math.Max(0, effective_ignitions - ignitions).ToString());//"ignitions:"
							}
						}

						if (rated_radiation > 0)
						{
							var rated = quality ? rated_radiation * Settings.QualityScale : rated_radiation;
							var current = vessel.KerbalismData().EnvironmentRadiation * 3600.0;
							if (rated < current)
							{
								Status = String.BuildString(Status, (string.IsNullOrEmpty(Status) ? "" : ", "), String.Color(Local.Reliability_takingradiationdamage, String.Kolor.Orange));//"taking radiation damage"
							}
						}
					}

					if (string.IsNullOrEmpty(Status)) Status = Local.Generic_NOMINAL;//"nominal"

					Events["Inspect"].active = !broken && !needMaintenance;
					Events["Repair"].active = repair_cs && (broken || needMaintenance) && !critical;

					if (needMaintenance)
					{
						Events["Repair"].guiName = Local.Reliability_Service.Format("<b>" + title + "</b>");//String.BuildString("Service <<1>>")
					}
				}

				RunningCheck();

				// if it has failed, trigger malfunction
				var now = Planetarium.GetUniversalTime();
				if (next > 0 && now > next && !broken)
				{
#if DEBUG_RELIABILITY
					Logging.Log("Reliablity: breakdown for " + part.partInfo.title);
#endif
					Break();
				}

				// set highlight
				Highlight(part);
			}
			else
			{
				// update ui
				if (part.IsPAWVisible())
				{
					Events["Quality"].guiName = UI.StatusToggle(Local.Reliability_qualityinfo.Format("<b>" + title + "</b>"), quality ? Local.Reliability_qualityhigh : Local.Reliability_qualitystandard);//String.BuildString(<<1>> quality")"high""standard"

					Status = string.Empty;
					if (mtbf > 0 && PreferencesReliability.Instance.mtbfFailures)
					{
						double effective_mtbf = EffectiveMTBF(quality, mtbf);
						Status = String.BuildString(Status,
							(string.IsNullOrEmpty(Status) ? "" : ", "),
							Local.Reliability_MTBF + " ", HumanReadable.Duration(effective_mtbf));//"MTBF:"
					}

					if (rated_operation_duration > 0 && PreferencesReliability.Instance.engineFailures)
					{
						double effective_duration = EffectiveDuration(quality, rated_operation_duration);
						Status = String.BuildString(Status,
							(string.IsNullOrEmpty(Status) ? "" : ", "),
							Local.Reliability_Burntime + " ",//"Burn time:
							HumanReadable.Duration(effective_duration));
					}

					if (rated_ignitions > 0 && PreferencesReliability.Instance.engineFailures)
					{
						int effective_ignitions = EffectiveIgnitions(quality, rated_ignitions);
						Status = String.BuildString(Status,
							(string.IsNullOrEmpty(Status) ? "" : ", "),
							Local.Reliability_ignitions + " ", effective_ignitions.ToString());//"ignitions:
					}

					if (rated_radiation > 0 && PreferencesReliability.Instance.mtbfFailures)
					{
						var r = quality ? rated_radiation * Settings.QualityScale : rated_radiation;
						Status = String.BuildString(Status,
							(string.IsNullOrEmpty(Status) ? "" : ", "),
							HumanReadable.Radiation(r / 3600.0));
					}
				}
			}
		}

		void FixedUpdate()
		{
			// do nothing in the editor
			if (GameLogic.IsEditor()) return;

			var now = Planetarium.GetUniversalTime();

			// if it has not malfunctioned
			if (!broken && mtbf > 0 && PreferencesReliability.Instance.mtbfFailures)
			{
				// calculate time of next failure if necessary
				if (next <= 0)
				{
					last = now;
					var guaranteed = mtbf / 2.0;
					var r = 1 - System.Math.Pow(Random.RandomDouble(), 3);
					next = now + guaranteed + mtbf * (quality ? Settings.QualityScale : 1.0) * r;
#if DEBUG_RELIABILITY
					Logging.Log("Reliability: MTBF failure in " + (now - next) + " for " + part.partInfo.title);
#endif
				}

				var decay = RadiationDecay(quality, vessel.KerbalismData().EnvironmentRadiation, Kerbalism.elapsed_s, rated_radiation, radiation_decay_rate);
				next -= decay;
			}
		}

		double nextRunningCheck = 0.0;
		double lastRunningCheck = 0.0;

		/// <summary>
		/// This checks burn time and ignition failures, which is intended for engines only.
		/// Since engines don't run on on unloaded vessels in KSP, this is only implemented
		/// for loaded vessels.
		/// </summary>
		void RunningCheck()
		{
			if (!PreferencesReliability.Instance.engineFailures) return;

			// don't count engine running time during time warp (see https://github.com/Kerbalism/Kerbalism/pull/646)
			if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > 1)
			{
				// forget that the engine was running when we're warping. otherwise we might consume engine time
				// during warp when it actually didn't run because of KSP physics
				lastRunningCheck = 0;
				return;
			}

			if (broken || enforce_breakdown || turnon_failure_probability <= 0 && rated_operation_duration <= 0) return;
			double now = Planetarium.GetUniversalTime();
			if (now < nextRunningCheck) return;
			nextRunningCheck = now + 0.5; // twice a second is fast enough for a smooth countdown in the PAW

			if (!running)
			{
				if (IsRunning())
				{
					running = true;
					if (IgnitionCheck())
						Break();
				}
			}
			else
			{
				running = IsRunning();
			}

			if (running && rated_operation_duration > 1 && lastRunningCheck > 0)
			{
				var duration = now - lastRunningCheck;
				operation_duration += duration;
				vessel.KerbalismData().ResetReliabilityStatus();

				if (fail_duration <= 0)
				{
					// see https://www.desmos.com/calculator/l2sdoauiyw
					// these values will give the following result:
					// 0% failure at 40% of the operation duration
					// 1.68% failure at 100% of the operation duration
					// 100% failure at 140% of the operation duration

					int r = 8; // the random number exponent
					var g = 0.4; // guarantee factor

					// calculate a random point on which the engine will fail

					var f = rated_operation_duration;
					if (quality) f *= Settings.QualityScale;

					// extend engine burn duration by preference value 
					f /= PreferencesReliability.Instance.engineOperationFailureChance;

					// random^r so we get an exponentially increasing failure probability
					var p = System.Math.Pow(Random.RandomDouble(), r);

					// 1-p turns the probability of failure into one of non-failure
					p = 1 - p;

					// 35% guaranteed burn duration
					var guaranteed_operation = f * g;

					fail_duration = guaranteed_operation + f * p;
#if DEBUG_RELIABILITY
					Logging.Log(part.partInfo.title + " will fail after " + HumanReadable.Duration(fail_duration) + " burn time");
#endif
				}

				if (fail_duration < operation_duration)
				{
					next = now;
					enforce_breakdown = true;
					explode = Random.RandomDouble() < 0.2;
#if DEBUG_RELIABILITY
					Logging.Log("Reliability: " + part.partInfo.title + " fails because of material fatigue");
#endif
					FlightLogger.fetch?.LogEvent(Local.FlightLogger_MaterialFatigue.Format(part.partInfo.title));// <<1>> failed because of material fatigue"
				}
			}

			lastRunningCheck = now;
		}

		internal static void BackgroundUpdate(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, Reliability reliability, double elapsed_s)
		{
			if(!PreferencesReliability.Instance.mtbfFailures) return;

			// check for existing malfunction and if it actually uses MTBF failures
			if (Lib.Proto.GetBool(m, "broken")) return;
			if (reliability.mtbf <= 0) return;

			// get time of next failure
			double next = Lib.Proto.GetDouble(m, "next");
			bool quality = Lib.Proto.GetBool(m, "quality");
			var now = Planetarium.GetUniversalTime();

			// calculate epoch of failure if necessary
			if (next <= 0)
			{
				var guaranteed = reliability.mtbf / 2.0;
				var r = 1 - System.Math.Pow(Random.RandomDouble(), 3);
				next = now + guaranteed + reliability.mtbf * (quality ? Settings.QualityScale : 1.0) * r;
				Lib.Proto.Set(m, "last", now);
				Lib.Proto.Set(m, "next", next);
#if DEBUG_RELIABILITY
				Logging.Log("Reliability: background MTBF failure in " + (now - next) + " for " + p);
#endif
			}

			var rad = v.KerbalismData().EnvironmentRadiation;
			var decay = RadiationDecay(quality, rad, elapsed_s, reliability.rated_radiation, reliability.radiation_decay_rate);
			if (decay > 0)
			{
				next -= decay;
				Lib.Proto.Set(m, "next", next);
			}

			// if it has failed, trigger malfunction
			if (now > next)
			{
#if DEBUG_RELIABILITY
				Logging.Log("Reliablity: background MTBF failure for " + p);
#endif
					ProtoBreak(v, p, m);
			}
		}

		[KSPEvent(guiActiveEditor = true, guiName = "_", active = true, groupName = "Reliability", groupDisplayName = "#KERBALISM_Group_Reliability")]//Reliability
		// toggle between standard and high quality
		public void Quality()
		{
			quality = !quality;

			// sync all other modules in the symmetry group
			foreach (Part p in part.symmetryCounterparts)
			{
				Reliability reliability = p.Modules[part.Modules.IndexOf(this)] as Reliability;
				if (reliability != null)
				{
					reliability.quality = !reliability.quality;
				}
			}

			// refresh VAB/SPH ui
			if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

		[KSPEvent(guiActiveUnfocused = true, unfocusedRange = 3.5f, guiName = "_", active = false, groupName = "Reliability", groupDisplayName = "#KERBALISM_Group_Reliability")]//Reliability
		// show a message with some hint on time to next failure
		public void Inspect()
		{
			// disable for dead eva kerbals
			Vessel v = FlightGlobals.ActiveVessel;
			if (v == null || EVA.IsDeadEVA(v)) return;

			// get normalized time to failure
			double time_k = (Planetarium.GetUniversalTime() - last) / (next - last);
			needMaintenance = mtbf > 0 && time_k > 0.35;

			if (rated_ignitions > 0 && ignitions >= System.Math.Ceiling(EffectiveIgnitions(quality, rated_ignitions) * 0.4)) needMaintenance = true;
			if (rated_operation_duration > 0 && operation_duration >= EffectiveDuration(quality, rated_operation_duration) * 0.4) needMaintenance = true;

			v.KerbalismData().ResetReliabilityStatus();

			// notify user
			if (!needMaintenance)
			{
				last_inspection = Planetarium.GetUniversalTime();
				Message.Post(String.TextVariant(
					Local.Reliability_MessagePost1,//"It is practically new"
					Local.Reliability_MessagePost2,//"It is in good shape"
					Local.Reliability_MessagePost3,//"This will last for ages"
					Local.Reliability_MessagePost4,//"Brand new!"
					Local.Reliability_MessagePost5//"Doesn't look used. Is this even turned on?"
				));
			}
			else
			{
				Message.Post(String.TextVariant(
					Local.Reliability_MessagePost6,//"Looks like it's going to fall off soon."
					Local.Reliability_MessagePost7,//"Better get the duck tape ready!"
					Local.Reliability_MessagePost8,//"It is reaching its operational limits."
					Local.Reliability_MessagePost9,//"How is this still working?"
					Local.Reliability_MessagePost10//"It could fail at any moment now."
				));
			}
		}

		[KSPEvent(guiActiveUnfocused = true, unfocusedRange = 3.5f, guiName = "_", active = false, groupName = "Reliability", groupDisplayName = "#KERBALISM_Group_Reliability")]//Reliability
		// repair malfunctioned component
		public void Repair()
		{
			// disable for dead eva kerbals
			Vessel v = FlightGlobals.ActiveVessel;
			if (v == null || EVA.IsDeadEVA(v)) return;

			// check trait
			if (!repair_cs.Check(v))
			{
				Message.Post
				(
				  String.TextVariant
				  (
					Local.Reliability_MessagePost11,//"I'm not qualified for this"
					Local.Reliability_MessagePost12,//"I will not even know where to start"
					Local.Reliability_MessagePost13//"I'm afraid I can't do that"
				  ),
				  repair_cs.Warning()
				);
				return;
			}

			needMaintenance = false;
			enforce_breakdown = false;

			// reset times
			last = 0.0;
			next = 0.0;
			lastRunningCheck = 0;
			last_inspection = Planetarium.GetUniversalTime();

			operation_duration = 0;
			ignitions = 0;
			fail_duration = 0;
			vessel.KerbalismData().ResetReliabilityStatus();

			if (broken)
			{
				// flag as not broken
				broken = false;

				// re-enable module
				foreach (PartModule m in modules)
				{
					m.isEnabled = true;
					m.enabled = true;
				}

				// we need to reconfigure the module here, because if all modules of a type
				// share the broken state, and these modules are part of a configure setup,
				// then repairing will enable all of them, messing up with the configuration
				part.FindModulesImplementing<Configure>().ForEach(k => k.DoConfigure());

				// type-specific hacks
				Apply(false);

				// notify user
				Message.Post
				(
				  Local.Reliability_MessagePost14.Format("<b>"+title+"</b>"),//String.BuildString("<<1>> repaired")
				  String.TextVariant
				  (
					Local.Reliability_MessagePost15,//"A powerkick did the trick."
					Local.Reliability_MessagePost16,//"Duct tape, is there something it can't fix?"
					Local.Reliability_MessagePost17,//"Fully operational again."
					Local.Reliability_MessagePost18//"We are back in business."
				  )
				);
			} else {
				// notify user
				Message.Post
				(
				  Local.Reliability_MessagePost19.Format("<b>"+title+"</b>"),//String.BuildString(<<1>> serviced")
				  String.TextVariant
				  (
					Local.Reliability_MessagePost20,//"I don't know how this was still working."
					Local.Reliability_MessagePost21,//"Fastened that loose screw."
					Local.Reliability_MessagePost22,//"Someone forgot a toothpick in there."
					Local.Reliability_MessagePost23//"As good as new!"
				  )
				);
			}
		}

#if DEBUG_RELIABILITY
		[KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "_", active = true)] // [for testing]
		public
#endif
		void Break()
		{
			vessel.KerbalismData().ResetReliabilityStatus();

			if (broken) return;

			if (explode)
			{
				foreach (PartModule m in modules)
					m.part.explode();
				return;
			}

			// if enforced, manned, or if safemode didn't trigger
			if (enforce_breakdown || vessel.KerbalismData().CrewCapacity > 0 || Random.RandomDouble() > PreferencesReliability.Instance.safeModeChance)
			{
				// flag as broken
				broken = true;

				// determine if this is a critical failure
				critical = Random.RandomDouble() < PreferencesReliability.Instance.criticalChance;

				// disable module
				foreach (PartModule m in modules)
				{
					m.isEnabled = false;
					m.enabled = false;
				}

				// type-specific hacks
				Apply(true);

				// notify user
				Broken_msg(vessel, title, critical);
			}
			// safemode
			else
			{
				// reset age
				last = 0.0;
				next = 0.0;

				// notify user
				Safemode_msg(vessel, title);
			}

			// in any case, incentive redundancy
			if (PreferencesReliability.Instance.incentiveRedundancy)
			{
				Incentive_redundancy(vessel, redundancy);
			}
		}

		static void ProtoBreak(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m)
		{
			v.KerbalismData().ResetReliabilityStatus();

			// get reliability module prefab
			string type = Lib.Proto.GetString(m, "type", string.Empty);
			Reliability reliability = p.partPrefab.FindModulesImplementing<Reliability>().Find(k => k.type == type);
			if (reliability == null) return;

			bool enforce_breakdown = Lib.Proto.GetBool(m, "enforce_breakdown", false);

			// if manned, or if safemode didn't trigger
			if (enforce_breakdown || v.KerbalismData().CrewCapacity > 0 || Random.RandomDouble() > PreferencesReliability.Instance.safeModeChance)
			{
				// flag as broken
				Lib.Proto.Set(m, "broken", true);

				// determine if this is a critical failure
				bool critical = Random.RandomDouble() < PreferencesReliability.Instance.criticalChance;
				Lib.Proto.Set(m, "critical", critical);

				// for each associated module
				foreach (var proto_module in p.modules.FindAll(k => k.moduleName == reliability.type))
				{
					// disable the module
					Lib.Proto.Set(proto_module, "isEnabled", false);
				}

				// type-specific hacks
				switch (reliability.type)
				{
					case "ProcessController":
						foreach (ProcessController pc in p.partPrefab.FindModulesImplementing<ProcessController>())
						{
							ProtoPartResourceSnapshot res = p.resources.Find(k => k.resourceName == pc.resource);
							if (res != null) res.flowState = false;
						}
						break;
				}

				// show message
				Broken_msg(v, reliability.title, critical);
			}
			// safe mode
			else
			{
				// reset age
				Lib.Proto.Set(m, "last", 0.0);
				Lib.Proto.Set(m, "next", 0.0);

				// notify user
				Safemode_msg(v, reliability.title);
			}

			// in any case, incentive redundancy
			if (PreferencesReliability.Instance.incentiveRedundancy)
			{
				Incentive_redundancy(v, reliability.redundancy);
			}
		}

		// part tooltip
		public override string GetInfo()
		{
			return Specs().Info();
		}

		internal static double EffectiveMTBF(bool quality, double mtbf)
		{
			return mtbf * (quality ? Settings.QualityScale : 1.0);
		}

		internal static double EffectiveDuration(bool quality, double duration)
		{
			return duration * (quality ? Settings.QualityScale : 1.0);
		}

		internal static int EffectiveIgnitions(bool quality, int ignitions)
		{
			if(quality) return ignitions + (int)System.Math.Ceiling(ignitions * Settings.QualityScale * 0.2);
			return ignitions;
		}

		static double RadiationDecay(bool quality, double rad, double elapsed_s, double rated_radiation, double radiation_decay_rate)
		{
			rad *= 3600.0;
			if (quality) rated_radiation *= Settings.QualityScale;
			if (rad <= 0 || rated_radiation <= 0 || rad < rated_radiation) return 0.0;

			rad -= rated_radiation;

			return rad * elapsed_s * radiation_decay_rate;
		}

		// specifics support
		public Specifics Specs()
		{
			Specifics specs = new Specifics();
			if (redundancy.Length > 0) specs.Add(Local.Reliability_info1, redundancy);//"Redundancy"
			specs.Add(Local.Reliability_info2, new CrewSpecs(repair).Info());//"Repair"

			

			specs.Add(string.Empty);
			specs.Add("<color=#00ffff>"+Local.Reliability_info3 +"</color>");//Standard quality
			if(mtbf > 0) specs.Add(Local.Reliability_info4, HumanReadable.Duration(EffectiveMTBF(false, mtbf)));//"MTBF"
			if (turnon_failure_probability > 0) specs.Add(Local.Reliability_info5, HumanReadable.Percentage(turnon_failure_probability, "F1"));//"Ignition failures"
			if (rated_operation_duration > 0) specs.Add(Local.Reliability_info6, HumanReadable.Duration(EffectiveDuration(false, rated_operation_duration)));//"Rated burn duration"
			if (rated_ignitions > 0) specs.Add(Local.Reliability_info7, EffectiveIgnitions(false, rated_ignitions).ToString());//"Rated ignitions"
			if (mtbf > 0 && rated_radiation > 0) specs.Add(Local.Reliability_info8, HumanReadable.Radiation(rated_radiation / 3600.0));//"Radiation rating"

			specs.Add(string.Empty);
			specs.Add("<color=#00ffff>"+Local.Reliability_info9 +"</color>");//High quality
			if (extra_cost > double.Epsilon) specs.Add(Local.Reliability_info10, HumanReadable.Cost(extra_cost * part.partInfo.cost));//"Extra cost"
			if (extra_mass > double.Epsilon) specs.Add(Local.Reliability_info11, HumanReadable.Mass(extra_mass * part.partInfo.partPrefab.mass));//"Extra mass"
			if (mtbf > 0) specs.Add(Local.Reliability_info4, HumanReadable.Duration(EffectiveMTBF(true, mtbf)));//"MTBF"
			if (turnon_failure_probability > 0) specs.Add(Local.Reliability_info5, HumanReadable.Percentage(turnon_failure_probability / Settings.QualityScale, "F1"));//"Ignition failures"
			if (rated_operation_duration > 0) specs.Add(Local.Reliability_info6, HumanReadable.Duration(EffectiveDuration(true, rated_operation_duration)));//"Rated burn duration"
			if (rated_ignitions > 0) specs.Add(Local.Reliability_info7, EffectiveIgnitions(true, rated_ignitions).ToString());//"Rated ignitions"
			if (mtbf > 0 && rated_radiation > 0) specs.Add(Local.Reliability_info8, HumanReadable.Radiation(Settings.QualityScale * rated_radiation / 3600.0));//"Radiation rating"

			return specs;
		}

		// module info support
		public string GetModuleTitle() { return String.BuildString(title, " Reliability"); }
		public override string GetModuleDisplayName() { return String.BuildString(title, " ",Local.Reliability_Reliability); }//Reliability
		public string GetPrimaryField() { return string.Empty; }
		public Callback<Rect> GetDrawModulePanelCallback() { return null; }


		// module cost support
		public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) { return quality ? (float)extra_cost * part.partInfo.cost : 0.0f; }


		// module mass support
		public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) { return quality ? (float)extra_mass * part.partInfo.partPrefab.mass : 0.0f; }
		public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }
		public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

		bool IsRunning()
		{
			if (type.StartsWith("ModuleEngines", StringComparison.Ordinal))
			{
				foreach (PartModule m in modules)
				{
					var e = m as ModuleEngines;
					return e.currentThrottle > 0 && e.EngineIgnited && e.resultingThrust > 0;
				}
				return false;
			}

			switch (type)
			{
				case "ProcessController":
					foreach (PartModule m in modules)
						return (m as ProcessController).running;
					return false;

				case "ModuleLight":
					foreach (PartModule m in modules)
						return (m as ModuleLight).isOn;
					return false;
			}

			return false;
		}

		// apply type-specific hacks to enable/disable the module
		void Apply(bool b)
		{
			if(b && type.StartsWith("ModuleEngines", StringComparison.Ordinal))
			{
				foreach (PartModule m in modules)
				{
					var e = m as ModuleEngines;
					e.Shutdown();
					e.EngineIgnited = false;
					e.flameout = true;

					var efx = m as ModuleEnginesFX;
					if (efx != null)
					{
						efx.DeactivateRunningFX();
						efx.DeactivatePowerFX();
						efx.DeactivateLoopingFX();
					}
				}
			}

			switch (type)
			{
				case "ProcessController":
					if (b)
					{
						foreach (PartModule m in modules)
						{
							(m as ProcessController).ReliablityEvent(b);
						}
					}
					break;

				case "ModuleDeployableRadiator":
					if (b)
					{
						part.FindModelComponents<Animation>().ForEach(k => k.Stop());
					}
					break;

				case "ModuleLight":
					if (b)
					{
						foreach (PartModule m in modules)
						{
							ModuleLight l = m as ModuleLight;
							if (l.animationName.Length > 0)
							{
								new Animator(part, l.animationName).Still(0.0f);
							}
							else
							{
								part.FindModelComponents<Light>().ForEach(k => k.enabled = false);
							}
						}
					}
					break;

				case "ModuleRCSFX":
					if (b)
					{
						foreach (PartModule m in modules)
						{
							var e = m as ModuleRCSFX;
							if(e != null)
							{
								e.DeactivateFX();
								e.DeactivatePowerFX();
							}
						}
					}
					break;

				case "ModuleScienceExperiment":
					if (b)
					{
						foreach (PartModule m in modules)
						{
							(m as ModuleScienceExperiment).SetInoperable();
						}
					}
					break;

				case "Experiment":
					if (b)
					{
						foreach (PartModule m in modules)
						{
							(m as Experiment).ReliablityEvent(b);
						}
					}
					break;

				case "SolarPanelFixer":
					foreach (PartModule m in modules)
					{
						(m as SolarPanelFixer).ReliabilityEvent(b);
					}
					break;
			}

			API.Failure.Notify(part, type, b);
		}


		static void Incentive_redundancy(Vessel v, string redundancy)
		{
			if (v.loaded)
			{
				foreach (Reliability m in Lib.FindModules<Reliability>(v))
				{
					if (m.redundancy == redundancy)
					{
						m.next += m.next - m.last;
					}
				}
			}
			else
			{
				var PD = new Dictionary<string, Lib.Module_prefab_data>();

				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
				{
					Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;
					var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();
					PD.Clear();

					foreach (ProtoPartModuleSnapshot m in p.modules)
					{
						if (m.moduleName != "Reliability") continue;

						PartModule module_prefab = Lib.ModulePrefab(module_prefabs, m.moduleName, PD);
						if (!module_prefab) continue;

						string r = Lib.Proto.GetString(m, "redundancy", string.Empty);
						if (r == redundancy)
						{
							double last = Lib.Proto.GetDouble(m, "last");
							double next = Lib.Proto.GetDouble(m, "next");
							Lib.Proto.Set(m, "next", next + (next - last));
						}
					}
				}
			}
		}


		// set highlighting
		static void Highlight(Part p)
		{
			if (p.vessel.KerbalismData().configHighlights)
			{
				// get state among all reliability components in the part
				bool broken = false;
				bool critical = false;
				foreach (Reliability m in p.FindModulesImplementing<Reliability>())
				{
					broken |= m.broken;
					critical |= m.critical;
				}

				if (broken)
				{
					Highlighter.Set(p.flightID, !critical ? Color.yellow : Color.red);
				}
			}
		}


		static void Broken_msg(Vessel v, string title, bool critical)
		{
			if (v.KerbalismData().configMalfunctions)
			{
				if (!critical)
				{
					Message.Post
					(
					  Severity.warning,
					  Local.Reliability_MessagePost24.Format("<b>"+title+"</b>","<b>"+v.vesselName+"</b>"),//String.BuildString(<<1>> malfunctioned on <<2>>)
					  Local.Reliability_MessagePost25//"We can still repair it"
					);
				}
				else
				{
					Message.Post
					(
					  Severity.danger,
					  Local.Reliability_MessagePost26.Format("<b>" + title + "</b>", "<b>" + v.vesselName + "</b>"),//String.BuildString(<<1>> failed on <<2>>)
					  Local.Reliability_MessagePost27//"It is gone for good"
					);
				}
			}
		}


		static void Safemode_msg(Vessel v, string title)
		{
			Message.Post
			(
			  Local.Reliability_MessagePost28.Format("<b>" + title + "</b>", "<b>" + v.vesselName + "</b>"),//String.BuildString("There has been a problem with <<1>> on <<2>>)
			  Local.Reliability_MessagePost29//"We were able to fix it remotely, this time"
			);
		}


		// cause a part at random to malfunction
		internal static void CauseMalfunction(Vessel v)
		{
			// if vessel is loaded
			if (v.loaded)
			{
				// choose a module at random
				var modules = Lib.FindModules<Reliability>(v).FindAll(k => !k.broken);
				if (modules.Count == 0) return;
				var m = modules[Random.RandomInt(modules.Count)];

				// break it
				m.Break();
			}
			// if vessel is not loaded
			else
			{
				// choose a module at random
				var modules = Lib.FindModules(v.protoVessel, "Reliability").FindAll(k => !Lib.Proto.GetBool(k, "broken"));
				if (modules.Count == 0) return;
				var m = modules[Random.RandomInt(modules.Count)];

				// find its part
				ProtoPartSnapshot p = v.protoVessel.protoPartSnapshots.Find(k => k.modules.Contains(m));

				// break it
				ProtoBreak(v, p, m);
			}
		}


		// return true if it make sense to trigger a malfunction on the vessel
		internal static bool CanMalfunction(Vessel v)
		{
			if (v.loaded)
			{
				return Lib.HasModule<Reliability>(v, k => !k.broken);
			}
			else
			{
				return Lib.HasModule(v.protoVessel, "Reliability", k => !Lib.Proto.GetBool(k, "broken"));
			}
		}


		// return true if at least a component has malfunctioned or had a critical failure
		internal static bool HasMalfunction(Vessel v)
		{
			if (v.loaded)
			{
				foreach (Reliability m in Lib.FindModules<Reliability>(v))
				{
					if (m.broken) return true;
				}
			}
			else
			{
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Reliability"))
				{
					if (Lib.Proto.GetBool(m, "broken")) return true;
				}
			}

			return false;
		}


		// return true if at least a component has a critical failure
		internal static bool HasCriticalFailure(Vessel v)
		{
			if (v.loaded)
			{
				foreach (Reliability m in Lib.FindModules<Reliability>(v))
				{
					if (m.critical) return true;
				}
			}
			else
			{
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Reliability"))
				{
					if (Lib.Proto.GetBool(m, "critical")) return true;
				}
			}
			return false;
		}
	}


} // KERBALISM

