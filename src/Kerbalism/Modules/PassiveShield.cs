using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class PassiveShield: PartModule, IPartMassModifier, IKerbalismModule
	{
		// config
		[KSPField] public string title = Local.PassiveShield_Sandbags;//"Sandbags"              // GUI name of the status action in the PAW
		[KSPField] public string engageActionTitle = Local.PassiveShield_fill;//"fill"      // what the deploy action should be called
		[KSPField] public string disengageActionTitle = Local.PassiveShield_empty;//"empty"  // what the empty action should be called
		[KSPField] public string disabledTitle = Local.PassiveShield_stowed;// "stowed"       // what to display in the status text while not deployed

		[KSPField] public bool toggle = true;                     // true if the effect can be toggled on/off
		[KSPField] public string animation;                       // name of animation to play when enabling/disabling
		[KSPField] public float added_mass = 1.5f;                // mass added when deployed, in tons
		[KSPField] public bool require_eva = true;                // true if only accessible by EVA
		[KSPField] public bool require_landed = true;             // true if only deployable when landed
		[KSPField] public string crew_operate = "true";           // operator crew requirement. true means anyone

		// persisted for simplicity, so that the values are available in Total() below
		[KSPField(isPersistant = true)] public double radiation;                       // radiation effect in rad/s
		[KSPField(isPersistant = true)] public double ec_rate = 0;                     // EC consumption rate per-second (optional)

		// persistent
		[KSPField(isPersistant = true)] public bool deployed = false; // currently deployed

		[KSPField(guiActive = true, guiActiveEditor = true, guiName = "_", groupName = "Radiation", groupDisplayName = "#KERBALISM_Group_Radiation")]//Radiation
		// rmb status
		public string Status;  // rate of radiation emitted/shielded

		Animator deploy_anim;
		CrewSpecs deploy_cs;

		// pseudo-ctor
		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;

			// update RMB ui
			Fields["Status"].guiName = title;
			Actions["Action"].active = toggle;

			if(toggle)
			{
				Events["Toggle"].guiActiveUnfocused = require_eva;
				Events["Toggle"].guiActive = !require_eva || GameLogic.IsEditor();
			}

			// deal with non-toggable
			if (!toggle)
			{
				deployed = true;
			}

			// create animator
			deploy_anim = new Animator(part, animation);

			// set animation initial state
			deploy_anim.Still(deployed ? 1.0 : 0.0);

			deploy_cs = new CrewSpecs(crew_operate);
		}

		void Update()
		{
			if (!part.IsPAWVisible())
				return;

			// update ui
			Status = deployed ? String.BuildString(Local.PassiveShield_absorbing ," ", HumanReadable.Radiation(System.Math.Abs(radiation))) : disabledTitle;//"absorbing
			Events["Toggle"].guiName = UI.StatusToggle(title, deployed ? disengageActionTitle : engageActionTitle);
		}

		void FixedUpdate()
		{
			// do nothing else in the editor
			if (GameLogic.IsEditor()) return;

			// allow sandbag filling only when landed
			bool allowDeploy = vessel.Landed || !require_landed;
			Events["Toggle"].active = toggle && (allowDeploy || deployed);
		}

		/// <summary>
		/// We're always going to call you for resource handling.  You tell us what to produce or consume.  Here's how it'll look when your vessel is NOT loaded
		/// </summary>
		/// <param name="vessel">the vessel (unloaded)</param>
		/// <param name="proto_part">proto part snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="proto_module">proto part module snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="partModule">proto part module snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="part">proto part snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="availableResources">key-value pair containing all available resources and their currently available amount on the vessel. if the resource is not in there, it's not available</param>
		/// <param name="resourceChangeRequest">key-value pair that contains the resource names and the units per second that you want to produce/consume (produce: positive, consume: negative)</param>
		/// <param name="elapsed_s">how much time elapsed since the last time. note this can be very long, minutes and hours depending on warp speed</param>
		/// <returns>the title to be displayed in the resource tooltip</returns>
		internal static string BackgroundUpdate(Vessel vessel, ProtoPartSnapshot proto_part,
			ProtoPartModuleSnapshot proto_module, PartModule partModule, Part part,
			Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			PassiveShield passiveShield = partModule as PassiveShield;
			if (passiveShield == null) return string.Empty;
			if (passiveShield.ec_rate > 0) return string.Empty;

			bool deployed = Lib.Proto.GetBool(proto_module, "deployed");
			if(deployed)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -passiveShield.ec_rate));
			}

			return passiveShield.title;
		}

		public virtual string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			// if there is ec consumption
			if (deployed && ec_rate > 0)
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
			if (GameLogic.IsFlight())
			{

				// disable for dead eva kerbals
				Vessel v = FlightGlobals.ActiveVessel;
				if (v == null || EVA.IsDeadEVA(v)) return;
				if (!deploy_cs.Check(v))
				{
					Message.Post
					(
					  String.TextVariant
					  (
						Local.PassiveShield_MessagePost//"I don't know how this works!"
					  ),
					  deploy_cs.Warning()
					);
					return;
				}
			}

			// switch status
			deployed = !deployed;

			// play animation
			deploy_anim.Play(!deployed, false);

			// refresh VAB/SPH ui
			if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

		// action groups
		[KSPAction("Toggle")] public void Action(KSPActionParam param) { Toggle(); }


		// return total radiation emitted in a vessel
		internal static double Total(Vessel v)
		{
			// get resource cache
			ResourceInfo ec = ResourceCache.GetResource(v, "ElectricCharge");

			double total = 0.0;
			if (v.loaded)
			{
				foreach (var shield in Lib.FindModules<PassiveShield>(v))
				{
					if (ec.Amount > 0 || shield.ec_rate <= 0)
					{
						if (shield.deployed)
							total += shield.radiation;
					}
				}
			}
			else
			{
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "PassiveShield"))
				{
					if (ec.Amount > 0 || Lib.Proto.GetDouble(m, "ec_rate") <= 0)
					{
						if (Lib.Proto.GetBool(m, "deployed"))
						{
							var rad = Lib.Proto.GetDouble(m, "radiation");
							total += rad;
						}
					}
				}
			}
			return total;
		}

		// mass change support
		public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) { return deployed ? added_mass : 0.0f; }
		public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }
	}

} // KERBALISM

