using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class Emitter : PartModule, ISpecifics, IKerbalismModule
	{
		// config
		[KSPField] public string active;                          // name of animation to play when enabling/disabling

		[KSPField(isPersistant = true)] public string title = string.Empty;     // GUI name of the status action in the PAW
		[KSPField(isPersistant = true)] public bool toggle;						// true if the effect can be toggled on/off
		[KSPField(isPersistant = true)] public double radiation;				// radiation in rad/s
		[KSPField(isPersistant = true)] public double ec_rate;					// EC consumption rate per-second (optional)
		[KSPField(isPersistant = true)] public bool running;
		[KSPField(isPersistant = true)] public double radiation_impact = 1.0;	// calculated based on vessel design

		[KSPField(guiActive = true, guiActiveEditor = true, guiName = "_", groupName = "Radiation", groupDisplayName = "#KERBALISM_Group_Radiation")]//Radiation
		public string Status;  // rate of radiation emitted/shielded

		// animations
		Animator active_anim;
		bool radiation_impact_calculated = false;

		// pseudo-ctor
		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;

			// update RMB ui
			if (string.IsNullOrEmpty(title))
				title = radiation >= 0.0 ? "Radiation" : "Active shield";

			Fields["Status"].guiName = title;
			Events["Toggle"].active = toggle;
			Actions["Action"].active = toggle;

			// deal with non-toggable emitters
			if (!toggle) running = true;

			// create animator
			active_anim = new Animator(part, active);

			// set animation initial state
			active_anim.Still(running ? 0.0 : 1.0);
		}

		class HabitatInfo
		{
			internal Habitat habitat;
			internal float distance;

			internal HabitatInfo(Habitat habitat, float distance)
			{
				this.habitat = habitat;
				this.distance = distance;
			}
		}
		List<HabitatInfo> habitatInfos = null;

		internal void Recalculate()
		{
			habitatInfos = null;
			CalculateRadiationImpact();
		}

		void BuildHabitatInfos()
		{
			if (habitatInfos != null) return;
			if (part.transform == null) return;
			var emitterPosition = part.transform.position;

			List<Habitat> habitats;

			if (GameLogic.IsEditor())
			{
				habitats = new List<Habitat>();

				List<Part> parts = Lib.GetPartsRecursively(EditorLogic.RootPart);
				foreach (var p in parts)
				{
					var habitat = p.FindModuleImplementing<Habitat>();
					if (habitat != null) habitats.Add(habitat);
				}
			}
			else
			{
				habitats = vessel.FindPartModulesImplementing<Habitat>();
			}

			habitatInfos = new List<HabitatInfo>();

			foreach (var habitat in habitats)
			{
				var habitatPosition = habitat.part.transform.position;
				var vector = habitatPosition - emitterPosition;

				HabitatInfo spi = new HabitatInfo(habitat, vector.magnitude);
				habitatInfos.Add(spi);
			}
		}

		/// <summary>Calculate the average radiation effect to all habitats. returns true if successful.</summary>
		bool CalculateRadiationImpact()
		{
			if (radiation < 0)
			{
				radiation_impact = 1.0;
				return true;
			}

			if (habitatInfos == null) BuildHabitatInfos();
			if (habitatInfos == null) return false;

			radiation_impact = 0.0;
			int habitatCount = 0;

			foreach (var hi in habitatInfos)
			{
				radiation_impact += Radiation.DistanceRadiation(1.0, hi.distance);
				habitatCount++;
			}

			if (habitatCount > 1)
				radiation_impact /= habitatCount;

			return true;
		}

		void Update()
		{
			// update ui
			if (!part.IsPAWVisible())
				return;

			Status = running ? HumanReadable.Radiation(System.Math.Abs(radiation)) : Local.Emitter_none;//"none"
			Events["Toggle"].guiName = UI.StatusToggle(part.partInfo.title, running ? Local.Generic_ACTIVE : Local.Generic_DISABLED);
		}

		void FixedUpdate()
		{
			if (!radiation_impact_calculated)
				radiation_impact_calculated = CalculateRadiationImpact();
		}

		// See IKerbalismModule
		static string BackgroundUpdate(Vessel v,
			ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot,
			PartModule proto_part_module, Part proto_part,
			Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			Emitter emitter = proto_part_module as Emitter;
			if (emitter == null) return string.Empty;

			if (Lib.Proto.GetBool(module_snapshot, "running") && emitter.ec_rate > 0)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -emitter.ec_rate));
			}

			return emitter.title;
		}

		public virtual string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			// if enabled, and there is ec consumption
			if (running && ec_rate > 0)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ec_rate));
			}

			return title;
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			return ResourceUpdate(null, resourceChangeRequest);
		}


		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "_", active = true, groupName = "Radiation", groupDisplayName = "#KERBALISM_Group_Radiation")]//Radiation
		public void Toggle()
		{
			// switch status
			running = !running;

			// play animation
			active_anim.Play(running, false);

			// refresh VAB/SPH ui
			if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}


		// action groups
		[KSPAction("#KERBALISM_Emitter_Action")] public void Action(KSPActionParam param) { Toggle(); }


		// part tooltip
		public override string GetInfo()
		{
			string desc = radiation > double.Epsilon
			  ? Local.Emitter_EmitIonizing
			  : Local.Emitter_ReduceIncoming;

			return Specs().Info(desc);
		}

		// specifics support
		public Specifics Specs()
		{
			Specifics specs = new Specifics();
			specs.Add(radiation >= 0.0 ? Local.Emitter_Emitted : Local.Emitter_ActiveShielding, HumanReadable.Radiation(System.Math.Abs(radiation)));
			if (ec_rate > double.Epsilon)
			{
				if (Settings.UseSIUnits)
					specs.Add(Local.Deploy_actualCost, SI.SIRate(ec_rate, ResourceUnitInfo.ECResID));
				else
					specs.Add("EC/s", HumanReadable.Rate(ec_rate));
			}
			return specs;
		}

		/// <summary>
		/// get the total radiation emitted by nearby emitters (used for EVAs). only works for loaded vessels.
		/// </summary>
		internal static double Nearby(Vessel v)
		{
			if (!v.loaded || !v.isEVA) return 0.0;
			var evaPosition = v.rootPart.transform.position;

			double result = 0.0;

			foreach (Vessel n in FlightGlobals.VesselsLoaded)
			{
				var vd = n.KerbalismData();
				if (!vd.IsSimulated) continue;

				foreach (var emitter in Lib.FindModules<Emitter>(n))
				{
					if (emitter.part == null || emitter.part.transform == null) continue;
					if (emitter.radiation <= 0) continue; // ignore shielding effects here
					if (!emitter.running) continue;

					var emitterPosition = emitter.part.transform.position;
					var vector = evaPosition - emitterPosition;
					var distance = vector.magnitude;

					result += Radiation.DistanceRadiation(emitter.radiation, distance);
				}
			}

			return result;
		}

		// return total radiation emitted in a vessel
		internal static double Total(Vessel v)
		{
			// get resource cache
			ResourceInfo ec = ResourceCache.GetResource(v, "ElectricCharge");

			double tot = 0.0;
			if (v.loaded)
			{
				foreach (var emitter in Lib.FindModules<Emitter>(v))
				{
					if (ec.Amount > double.Epsilon || emitter.ec_rate <= double.Epsilon)
					{
						if (emitter.running)
						{
							if (emitter.radiation > 0) tot += emitter.radiation * emitter.radiation_impact;
							else tot += emitter.radiation; // always account for full shielding effect
						}
					}
				}
			}
			else
			{
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Emitter"))
				{
					if (ec.Amount > double.Epsilon || Lib.Proto.GetDouble(m, "ec_rate") <= double.Epsilon)
					{
						if (Lib.Proto.GetBool(m, "running"))
						{
							var rad = Lib.Proto.GetDouble(m, "radiation");
							if (rad < 0) tot += rad;
							else
							{
								tot += rad * Lib.Proto.GetDouble(m, "radiation_factor");
							}
						}
					}
				}
			}
			return tot;
		}
	}

} // KERBALISM

