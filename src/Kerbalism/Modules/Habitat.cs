using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KERBALISM
{
    class Habitat : PartModule, ISpecifics, IModuleInfo, IPartCostModifier
	{
        // config
        [KSPField] public double volume = 0.0;                      // habitable volume in m^3, deduced from bounding box if not specified
        [KSPField] public double surface = 0.0;                     // external surface in m^2, deduced from bounding box if not specified
        [KSPField] public string inflate = string.Empty;            // inflate animation, if any
        [KSPField] public bool inflatableUsingRigidWalls = false;   // can shielding be applied to inflatable structure?
        [KSPField] public bool toggle = true;                       // show the enable/disable toggle
		[KSPField] public double max_pressure = 1.0;                // max. sustainable pressure, in percent of sea level
																	// for now this won't do anything

		// method to use for calculating volume and surface
		[KSPField] public VolumeAndSurfaceMethod volumeAndSurfaceMethod = VolumeAndSurfaceMethod.Best;
		[KSPField] public bool substractAttachementNodesSurface = true;

		// persistence
		[KSPField(isPersistant = true)] public State state = State.enabled;
        [KSPField(isPersistant = true)] public double perctDeployed = 0;

        // rmb ui status strings
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#KERBALISM_Habitat_Volume", groupName = "Habitat", groupDisplayName = "#KERBALISM_Group_Habitat")]//Habitat
        public string Volume;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#KERBALISM_Habitat_Surface", groupName = "Habitat", groupDisplayName = "#KERBALISM_Group_Habitat")]//Habitat
        public string Surface;

        // animations
        Animator inflate_anim;

        [KSPField] public bool animBackwards;  // invert animation (case state is deployed but it is showing the part retracted)
		internal bool needEqualize = false;      // Used to trigger the ResourceBalance

        bool hasCLS;                   // Has CLS mod?
        bool FixIVA = false;           // Used only CrewTransferred event, CrewTrans occur after FixedUpdate, then FixedUpdate needs to know to fix it
        bool hasGravityRing;
        GravityRing gravityRing;

        State prev_state;                      // State during previous GPU frame update
        bool configured = false;       // true if configure method has been executed
		float shieldingCost;

		// volume / surface cache
		internal static Dictionary<string, PartVolumeAndSurfaceInfo> habitatDatabase;
		internal const string habitatDataCacheNodeName = "KERBALISM_HABITAT_INFO";
		internal static string HabitatDataCachePath => Path.Combine(Lib.KerbalismRootPath, "HabitatData.cache");

		// volume / surface evaluation at prefab compilation
		public override void OnLoad(ConfigNode node)
		{
			// volume/surface calcs are quite slow and memory intensive, so we do them only once on the prefab
			// then get the prefab values from OnStart. Moreover, we cache the results in the 
			// Kerbalism\HabitatData.cache file and reuse those cached results on next game launch.
			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				if (volume <= 0.0 || surface <= 0.0)
				{
					if (habitatDatabase == null)
					{
						ConfigNode dbRootNode = ConfigNode.Load(HabitatDataCachePath);
						ConfigNode[] habInfoNodes = dbRootNode?.GetNodes(habitatDataCacheNodeName);
						habitatDatabase = new Dictionary<string, PartVolumeAndSurfaceInfo>();

						if (habInfoNodes != null)
						{
							for (int i = 0; i < habInfoNodes.Length; i++)
							{
								string partName = habInfoNodes[i].GetValue("partName") ?? string.Empty;
								if (!string.IsNullOrEmpty(partName) && !habitatDatabase.ContainsKey(partName))
									habitatDatabase.Add(partName, new PartVolumeAndSurfaceInfo(habInfoNodes[i]));
							}
						}
					}

					// SSTU specific support copypasted from the old system, not sure how well this works
					foreach (PartModule pm in part.Modules)
					{
						if (pm.moduleName == "SSTUModularPart")
						{
							Bounds bb = Reflection.ReflectionCall<Bounds>(pm, "getModuleBounds", new Type[] { typeof(string) }, new string[] { "CORE" });
							if (bb != null)
							{
								if (volume <= 0.0) volume = BoundsVolume(bb) * 0.785398; // assume it's a cylinder
								if (surface <= 0.0) surface = BoundsSurface(bb) * 0.95493; // assume it's a cylinder
							}
							return;
						}
					}

					string configPartName = part.name.Replace('.', '_');
					PartVolumeAndSurfaceInfo partInfo;
					if (!habitatDatabase.TryGetValue(configPartName, out partInfo))
					{
						// Find deploy/retract animations, either here on in the gravityring module
						// then set the part to the deployed state before doing the volume/surface calcs
						// if part has Gravity Ring, find it.
						gravityRing = part.FindModuleImplementing<GravityRing>();
						hasGravityRing = gravityRing != null;

						// create animators and set the model to the deployed state
						if (hasGravityRing)
						{
							gravityRing.deploy_anim = new Animator(part, gravityRing.deploy);
							gravityRing.deploy_anim.reversed = gravityRing.animBackwards;

							if (gravityRing.deploy_anim.IsDefined)
								gravityRing.deploy_anim.Still(1.0);
						}
						else
						{
							inflate_anim = new Animator(part, inflate);
							inflate_anim.reversed = animBackwards;

							if (inflate_anim.IsDefined)
								inflate_anim.Still(1.0);
						}

						// get surface and volume
						partInfo = GetPartVolumeAndSurface(part, Settings.VolumeAndSurfaceLogging);

						habitatDatabase.Add(configPartName, partInfo);
					}

					partInfo.GetUsingMethod(
						volumeAndSurfaceMethod != VolumeAndSurfaceMethod.Best ? volumeAndSurfaceMethod : partInfo.bestMethod,
						out double infoVolume, out double infoSurface, substractAttachementNodesSurface);

					if (volume <= 0.0) volume = infoVolume;
					if (surface <= 0.0) surface = infoSurface;
				}
			}
		}

		// pseudo-ctor
		public override void OnStart(StartState state)
        {
            // don't break tutorial scenarios
            if (GameLogic.DisableScenario(this)) return;

            // check if has Connected Living Space mod
            hasCLS = Lib.HasAssembly("ConnectedLivingSpace");

            // if part has Gravity Ring, find it.
            gravityRing = part.FindModuleImplementing<GravityRing>();
            hasGravityRing = gravityRing != null;

			if (volume <= 0.0 || surface <= 0.0)
			{
				Habitat prefab = part.partInfo.partPrefab.FindModuleImplementing<Habitat>();
				if (volume <= 0.0) volume = prefab.volume;
				if (surface <= 0.0) surface = prefab.surface;
			}

			// set RMB UI status strings
			Volume = HumanReadable.Volume(volume);
            Surface = HumanReadable.Surface(surface);

            // hide toggle if specified
            Events["Toggle"].active = toggle;
            //Actions["Action"].active = toggle;

#if DEBUG
			Events["LogVolumeAndSurface"].active = true;
#else
			Events["LogVolumeAndSurface"].active = Settings.VolumeAndSurfaceLogging;
#endif
			// create animators
			if (!hasGravityRing)
            {
                inflate_anim = new Animator(part, inflate);
            }

			// add the cost of shielding to the base part cost
			shieldingCost = (float)surface * PartResourceLibrary.Instance.GetDefinition("Shielding").unitCost;

			// configure on start
			Configure();

            perctDeployed = Lib.Level(part, "Atmosphere", true);

            switch (this.state)
            {
                case State.enabled: Set_flow(true); break;
                case State.disabled: Set_flow(false); break;
                case State.pressurizing: Set_flow(true); break;
                case State.depressurizing: Set_flow(false); break;
            }

            if (Get_inflate_string().Length == 0) // not inflatable
            {
                SetPassable(true);
                UpdateIVA(true);
            }
            else
            {
                SetPassable(System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 <= ResourceBalance.precision);
                UpdateIVA(System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 <= ResourceBalance.precision);
            }

            if (GameLogic.IsFlight())
            {
                // For fix IVA when crewTransfered occur, add event to define flag for FixedUpdate
                GameEvents.onCrewTransferred.Add(UpdateCrew);
            }
        }

        void OnDestroy()
        {
            GameEvents.onCrewTransferred.Remove(UpdateCrew);
        }

        string Get_inflate_string()
        {
            if (hasGravityRing)
            {
                return gravityRing.deploy;
            }
            return inflate;
        }

		bool Get_inflate_anim_backwards()
        {
            if (hasGravityRing)
            {
                return gravityRing.animBackwards;
            }
            return animBackwards;
        }

        Animator Get_inflate_anim()
        {
            if (hasGravityRing)
            {
                return gravityRing.deploy_anim;
            }
            return inflate_anim;
        }

        void Set_pressurized(bool pressurized)
        {
            if (hasGravityRing)
            {
                gravityRing.isHabitat = true;
                gravityRing.deployed = pressurized;
            }
        }

        void Configure()
        {
            // if never set, this is the case if:
            // - part is added in the editor
            // - module is configured first time either in editor or in flight
            // - module is added to an existing savegame
            if (!part.Resources.Contains("Atmosphere"))
            {
                // add internal atmosphere resources
                // - disabled habitats start with zero atmosphere
                Lib.AddResource(part, "Atmosphere", (state == State.enabled && Features.Pressure) ? volume * 1e3 : 0.0, volume * 1e3);
                Lib.AddResource(part, "WasteAtmosphere", 0.0, volume * 1e3);

                // add external surface shielding
                PartResource shieldingRes = Lib.AddResource(part, "Shielding", 0.0, surface);

				// inflatable habitats can't be shielded (but still need the capacity) unless they have rigid walls
				shieldingRes.isTweakable = (Get_inflate_string().Length == 0) || inflatableUsingRigidWalls;

				// if shielding feature is disabled, just hide it
				shieldingRes.isVisible = Features.Shielding && shieldingRes.isTweakable;

                configured = true;
            }
        }

        void Set_flow(bool b)
        {
            Lib.SetResourceFlow(part, "Atmosphere", b);
            Lib.SetResourceFlow(part, "WasteAtmosphere", b);
            Lib.SetResourceFlow(part, "Shielding", b);
        }

        State Depressurizing()
        {
            // in flight
            if (GameLogic.IsFlight())
            {
                // All module are empty
                bool cond1 = true;

                // check amounts
                foreach (string resource in ResourceBalance.resourceName)
                {
                    if (part.Resources.Contains(resource))
                        cond1 &= part.Resources[resource].amount <= double.Epsilon;
                }

                // are all modules empty?
                if (cond1) return State.disabled;

                // Depressurize still in progress
                return State.depressurizing;
            }
            // in the editors
            else
            {
                // set amount to zero
                foreach (string resource in ResourceBalance.resourceName)
                {
                    if (part.Resources.Contains(resource))
                        part.Resources[resource].amount = 0.0;
                }

				// return new state
				return State.disabled;
            }
        }

        State Pressurizing()
        {
            // in flight
            if (GameLogic.IsFlight())
            {
                // full pressure the level is 99.9999% deployed or more
                if (System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 <= ResourceBalance.precision)
                {
                    SetPassable(true);
                    UpdateIVA(true);
                    return State.enabled;
                }
                return State.pressurizing;
            }
            // in the editors
            else
            {
                // The other resources in ResourceBalance are waste resources
                if (part.Resources.Contains("Atmosphere"))
                    part.Resources["Atmosphere"].amount = part.Resources["Atmosphere"].maxAmount;

				// return new state
				return State.enabled;
            }
        }

        void Update()
        {
            // The first time an existing save game is loaded with Kerbalism installed,
            // MM will to any existing vessels add Nitrogen with the correct capacities as set in default.cfg but they will have zero amounts,
            // this is not the case for any newly created vessels in the editor.
            if (configured)
            {
                if (state == State.enabled && Features.Pressure)
                    Lib.FillResource(part, "Nitrogen");
                else
                {
                    Lib.EmptyResource(part, "Nitrogen");
                }
                configured = false;
            }

            switch (state)
            {
                case State.enabled:
	                Set_pressurized(true);
	                // GOT 12-2020 : Disabling ability to disable habs due to pressurization bugs that I'm not willing to investigate
	                Events["Toggle"].guiActive = false;
					break;
                case State.disabled:
	                Set_pressurized(false);
                    break;
                case State.pressurizing:
	                Set_pressurized(false);
                    break;
                case State.depressurizing:
	                Set_pressurized(false);
                    break;
            }

			if (part.IsPAWVisible())
			{
				string status_str = string.Empty;
				switch (state)
				{
					case State.enabled:
						// No inflatable can be enabled been pressurizing
						if (System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 > ResourceBalance.precision)
							status_str = Local.Habitat_pressurizing;
						else
							status_str = Local.Generic_ENABLED;
						break;
					case State.disabled:
						status_str = Local.Generic_DISABLED;
						break;
					case State.pressurizing:
						status_str = Get_inflate_string().Length == 0 ? Local.Habitat_pressurizing : Local.Habitat_inflating;
						status_str += string.Format("{0:p2}", perctDeployed);
						break;
					case State.depressurizing:
						status_str = Get_inflate_string().Length == 0 ? Local.Habitat_depressurizing : Local.Habitat_deflating;
						status_str += string.Format("{0:p2}", perctDeployed);
						break;
				}

				Events["Toggle"].guiName = Lib.StatusToggle(Local.StatuToggle_Habitat, status_str);//"Habitat"
			}

            // Changing this animation when we expect rotation will not work because
            // Unity disables other animations when playing the inflation animation.
            if (prev_state != State.enabled)
            {
                Set_inflation();
            }
            prev_state = state;
        }

        void FixedUpdate()
        {
            // if part is manned (even in the editor), force enabled
            if (Lib.IsCrewed(part) && state != State.enabled)
            {
                Set_flow(true);
                state = State.pressurizing;

                // Equalize run only in Flight mode
                needEqualize = GameLogic.IsFlight();
            }

            perctDeployed = Lib.Level(part, "Atmosphere", true);

            // Only handle crewTransferred & Toggle, this way has less calls in FixedUpdate
            // CrewTransferred Event occur after FixedUpdate, this must be check in crewtransferred
            if (FixIVA)
            {
                if (Get_inflate_string().Length == 0) // it is not inflatable (We always going to show and cross those habitats)
                {
                    SetPassable(true);
                    UpdateIVA(true);
                }
                else
                {
                    // Inflatable modules shows IVA and are passable only in 99.9999% deployed
                    SetPassable(Lib.IsCrewed(part) || System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 <= ResourceBalance.precision);
                    UpdateIVA(System.Math.Truncate(System.Math.Abs((perctDeployed + ResourceBalance.precision) - 1.0) * 100000) / 100000 <= ResourceBalance.precision);
                }
                FixIVA = false;
            }

            // state machine
            switch (state)
            {
                case State.enabled:
                    // In case it is losting pressure
                    if (perctDeployed < Settings.PressureThreshold)
                    {
                        if (Get_inflate_string().Length != 0)         // it is inflatable
                        {
                            SetPassable(false || Lib.IsCrewed(part)); // Prevent to not lock a Kerbal into a the part
                            UpdateIVA(false);
                        }
                        needEqualize = true;
                        state = State.pressurizing;
                    }
                    break;

                case State.disabled:
                    break;

                case State.pressurizing:
                    state = Pressurizing();
                    break;

                case State.depressurizing:
                    // Just do Venting when has no gravityRing or when the gravity ring is not spinning.
                    if (hasGravityRing && !gravityRing.Is_rotating()) state = Depressurizing();
                    else if (!hasGravityRing) state = Depressurizing();
                    break;
            }
        }

        void Set_inflation()
        {
            // if there is an inflate animation, set still animation from pressure
            if (Get_inflate_anim_backwards()) Get_inflate_anim().Still(System.Math.Abs(Lib.Level(part, "Atmosphere", true) - 1));
            else Get_inflate_anim().Still(Lib.Level(part, "Atmosphere", true));
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "_", active = true, groupName = "Habitat", groupDisplayName = "#KERBALISM_Group_Habitat")]//Habitat
        public void Toggle()
        {
            // if manned, we can't depressurize
            if (Lib.IsCrewed(part) && (state == State.enabled || state == State.pressurizing))
            {
                Message.Post(Local.Habitat_postmsg.Format(Lib.PartName(part)));//"Can't disable <b><<1>> habitat</b> while crew is inside"//Lib.BuildString("Can't disable <b>", , " habitat</b> while crew is inside"
				return;
            }

            // Need be equalized
            needEqualize = true;
            FixIVA = true;

            // Every time that toggle bot be clicked, it will change the flow, better then call it every frame
            // state switching
            switch (state)
            {
                // Make Set_flow be called only once throgh the Toggle

				// GOT 12-2020 : Disabling ability to disable habs due to pressurization bugs that I'm not willing to investigate
                case State.enabled:
					if (GameLogic.IsFlight())
						break;

					Set_flow(false);
					state = State.depressurizing;
					break;
				case State.disabled: Set_flow(true); state = State.pressurizing; break;
                case State.pressurizing: Set_flow(false); state = State.depressurizing; break;
                case State.depressurizing: Set_flow(true); state = State.pressurizing; break;
            }

            // refresh VAB/SPH ui
            if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

		// action groups
		// GOT 12-2020 : Disabling ability to disable habs due to pressurization bugs that I'm not willing to investigate
		//[KSPAction("#KERBALISM_Habitat_Action")] public void Action(KSPActionParam param) { Toggle(); }

		// part tooltip
		public override string GetInfo()
        {
            return Specs().Info();
        }

        // specifics support
        public Specifics Specs()
        {
            Specifics specs = new Specifics();
            specs.Add(Local.Habitat_info1, HumanReadable.Volume(volume > 0.0 ? volume : PartBoundsVolume(part)) + (volume > 0.0 ? "" : " (bounds)"));//"Volume"
            specs.Add(Local.Habitat_info2, HumanReadable.Surface(surface > 0.0 ? surface : PartBoundsSurface(part)) + (surface > 0.0 ? "" : " (bounds)"));//"Surface"
            specs.Add(Local.Habitat_info3, max_pressure >= Settings.PressureThreshold ? Local.Habitat_yes : Local.Habitat_no);//"Pressurized""yes""no"
            if (inflate.Length > 0) specs.Add(Local.Habitat_info4, Local.Habitat_yes);//"Inflatable""yes"
            if (PhysicsGlobals.KerbalCrewMass > 0)
                specs.Add(Local.Habitat_info5, HumanReadable.Mass(PhysicsGlobals.KerbalCrewMass));//"Added mass per crew"

            return specs;
        }

		// return habitat volume in a vessel in m^3
		internal static double Tot_volume(Vessel v)
        {
            // we use capacity: this mean that partially pressurized parts will still count,
            return ResourceCache.GetResource(v, "Atmosphere").Capacity / 1e3;
        }

		// return habitat surface in a vessel in m^2
		internal static double Tot_surface(Vessel v)
        {
            // we use capacity: this mean that partially pressurized parts will still count,
            return ResourceCache.GetResource(v, "Shielding").Capacity;
        }

		// return normalized pressure in a vessel
		internal static double Pressure(Vessel v)
        {
            // the pressure is simply the atmosphere level
            return ResourceCache.GetResource(v, "Atmosphere").Level;
        }

		// return waste level in a vessel atmosphere
		internal static double Poisoning(Vessel v)
        {
            // the proportion of co2 in the atmosphere is simply the level of WasteAtmo
            return ResourceCache.GetResource(v, "WasteAtmosphere").Level;
        }

		/// <summary>
		/// Return vessel shielding factor.
		/// </summary>
		internal static double Shielding(Vessel v)
        {
            return Radiation.ShieldingEfficiency(ResourceCache.GetResource(v, "Shielding").Level);
        }

		// return living space factor in a vessel
		internal static double Living_space(Vessel v)
        {
            // living space is the volume per-capita normalized against an 'ideal living space' and clamped in an acceptable range
            return Math.Clamp(Volume_per_crew(v) / PreferencesComfort.Instance.livingSpace, 0.1, 1.0);
        }

		internal static double Volume_per_crew(Vessel v)
        {
            // living space is the volume per-capita normalized against an 'ideal living space' and clamped in an acceptable range
            return Tot_volume(v) / System.Math.Max(1, Lib.CrewCount(v));
        }

		// return a verbose description of shielding capability
		internal static string Shielding_to_string(double v)
        {
            return v <= double.Epsilon ? Local.Habitat_none : Lib.BuildString((20.0 * v / PreferencesRadiation.Instance.shieldingEfficiency).ToString("F2"), " mm");//"none"
        }

		// traduce living space value to string
		internal static string Living_space_to_string(double v)
        {
            if (v >= 0.99) return Local.Habitat_Summary1;//"ideal"
            else if (v >= 0.75) return Local.Habitat_Summary2;//"good"
            else if (v >= 0.5) return Local.Habitat_Summary3;//"modest"
            else if (v >= 0.25) return Local.Habitat_Summary4;//"poor"
            else return Local.Habitat_Summary5;//"cramped"
        }

        // enable/disable dialog "Transfer crew" on UI
        void RefreshDialog()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartTweaked, part);
                if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselWasModified.Fire(this.vessel);
            }

            part.CheckTransferDialog();
            MonoUtilities.RefreshContextWindows(part);
        }

        // Support Connected Living Space
        void SetPassable(bool isPassable)
        {
            if (hasCLS)
            {
                // for each module
                foreach (PartModule m in part.Modules)
                {
                    if (m.moduleName == "ModuleConnectedLivingSpace")
                    {
                        Logging.LogDebug("Part '{0}', CLS has been {1}", Logging.LogLevel.Message, part.partInfo.title, isPassable ? "enabled" : "disabled");
                        Reflection.ReflectionValue(m, "passable", isPassable);
                    }
                }
            }

            Logging.LogDebug("CrewCapacity: '{0}'", Logging.LogLevel.Message, part.CrewCapacity);
            Logging.LogDebug("CrewTransferAvailable: '{0}'", Logging.LogLevel.Message, isPassable);
            part.crewTransferAvailable = isPassable;
        }

        ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }

        // Enable/Disable IVA
        void UpdateIVA(bool ative)
        {
            if (GameLogic.IsFlight())
            {
                if (vessel.isActiveVessel)
                {
                    if (ative)
                    {
                        Logging.LogDebugStack("Part '{0}', Spawning IVA.", Logging.LogLevel.Message, part.partInfo.title);
                        part.SpawnIVA();
                    }
                    else
                    {
                        Logging.LogDebugStack("Part '{0}', Destroying IVA.", Logging.LogLevel.Message, part.partInfo.title);
                        part.DespawnIVA();
                    }
                    RefreshDialog();
                }
            }
        }

        // Fix IVA when transfer crew
        void UpdateCrew(GameEvents.HostedFromToAction<ProtoCrewMember, Part> dat)
        {
            if (dat.to == part)
            {
                // Need be equalized
                // Enable flow for be pressurized
                Set_flow(true);
                needEqualize = true;
            }

            // Every time that crew be transfered, need update all IVAs for active Vessel
            FixIVA = vessel.isActiveVessel;
        }

		// habitat state
		internal enum State
        {
            disabled,        // hab is disabled
            enabled,         // hab is enabled
            pressurizing,    // hab is pressurizing (between uninhabited and habitats)
            depressurizing,  // hab is depressurizing (between enabled and disabled)
        }

        public override string GetModuleDisplayName() { return Local.Habitat; }//"Habitat"

		public string GetModuleTitle() => Local.Habitat;
		public Callback<Rect> GetDrawModulePanelCallback() => null;
		public string GetPrimaryField()
		{
			return Lib.BuildString(
				Lib.Bold(Local.Habitat + " " + Local.Habitat_info1), // "Habitat" + "Volume"
				" : ",
				HumanReadable.Volume(volume > 0.0 ? volume : PartBoundsVolume(part)),
				volume > 0.0 ? "" : " (bounds)");
		}

		public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) => shieldingCost;
		public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.CONSTANTLY;

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "[Debug] log volume/surface", active = false, groupName = "Habitat", groupDisplayName = "#KERBALISM_Group_Habitat")]//Habitat
		public void LogVolumeAndSurface()
		{
			GetPartVolumeAndSurface(part, true);
		}

		/// <summary>
		/// return the volume of a part bounding box, in m^3
		/// note: this can only be called when part has not been rotated
		/// </summary>
		static double PartBoundsVolume(Part p, bool applyCylinderFactor = false)
		{
			return applyCylinderFactor ? BoundsVolume(GetPartBounds(p)) * 0.785398 : BoundsVolume(GetPartBounds(p));
		}

		/// <summary>
		/// return the surface of a part bounding box, in m^2
		/// note: this can only be called when part has not been rotated
		/// </summary>
		static double PartBoundsSurface(Part p, bool applyCylinderFactor = false)
		{
			return applyCylinderFactor ? BoundsSurface(GetPartBounds(p)) * 0.95493 : BoundsSurface(GetPartBounds(p));
		}

		static double BoundsVolume(Bounds bb)
		{
			Vector3 size = bb.size;
			return size.x * size.y * size.z;
		}

		static double BoundsSurface(Bounds bb)
		{
			Vector3 size = bb.size;
			double a = size.x;
			double b = size.y;
			double c = size.z;
			return 2.0 * (a * b + a * c + b * c);
		}

		static double BoundsIntersectionVolume(Bounds a, Bounds b)
		{
			Vector3 aMin = a.min;
			Vector3 aMax = a.max;
			Vector3 bMin = b.min;
			Vector3 bMax = b.max;

			Vector3 intersectionSize = default;
			intersectionSize.x = System.Math.Max(System.Math.Min(aMax.x, bMax.x) - System.Math.Max(aMin.x, bMin.x), 0f);
			intersectionSize.y = System.Math.Max(System.Math.Min(aMax.y, bMax.y) - System.Math.Max(aMin.y, bMin.y), 0f);
			intersectionSize.z = System.Math.Max(System.Math.Min(aMax.z, bMax.z) - System.Math.Max(aMin.z, bMin.z), 0f);

			return intersectionSize.x * intersectionSize.y * intersectionSize.z;
		}

		/// <summary>
		/// Get the part currently active geometry bounds. Similar to the Part.GetPartRendererBound() method but don't account for inactive renderers.
		/// Note : bounds are world axis aligned, meaning they will change if the part is rotated.
		/// </summary>
		static Bounds GetPartBounds(Part part) => GetTransformRootAndChildrensBounds(part.transform);

		static Bounds GetTransformRootAndChildrensBounds(Transform transform)
		{
			Bounds bounds = default;
			Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);

			bool firstRenderer = true;
			foreach (Renderer renderer in renderers)
			{
				if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
					continue;

				if (firstRenderer)
				{
					bounds = renderer.bounds;
					firstRenderer = false;
					continue;
				}
				bounds.Encapsulate(renderer.bounds);
			}

			return bounds;
		}

		internal class PartVolumeAndSurfaceInfo
		{
			internal VolumeAndSurfaceMethod bestMethod = VolumeAndSurfaceMethod.Best;

			internal double boundsVolume = 0.0;
			internal double boundsSurface = 0.0;

			internal double colliderVolume = 0.0;
			internal double colliderSurface = 0.0;

			internal double meshVolume = 0.0;
			internal double meshSurface = 0.0;

			internal double attachNodesSurface = 0.0;

			internal PartVolumeAndSurfaceInfo() { }

			internal PartVolumeAndSurfaceInfo(ConfigNode node)
			{
				bestMethod = Lib.ConfigEnum(node, "bestMethod", VolumeAndSurfaceMethod.Best);
				boundsVolume = Lib.ConfigValue(node, "boundsVolume", 0.0);
				boundsSurface = Lib.ConfigValue(node, "boundsSurface", 0.0);
				colliderVolume = Lib.ConfigValue(node, "colliderVolume", 0.0);
				colliderSurface = Lib.ConfigValue(node, "colliderSurface", 0.0);
				meshVolume = Lib.ConfigValue(node, "meshVolume", 0.0);
				meshSurface = Lib.ConfigValue(node, "meshSurface", 0.0);
				attachNodesSurface = Lib.ConfigValue(node, "attachNodesSurface", 0.0);
			}

			internal void Save(ConfigNode node)
			{
				node.AddValue("bestMethod", bestMethod.ToString());
				node.AddValue("boundsVolume", boundsVolume.ToString("G17"));
				node.AddValue("boundsSurface", boundsSurface.ToString("G17"));
				node.AddValue("colliderVolume", colliderVolume.ToString("G17"));
				node.AddValue("colliderSurface", colliderSurface.ToString("G17"));
				node.AddValue("meshVolume", meshVolume.ToString("G17"));
				node.AddValue("meshSurface", meshSurface.ToString("G17"));
				node.AddValue("attachNodesSurface", attachNodesSurface.ToString("G17"));
			}

			internal void GetUsingBestMethod(out double volume, out double surface, bool substractAttachNodesSurface = true)
			{
				GetUsingMethod(bestMethod, out volume, out surface, substractAttachNodesSurface);
			}

			internal void GetUsingMethod(VolumeAndSurfaceMethod method, out double volume, out double surface, bool substractAttachNodesSurface = true)
			{
				switch (method)
				{
					case VolumeAndSurfaceMethod.Bounds:
						volume = boundsVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(boundsSurface, attachNodesSurface) : boundsSurface;
						return;
					case VolumeAndSurfaceMethod.Collider:
						volume = colliderVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(colliderSurface, attachNodesSurface) : colliderSurface;
						return;
					case VolumeAndSurfaceMethod.Mesh:
						volume = meshVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(meshSurface, attachNodesSurface) : meshSurface;
						return;
					default:
						volume = 0.0;
						surface = 0.0;
						return;
				}
			}

			double SubstractNodesSurface(double surface, double nodesSurface)
			{
				return System.Math.Max(surface * 0.5, surface - nodesSurface);
			}
		}

		internal enum VolumeAndSurfaceMethod
		{
			Best = 0,
			Bounds,
			Collider,
			Mesh
		}

		struct MeshInfo : IEquatable<MeshInfo>
		{
			string name;
			internal double volume;
			internal double surface;
			internal Bounds bounds;
			internal double boundsVolume;

			internal MeshInfo(string name, double volume, double surface, Bounds bounds)
			{
				this.name = name;
				this.volume = volume;
				this.surface = surface;
				this.bounds = bounds;
				boundsVolume = bounds.size.x * bounds.size.y * bounds.size.z;
			}

			public override string ToString()
			{
				return $"\"{name}\" : VOLUME={volume.ToString("0.00m3")} - SURFACE={surface.ToString("0.00m2")} - BOUNDS VOLUME={boundsVolume.ToString("0.00m3")}";
			}

			public bool Equals(MeshInfo other)
			{
				return volume == other.volume && surface == other.surface && bounds == other.bounds;
			}

			public override bool Equals(object obj) => Equals((MeshInfo)obj);

			public static bool operator ==(MeshInfo lhs, MeshInfo rhs) => lhs.Equals(rhs);

			public static bool operator !=(MeshInfo lhs, MeshInfo rhs) => !lhs.Equals(rhs);

			public override int GetHashCode() => volume.GetHashCode() ^ surface.GetHashCode() ^ bounds.GetHashCode();
		}

		// As a general rule, at least one of the two mesh based methods will return very accurate results.
		// This is very dependent on how the model is done. Specifically, results will be inaccurate in the following cases : 
		// - non closed meshes, larger holes = higher error
		// - overlapping meshes. Obviously any intersection will cause the volume/surface to be higher
		// - surface area will only be accurate in the case of a single mesh per part. A large number of meshes will result in very inaccurate surface evaluation.
		// - results may not be representative of the habitable volume if there are a lot of large structural or "technical" shapes like fuel tanks, shrouds, interstages, integrated engines, etc...

		// Note on surface : surface in kerbalism is meant as the surface of the habitat outer hull exposed to the environment,
		// that's why it make sense to substract the attach nodes area, as that surface will usually by covered by connnected parts.

		/// <summary>
		/// Estimate the part volume and surface by using 3 possible methods : 3D meshes, 3D collider meshes or axis aligned bounding box.
		/// Uses the currently enabled meshes/colliders, and will work with skinned meshes (inflatables).
		/// VERY SLOW, 20-100 ms per call, use it only once and cache the results
		/// </summary>
		/// <param name="part">An axis aligned part, with its geometry in the desired state (mesh switching / animations).</param>
		/// <param name="logAll">If true, the result of all 3 methods will be logged</param>
		/// <param name="ignoreSkinnedMeshes">If true, the volume/surface of deformable meshes (ex : inflatables) will be ignored</param>
		/// <param name="rootTransform">if specified, only bounds/meshes/colliders on this transform and its children will be used</param>
		/// <returns>surface/volume results for the 3 methods, and the best method to use</returns>
		static PartVolumeAndSurfaceInfo GetPartVolumeAndSurface(
			Part part,
			bool logAll = false,
			bool ignoreSkinnedMeshes = false,
			Transform rootTransform = null)
		{
			if (logAll) Logging.Log($"====== Volume and surface evaluation for part :{part.name} ======");

			if (rootTransform == null) rootTransform = part.transform;

			PartVolumeAndSurfaceInfo results = new PartVolumeAndSurfaceInfo();

			if (logAll) Logging.Log("Searching for meshes...");
			List<MeshInfo> meshInfos = GetPartMeshesVolumeAndSurface(rootTransform, ignoreSkinnedMeshes);
			int usedMeshCount = GetMeshesTotalVolumeAndSurface(meshInfos, out results.meshVolume, out results.meshSurface, logAll);


			if (logAll) Logging.Log("Searching for colliders...");
			// Note that we only account for mesh colliders and ignore any box/sphere/capsule collider because :
			// - they usually are used as an array of overlapping box colliders, giving very unreliable results
			// - they are often used for hollow geometry like trusses
			// - they are systematically used for a variety of non shape related things like ladders/handrails/hatches hitboxes (note that it is be possible to filter those by checking for the "Airlock" or "Ladder" tag on the gameobject)
			List<MeshInfo> colliderMeshInfos = GetPartMeshCollidersVolumeAndSurface(rootTransform);
			int usedCollidersCount = GetMeshesTotalVolumeAndSurface(colliderMeshInfos, out results.colliderVolume, out results.colliderSurface, logAll);

			Bounds partBounds = GetTransformRootAndChildrensBounds(rootTransform);
			results.boundsVolume = BoundsVolume(partBounds);
			results.boundsSurface = BoundsSurface(partBounds);

			// If volume is greater than 90% the bounds volume or less than 0.25 m3 it's obviously wrong
			double validityFactor = 0.9;
			bool colliderIsValid = results.colliderVolume < results.boundsVolume * validityFactor && results.colliderVolume > 0.25;
			bool meshIsValid = results.meshVolume < results.boundsVolume * validityFactor && results.meshVolume > 0.25;


			if (!colliderIsValid && !meshIsValid)
				results.bestMethod = VolumeAndSurfaceMethod.Bounds;
			else if (!colliderIsValid)
				results.bestMethod = VolumeAndSurfaceMethod.Mesh;
			else if (!meshIsValid)
				results.bestMethod = VolumeAndSurfaceMethod.Collider;
			else
			{
				// we consider that both methods are accurate if the volume difference is less than 10%
				double volumeDifference = System.Math.Abs(results.colliderVolume - results.meshVolume) / System.Math.Max(results.colliderVolume, results.meshVolume);

				// in case the returned volumes are similar, the method that use the less collider / mesh count will be more accurate for surface
				if (volumeDifference < 0.2 && (usedCollidersCount != usedMeshCount))
					results.bestMethod = usedCollidersCount < usedMeshCount ? VolumeAndSurfaceMethod.Collider : VolumeAndSurfaceMethod.Mesh;
				// in case the returned volumes are still not completely off from one another, favor the result that used only one mesh
				else if (volumeDifference < 0.75 && usedCollidersCount == 1 && usedMeshCount != 1)
					results.bestMethod = VolumeAndSurfaceMethod.Collider;
				else if (volumeDifference < 0.75 && usedMeshCount == 1 && usedCollidersCount != 1)
					results.bestMethod = VolumeAndSurfaceMethod.Mesh;
				// in other cases, the method that return the largest volume is usually right
				else
					results.bestMethod = results.colliderVolume > results.meshVolume ? VolumeAndSurfaceMethod.Collider : VolumeAndSurfaceMethod.Mesh;
			}

			foreach (AttachNode attachNode in part.attachNodes)
			{
				// its seems the standard way of disabling a node involve
				// reducing the rendered radius to 0.001f
				if (attachNode.radius < 0.1f)
					continue;

				switch (attachNode.size)
				{
					case 0: results.attachNodesSurface += 0.3068; break;// 0.625 m disc
					case 1: results.attachNodesSurface += 1.2272; break;// 1.25 m disc
					case 2: results.attachNodesSurface += 4.9090; break;// 2.5 m disc
					case 3: results.attachNodesSurface += 11.045; break;// 3.75 m disc
					case 4: results.attachNodesSurface += 19.635; break;// 5 m disc
				}
			}

			if (logAll)
			{
				double rawColliderVolume = 0.0;
				double rawColliderSurface = 0.0;
				int colliderCount = 0;
				if (colliderMeshInfos != null)
				{
					rawColliderVolume = colliderMeshInfos.Sum(p => p.volume);
					rawColliderSurface = colliderMeshInfos.Sum(p => p.surface);
					colliderCount = colliderMeshInfos.Count();
				}

				double rawMeshVolume = 0.0;
				double rawMeshSurface = 0.0;
				int meshCount = 0;
				if (meshInfos != null)
				{
					rawMeshVolume = meshInfos.Sum(p => p.volume);
					rawMeshSurface = meshInfos.Sum(p => p.surface);
					meshCount = meshInfos.Count();
				}

				results.GetUsingBestMethod(out double volume, out double surface, true);

				Logging.Log($"Evaluation results :");
				Logging.Log($"Bounds method :   Volume:{results.boundsVolume.ToString("0.00m3")} - Surface:{results.boundsSurface.ToString("0.00m2")} - Max valid volume:{(results.boundsVolume * validityFactor).ToString("0.00m3")}");
				Logging.Log($"Collider method : Volume:{results.colliderVolume.ToString("0.00m3")} - Surface:{results.colliderSurface.ToString("0.00m2")} - Raw volume:{rawColliderVolume.ToString("0.00m3")} - Raw surface:{rawColliderSurface.ToString("0.00m2")} - Meshes: {usedCollidersCount}/{colliderCount} (valid/raw)");
				Logging.Log($"Mesh method :     Volume:{results.meshVolume.ToString("0.00m3")} - Surface:{results.meshSurface.ToString("0.00m2")} - Raw volume:{rawMeshVolume.ToString("0.00m3")} - Raw surface:{rawMeshSurface.ToString("0.00m2")} - Meshes: {usedMeshCount}/{meshCount} (valid/raw)");
				Logging.Log($"Attach nodes surface : {results.attachNodesSurface.ToString("0.00m2")}");
				Logging.Log($"Returned result : Volume:{volume.ToString("0.00m3")} - Surface:{surface.ToString("0.00m2")} - Method used : {results.bestMethod.ToString()}");
			}

			return results;
		}

		static int GetMeshesTotalVolumeAndSurface(List<MeshInfo> meshInfos, out double volume, out double surface, bool logAll = false)
		{
			volume = 0.0;
			surface = 0.0;
			int usedMeshesCount = 0;

			if (meshInfos == null || meshInfos.Count() == 0)
				return usedMeshesCount;

			// sort the meshes by their volume, largest first
			meshInfos.Sort((x, y) => y.volume.CompareTo(x.volume));

			// only account for meshes that are have at least 25% the volume of the biggest mesh, or are at least 0.5 m3, whatever is smaller
			double minMeshVolume = System.Math.Min(meshInfos[0].volume * 0.25, 0.5);

			for (int i = 0; i < meshInfos.Count; i++)
			{
				MeshInfo meshInfo = meshInfos[i];

				// for each mesh bounding box, get the volume of all other meshes bounding boxes intersections
				double intersectedVolume = 0.0;
				foreach (MeshInfo otherMeshInfo in meshInfos)
				{
					if (meshInfo == otherMeshInfo)
						continue;

					// Don't account large meshes whose bounding box volume is greater than 3 times their mesh volume because
					// their bounding box contains too much empty space that may enclose anpther mesh.
					// Typical case : the torus mesh of a gravity ring will enclose the central core mesh
					if (otherMeshInfo.volume > 10.0 && otherMeshInfo.boundsVolume > otherMeshInfo.volume * 3.0)
						continue;

					intersectedVolume += BoundsIntersectionVolume(meshInfo.bounds, otherMeshInfo.bounds);
				}

				if (meshInfo.volume < minMeshVolume)
				{
					if (logAll) Logging.Log($"Found {meshInfo.ToString()} - INTERSECTED VOLUME={intersectedVolume.ToString("0.00m3")} - Mesh rejected : too small");
					continue;
				}

				// exclude meshes whose intersected volume is greater than 75% their bounding box volume
				// always accept the first mesh (since it's the largest, we can assume it's other meshes that intersect it)
				if (i > 0 && intersectedVolume / meshInfo.boundsVolume > 0.75)
				{
					if (logAll) Logging.Log($"Found {meshInfo.ToString()} - INTERSECTED VOLUME={intersectedVolume.ToString("0.00m3")} - Mesh rejected : it is inside another mesh");
					continue;
				}

				if (logAll) Logging.Log($"Found {meshInfo.ToString()} - INTERSECTED VOLUME={intersectedVolume.ToString("0.00m3")} - Mesh accepted");
				usedMeshesCount++;
				volume += meshInfo.volume;

				// account for the full surface of the biggest mesh, then only half for the others
				if (i == 0)
					surface += meshInfo.surface;
				else
					surface += meshInfo.surface * 0.5;
			}

			return usedMeshesCount;
		}

		static List<MeshInfo> GetPartMeshesVolumeAndSurface(Transform partRootTransform, bool ignoreSkinnedMeshes)
		{
			List<MeshInfo> meshInfos = new List<MeshInfo>();

			if (!ignoreSkinnedMeshes)
			{
				SkinnedMeshRenderer[] skinnedMeshRenderers = partRootTransform.GetComponentsInChildren<SkinnedMeshRenderer>(false);
				for (int i = 0; i < skinnedMeshRenderers.Length; i++)
				{
					SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
					Mesh animMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(animMesh);

					MeshInfo meshInfo = new MeshInfo(
						skinnedMeshRenderer.transform.name,
						MeshVolume(animMesh.vertices, animMesh.triangles),
						MeshSurface(animMesh.vertices, animMesh.triangles),
						skinnedMeshRenderer.bounds);

					meshInfos.Add(meshInfo);
				}
			}

			MeshFilter[] meshFilters = partRootTransform.GetComponentsInChildren<MeshFilter>(false);
			int count = meshFilters.Length;

			if (count == 0)
				return meshInfos;

			foreach (MeshFilter meshFilter in meshFilters)
			{
				// Ignore colliders
				if (meshFilter.gameObject.GetComponent<MeshCollider>() != null)
					continue;

				// Ignore non rendered meshes
				MeshRenderer renderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
				if (renderer == null || !renderer.enabled)
					continue;

				Mesh mesh = meshFilter.sharedMesh;
				Vector3 scaleVector = meshFilter.transform.lossyScale;
				float scale = scaleVector.x * scaleVector.y * scaleVector.z;

				Vector3[] vertices;
				if (scale != 1f)
					vertices = ScaleMeshVertices(mesh.vertices, scaleVector);
				else
					vertices = mesh.vertices;

				MeshInfo meshInfo = new MeshInfo(
					meshFilter.transform.name,
					MeshVolume(vertices, mesh.triangles),
					MeshSurface(vertices, mesh.triangles),
					renderer.bounds);

				meshInfos.Add(meshInfo);
			}

			return meshInfos;
		}

		static List<MeshInfo> GetPartMeshCollidersVolumeAndSurface(Transform partRootTransform)
		{
			MeshCollider[] meshColliders = partRootTransform.GetComponentsInChildren<MeshCollider>(false);
			int count = meshColliders.Length;

			List<MeshInfo> meshInfos = new List<MeshInfo>(count);

			if (count == 0)
				return meshInfos;

			foreach (MeshCollider meshCollider in meshColliders)
			{
				Mesh mesh = meshCollider.sharedMesh;
				Vector3 scaleVector = meshCollider.transform.lossyScale;
				float scale = scaleVector.x * scaleVector.y * scaleVector.z;

				Vector3[] vertices;
				if (scale != 1f)
					vertices = ScaleMeshVertices(mesh.vertices, scaleVector);
				else
					vertices = mesh.vertices;

				MeshInfo meshInfo = new MeshInfo(
					meshCollider.transform.name,
					MeshVolume(vertices, mesh.triangles),
					MeshSurface(vertices, mesh.triangles),
					meshCollider.bounds);

				meshInfos.Add(meshInfo);
			}

			return meshInfos;
		}

		/// <summary>
		/// Scale a vertice array (note : this isn't enough to produce a valid unity mesh, would need to recalculate normals and UVs)
		/// </summary>
		static Vector3[] ScaleMeshVertices(Vector3[] sourceVertices, Vector3 scale)
		{
			Vector3[] scaledVertices = new Vector3[sourceVertices.Length];
			for (int i = 0; i < sourceVertices.Length; i++)
			{
				scaledVertices[i] = new Vector3(
					sourceVertices[i].x * scale.x,
					sourceVertices[i].y * scale.y,
					sourceVertices[i].z * scale.z);
			}
			return scaledVertices;
		}

		/// <summary>
		/// Calculate a mesh surface in m^2. WARNING : slow
		/// Very accurate as long as the mesh is fully closed
		/// </summary>
		static double MeshSurface(Vector3[] vertices, int[] triangles)
		{
			if (triangles.Length == 0)
				return 0.0;

			double sum = 0.0;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 corner = vertices[triangles[i]];
				Vector3 a = vertices[triangles[i + 1]] - corner;
				Vector3 b = vertices[triangles[i + 2]] - corner;

				sum += Vector3.Cross(a, b).magnitude;
			}

			return sum / 2.0;
		}

		/// <summary>
		/// Calculate a mesh volume in m^3. WARNING : slow
		/// Very accurate as long as the mesh is fully closed
		/// </summary>
		static double MeshVolume(Vector3[] vertices, int[] triangles)
		{
			double volume = 0f;
			if (triangles.Length == 0)
				return volume;

			Vector3 o = new Vector3(0f, 0f, 0f);
			// Computing the center mass of the polyhedron as the fourth element of each mesh
			for (int i = 0; i < triangles.Length; i++)
			{
				o += vertices[triangles[i]];
			}
			o = o / triangles.Length;

			// Computing the sum of the volumes of all the sub-polyhedrons
			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 p1 = vertices[triangles[i + 0]];
				Vector3 p2 = vertices[triangles[i + 1]];
				Vector3 p3 = vertices[triangles[i + 2]];
				volume += SignedVolumeOfTriangle(p1, p2, p3, o);
			}
			return System.Math.Abs(volume);
		}

		static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 o)
		{
			Vector3 v1 = p1 - o;
			Vector3 v2 = p2 - o;
			Vector3 v3 = p3 - o;

			return Vector3.Dot(Vector3.Cross(v1, v2), v3) / 6f; ;
		}
	}
}
