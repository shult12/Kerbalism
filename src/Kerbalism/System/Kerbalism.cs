using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
	/// <summary>
	/// Main initialization class : for everything that isn't save-game dependant.
	/// For save-dependant things, or things that require the game to be loaded do it in Kerbalism.OnLoad()
	/// </summary>
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismCoreSystems : MonoBehaviour
	{
		void Start()
		{
			// reset the save game initialized flag
			Kerbalism.IsSaveGameInitDone = false;

			// things in here will be only called once per KSP launch, after loading
			// nearly everything is available at this point, including the Kopernicus patched bodies.
			if (!Kerbalism.IsCoreMainMenuInitDone)
			{
				Kerbalism.IsCoreMainMenuInitDone = true;
			}

			// things in here will be called every the player goes to the main menu 
			RemoteTech.EnableInSPC();                   // allow RemoteTech Core to run in the Space Center
		}
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
	public sealed class Kerbalism : ScenarioModule
	{
		#region declarations

		/// <summary> global access </summary>
		internal static Kerbalism Fetch { get; private set; } = null;

		/// <summary> Is the one-time main menu init done. Becomes true after loading, when the the main menu is shown, and never becomes false again</summary>
		internal static bool IsCoreMainMenuInitDone { get; set; } = false;

		/// <summary> Is the one-time on game load init done. Becomes true after the first OnLoad() of a game, and never becomes false again</summary>
		static bool IsCoreGameInitDone { get; set; } = false;

		/// <summary> Is the savegame (or new game) first load done. Becomes true after the first OnLoad(), and false when returning to the main menu</summary>
		internal static bool IsSaveGameInitDone { get; set; } = false;

		// used to setup KSP callbacks
		internal static Callbacks Callbacks { get; private set; }

		// the rendering script attached to map camera
		static MapCameraScript map_camera_script;

		// store time until last update for unloaded vessels
		// note: not using reference_wrapper<T> to increase readability
		sealed class Unloaded_data { internal double time; }; //< reference wrapper
		static Dictionary<Guid, Unloaded_data> unloaded = new Dictionary<Guid, Unloaded_data>();

		// used to update storm data on one body per step
		static int storm_index;
		class Storm_data { internal double time; internal CelestialBody body; };
		static List<Storm_data> storm_bodies = new List<Storm_data>();

		// equivalent to TimeWarp.fixedDeltaTime
		// note: stored here to avoid converting it to double every time
		internal static double elapsed_s;

		// number of steps from last warp blending
		static uint warp_blending;

		/// <summary>Are we in an intermediary timewarp speed ?</summary>
		internal static bool WarpBlending => warp_blending > 2u;

		// last savegame unique id
		static int savegame_uid;

		/// <summary> real time of last game loaded event </summary>
		internal static float gameLoadTime = 0.0f;

		internal static bool SerenityEnabled { get; private set; }

		static bool didSanityCheck = false;

		#endregion

		#region initialization & save/load

		//  constructor
		Kerbalism()
		{
			// enable global access
			Fetch = this;

			SerenityEnabled = Expansions.ExpansionsLoader.IsExpansionInstalled("Serenity");
		}

		void OnDestroy()
		{
			Fetch = null;
		}

		public override void OnLoad(ConfigNode node)
		{
			// everything in there will be called only one time : the first time a game is loaded from the main menu
			if (!IsCoreGameInitDone)
			{
				try
				{
					// core game systems
					Sim.Init();         // find suns (Kopernicus support)
					Radiation.Init();   // create the radiation fields
					ScienceDB.Init();   // build the science database (needs Sim.Init() and Radiation.Init() first)
					Science.Init();     // register the science hijacker

					// static graphic components
					LineRenderer.Init();
					ParticleRenderer.Init();
					Highlighter.Init();

					// UI
					Textures.Init();                      // set up the icon textures
					UI.Init();                                  // message system, main gui, launcher
					KsmGui.KsmGuiMasterController.Init(); // setup the new gui framework

					// part prefabs hacks
					Profile.SetupPods(); // add supply resources to pods
					Misc.PartPrefabsTweaks(); // part prefabs tweaks, must be called after ScienceDB.Init() 

					// Create KsmGui windows
					new ScienceArchiveWindow();

					// GameEvents callbacks
					Callbacks = new Callbacks();
				}
				catch (Exception e)
				{
					string fatalError = SanityCheck(true);
					if (fatalError == null)
						fatalError = string.Empty;
					else
						fatalError += "\n\n";

					fatalError += "FATAL ERROR : Kerbalism core init has failed :" + "\n" + e.ToString();
					Logging.Log(fatalError, Logging.LogLevel.Error);
					LoadFailedPopup(fatalError);
				}

				IsCoreGameInitDone = true;
			}

			// everything in there will be called every time a savegame (or a new game) is loaded from the main menu
			if (!IsSaveGameInitDone)
			{
				try
				{
					Cache.Init();
					ResourceCache.Init();

					// prepare storm data
					foreach (CelestialBody body in FlightGlobals.Bodies)
					{
						if (Storm.Skip_body(body))
							continue;
						Storm_data sd = new Storm_data { body = body };
						storm_bodies.Add(sd);
					}

					BackgroundResources.DisableBackgroundResources();
				}
				catch (Exception e)
				{
					string fatalError = "FATAL ERROR : Kerbalism save game init has failed :" + "\n" + e.ToString();
					Logging.Log(fatalError, Logging.LogLevel.Error);
					LoadFailedPopup(fatalError);
				}

				IsSaveGameInitDone = true;

				Message.Clear();
			}

			// eveything else will be called on every OnLoad() call :
			// - save/load
			// - every scene change
			// - in various semi-random situations (thanks KSP)

			// Fix for background IMGUI textures being dropped on scene changes since KSP 1.8
			Styles.ReloadBackgroundStyles();

			// always clear the caches
			Cache.Clear();
			ResourceCache.Clear();

			// deserialize our database
			try
			{
				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Load");
				DB.Load(node);
				UnityEngine.Profiling.Profiler.EndSample();
			}
			catch (Exception e)
			{
				string fatalError = "FATAL ERROR : Kerbalism save game load has failed :" + "\n" + e.ToString();
				Logging.Log(fatalError, Logging.LogLevel.Error);
				LoadFailedPopup(fatalError);
			}

			// detect if this is a different savegame
			if (DB.uid != savegame_uid)
			{
				// clear caches
				Message.all_logs.Clear();

				// sync main window pos from db
				UI.Sync();

				// remember savegame id
				savegame_uid = DB.uid;
			}

			Kerbalism.gameLoadTime = UnityEngine.Time.time;
		}

		public override void OnSave(ConfigNode node)
		{
			if (!enabled) return;

			// serialize data
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Save");
			DB.Save(node);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void LoadFailedPopup(string error)
		{
			string popupMsg = "Kerbalism has encountered an unrecoverable error and KSP must be closed\n\n";
			popupMsg += "If you can't fix it, ask for help in the <b>kerbalism discord</b> or at the KSP forums thread\n\n";
			popupMsg += "Please provide a screenshot of this message, and your ksp.log file found in your KSP install folder\n\n";
			popupMsg += error;

			UI.Popup("Kerbalism fatal error", popupMsg, 600f);
		}

		#endregion

		#region fixedupdate

		void FixedUpdate()
		{
			// remove control locks in any case
			Misc.ClearLocks();

			// do nothing if paused
			if (GameLogic.IsPaused())
				return;

			// convert elapsed time to double only once
			double fixedDeltaTime = TimeWarp.fixedDeltaTime;

			// and detect warp blending
			if (System.Math.Abs(fixedDeltaTime - elapsed_s) < 0.001)
				warp_blending = 0;
			else
				++warp_blending;

			// update elapsed time
			elapsed_s = fixedDeltaTime;

			// store info for oldest unloaded vessel
			double last_time = 0.0;
			Guid last_id = Guid.Empty;
			Vessel last_v = null;
			VesselData last_vd = null;
			VesselResources last_resources = null;

			// credit science at regular interval
			ScienceDB.CreditScienceBuffers(elapsed_s);

			foreach (VesselData vd in DB.VesselDatas)
			{
				vd.EarlyUpdate();
			}

			// for each vessel
			foreach (Vessel v in FlightGlobals.Vessels)
			{
				// get vessel data
				VesselData vd = v.KerbalismData();

				// update the vessel data validity
				vd.Update(v);

				// set locks for active vessel
				if (v.isActiveVessel)
				{
					Misc.SetLocks(v);
				}

				// maintain eva dead animation and helmet state
				if (v.loaded && v.isEVA)
				{
					EVA.Update(v);
				}

				// keep track of rescue mission kerbals, and gift resources to their vessels on discovery
				if (v.loaded && vd.IsVessel)
				{
					// manage rescue mission mechanics
					Misc.ManageRescueMission(v);
				}

				// do nothing else for invalid vessels
				if (!vd.IsSimulated)
					continue;

				// get resource cache
				VesselResources resources = ResourceCache.Get(v);

				// if loaded
				if (v.loaded)
				{
					//UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.VesselDataEval");
					// update the vessel info
					vd.Evaluate(false, elapsed_s);
					//UnityEngine.Profiling.Profiler.EndSample();

					// get most used resource
					ResourceInfo ec = resources.GetResource(v, "ElectricCharge");

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Radiation");
					// show belt warnings
					Radiation.BeltWarnings(v, vd);

					// update storm data
					Storm.Update(v, vd, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample();

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Comms");
					CommsMessages.Update(v, vd, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample();

					// Habitat equalization
					ResourceBalance.Equalizer(v);

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Science");
					// transmit science data
					Science.Update(v, vd, ec, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample();

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Profile");
					// apply rules
					Profile.Execute(v, vd, resources, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample();

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Profile");
					// part module resource updates
					vd.ResourceUpdate(resources, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample(); 

					UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Resource");
					// apply deferred requests
					resources.Sync(v, vd, elapsed_s);
					UnityEngine.Profiling.Profiler.EndSample();

					// call automation scripts
					vd.computer.Automate(v, vd, resources);

					// remove from unloaded data container
					unloaded.Remove(vd.VesselID);
				}
				// if unloaded
				else
				{
					// get unloaded data, or create an empty one
					Unloaded_data ud;
					if (!unloaded.TryGetValue(vd.VesselID, out ud))
					{
						ud = new Unloaded_data();
						unloaded.Add(vd.VesselID, ud);
					}

					// accumulate time
					ud.time += elapsed_s;

					// maintain oldest entry
					if (ud.time > last_time)
					{
						last_time = ud.time;
						last_v = v;
						last_vd = vd;
						last_resources = resources;
					}
				}
			}

			// at most one vessel gets background processing per physics tick :
			// if there is a vessel that is not the currently loaded vessel, then
			// we will update the vessel whose most recent background update is the oldest
			if (last_v != null)
			{
				//UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.VesselDataEval");
				// update the vessel info (high timewarp speeds reevaluation)
				last_vd.Evaluate(false, last_time);
				//UnityEngine.Profiling.Profiler.EndSample();

				// get most used resource
				ResourceInfo last_ec = last_resources.GetResource(last_v, "ElectricCharge");

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Radiation");
				// show belt warnings
				Radiation.BeltWarnings(last_v, last_vd);

				// update storm data
				Storm.Update(last_v, last_vd, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Comms");
				CommsMessages.Update(last_v, last_vd, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Profile");
				// apply rules
				Profile.Execute(last_v, last_vd, last_resources, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Background");
				// simulate modules in background
				Background.Update(last_v, last_vd, last_resources, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Science");
				// transmit science	data
				Science.Update(last_v, last_vd, last_ec, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Resource");
				// apply deferred requests
				last_resources.Sync(last_v, last_vd, last_time);
				UnityEngine.Profiling.Profiler.EndSample();

				// call automation scripts
				last_vd.computer.Automate(last_v, last_vd, last_resources);

				// remove from unloaded data container
				unloaded.Remove(last_vd.VesselID);
			}

			// update storm data for one body per-step
			if (storm_bodies.Count > 0)
			{
				storm_bodies.ForEach(k => k.time += elapsed_s);
				Storm_data sd = storm_bodies[storm_index];
				Storm.Update(sd.body, sd.time);
				sd.time = 0.0;
				storm_index = (storm_index + 1) % storm_bodies.Count;
			}
		}

		#endregion

		#region Update and GUI

		void Update()
		{
			if (!didSanityCheck)
				SanityCheck();

			// attach map renderer to planetarium camera once
			if (MapView.MapIsEnabled && map_camera_script == null)
				map_camera_script = PlanetariumCamera.Camera.gameObject.AddComponent<MapCameraScript>();

			// process keyboard input
			Misc.KeyboardInput();

			// add description to techs
			Misc.TechDescriptions();

			// set part highlight colors
			Highlighter.Update();

			// prepare gui content
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.UI.Update");
			UI.Update(Callbacks.visible);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void OnGUI()
		{
			UI.On_gui(Callbacks.visible);
		}

		#endregion

		string SanityCheck(bool forced = false)
		{
			// fix PostScreenMessage() not being available for a few updates after scene load since KSP 1.8
			if (!forced)
			{
				if (ScreenMessages.PostScreenMessage("") == null)
				{
					didSanityCheck = false;
					return string.Empty;
				}
				else
				{
					didSanityCheck = true;
				}
			}

			bool harmonyFound = false;
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				if (a.name.ToLower().Contains("harmony"))
					harmonyFound = true;
			}

			if (!harmonyFound)
			{
				string result = "<color=#FF4500><b>HarmonyKSP isn't installed</b></color>\nThis is a required dependency for Kerbalism!";
				DisplayWarning(result);
				enabled = false;
				return result;
			}

			if (!Settings.loaded)
			{
				string result = "<color=#FF4500><b>No Kerbalism configuration found</b></color>\nCheck that you have installed KerbalismConfig (or any other Kerbalism config pack).";
				DisplayWarning(result);
				enabled = false;
				return result;
			}

			List<string> incompatibleMods = Settings.IncompatibleMods();
			List<string> warningMods = Settings.WarningMods();

			List<string> incompatibleModsFound = new List<string>();
			List<string> warningModsFound = new List<string>();

			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				if (incompatibleMods.Contains(a.name.ToLower())) incompatibleModsFound.Add(a.name);
				if (warningMods.Contains(a.name.ToLower())) warningModsFound.Add(a.name);
			}

			string msg = string.Empty;

			var configNodes = GameDatabase.Instance.GetConfigs("Kerbalism");
			if (configNodes.Length > 1)
			{
				msg += "<color=#FF4500>Multiple configurations detected</color>\nHint: delete KerbalismConfig if you are using a custom config pack.\n\n";
			}

			if (Features.Habitat && Settings.CheckForCRP)
			{
				// check for CRP
				var reslib = PartResourceLibrary.Instance.resourceDefinitions;
				if (!reslib.Contains("Oxygen") || !reslib.Contains("Water") || !reslib.Contains("Shielding"))
				{
					msg += "<color=#FF4500>CommunityResourcePack (CRP) is not installed</color>\nYou REALLY need CRP for Kerbalism!\n\n";
				}
			}

			if (incompatibleModsFound.Count > 0)
			{
				msg += "<color=#FF4500>Mods with known incompatibilities found:</color>\n";
				foreach (var m in incompatibleModsFound) msg += "- " + m + "\n";
				msg += "Kerbalism will not run properly with these mods. Please remove them.\n\n";
			}

			if (warningModsFound.Count > 0)
			{
				msg += "<color=#FF4500>Mods with limited compatibility found:</color>\n";
				foreach (var m in warningModsFound) msg += "- " + m + "\n";
				msg += "You might have problems with these mods. Please consult the FAQ on on kerbalism.github.io\n\n";
			}

			DisplayWarning(msg);
			return msg;
		}

		static void DisplayWarning(string msg)
		{
			if (string.IsNullOrEmpty(msg)) return;

			msg = "<b>KERBALISM WARNING</b>\n\n" + msg;
			ScreenMessage sm = new ScreenMessage(msg, 20, ScreenMessageStyle.UPPER_CENTER);
			sm.color = Color.cyan;

			ScreenMessages.PostScreenMessage(sm);
			ScreenMessages.PostScreenMessage(msg, true);
			Logging.Log("Sanity check: " + msg);
		}
	}

	sealed class MapCameraScript: MonoBehaviour
	{
		void OnPostRender()
		{
			// do nothing when not in map view
			// - avoid weird situation when in some user installation MapIsEnabled is true in the space center
			if (!MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.SPACECENTER)
				return;

			// commit all geometry
			Radiation.Render();

			// render all committed geometry
			LineRenderer.Render();
			ParticleRenderer.Render();
		}
	}

	// misc functions
	static class Misc
	{
		internal static void ClearLocks()
		{
			// remove control locks
			InputLockManager.RemoveControlLock("eva_dead_lock");
			InputLockManager.RemoveControlLock("no_signal_lock");
		}

		internal static void SetLocks(Vessel v)
		{
			// lock controls for EVA death
			if (EVA.IsDeadEVA(v))
			{
				InputLockManager.SetControlLock(ControlTypes.EVA_INPUT, "eva_dead_lock");
			}
		}

		internal static void ManageRescueMission(Vessel v)
		{
			// true if we detected this was a rescue mission vessel
			bool detected = false;

			// deal with rescue missions
			foreach (ProtoCrewMember c in Lib.CrewList(v))
			{
				// get kerbal data
				KerbalData kd = DB.Kerbal(c.name);

				// flag the kerbal as not rescue at prelaunch
				if (v.situation == Vessel.Situations.PRELAUNCH)
					kd.rescue = false;

				// if the kerbal belong to a rescue mission
				if (kd.rescue)
				{
					// remember it
					detected = true;

					// flag the kerbal as non-rescue
					// note: enable life support mechanics for the kerbal
					kd.rescue = false;

					// show a message
					Message.Post(String.BuildString(Local.Rescuemission_msg1," <b>", c.name, "</b>"), String.BuildString((c.gender == ProtoCrewMember.Gender.Male ? Local.Kerbal_Male : Local.Kerbal_Female), Local.Rescuemission_msg2));//We found xx  "He"/"She"'s still alive!"
				}
			}

			// gift resources
			if (detected)
			{
				var reslib = PartResourceLibrary.Instance.resourceDefinitions;
				var parts = Lib.GetPartsRecursively(v.rootPart);

				// give the vessel some propellant usable on eva
				string monoprop_name = Lib.EvaPropellantName();
				double monoprop_amount = Lib.EvaPropellantCapacity();
				foreach (var part in parts)
				{
					if (part.CrewCapacity > 0 || part.FindModuleImplementing<KerbalEVA>() != null)
					{
						if (Lib.Capacity(part, monoprop_name) <= double.Epsilon)
						{
							Lib.AddResource(part, monoprop_name, 0.0, monoprop_amount);
						}
						break;
					}
				}
				ResourceCache.Produce(v, monoprop_name, monoprop_amount, ResourceBroker.Generic);

				// give the vessel some supplies
				Profile.SetupRescue(v);
			}
		}

		internal static void TechDescriptions()
		{
			var rnd = RDController.Instance;
			if (rnd == null)
				return;
			var selected = RDController.Instance.node_selected;
			if (selected == null)
				return;
			var techID = selected.tech.techID;
			if (rnd.node_description.text.IndexOf("<i></i>\n", StringComparison.Ordinal) == -1) //< check for state in the string
			{
				rnd.node_description.text += "<i></i>\n"; //< store state in the string

				// collect unique configure-related unlocks
				HashSet<string> labels = new HashSet<string>();
				foreach (AvailablePart p in PartLoader.LoadedPartsList)
				{
					// workaround for FindModulesImplementing nullrefs in 1.8 when called on the strange kerbalEVA_RD_Exp prefab
					// due to the (private) cachedModuleLists being null on it
					if (p.partPrefab.Modules.Count == 0)
						continue;

					foreach (Configure cfg in p.partPrefab.FindModulesImplementing<Configure>())
					{
						foreach (ConfigureSetup setup in cfg.Setups())
						{
							if (setup.tech == selected.tech.techID)
							{
								labels.Add(String.BuildString(setup.name, " to ", cfg.title));
							}
						}
					}
				}

				// add unique configure-related unlocks
				// avoid printing text over the "available parts" section
				int i = 0;
				foreach (string label in labels)
				{
					rnd.node_description.text += String.BuildString("\n• <color=#00ffff>", label, "</color>");
					i++;
					if(i >= 5 && labels.Count > i + 1)
					{
						rnd.node_description.text += String.BuildString("\n• <color=#00ffff>(+", (labels.Count - i).ToString(), " more)</color>");
						break;
					}
				}
			}
		}

		internal static void PartPrefabsTweaks()
		{
			List<string> partSequence = new List<string>();

			partSequence.Add("kerbalism-container-inline-prosemian-full-0625");
			partSequence.Add("kerbalism-container-inline-prosemian-full-125");
			partSequence.Add("kerbalism-container-inline-prosemian-full-250");
			partSequence.Add("kerbalism-container-inline-prosemian-full-375");

			partSequence.Add("kerbalism-container-inline-prosemian-half-125");
			partSequence.Add("kerbalism-container-inline-prosemian-half-250");
			partSequence.Add("kerbalism-container-inline-prosemian-half-375");

			partSequence.Add("kerbalism-container-radial-box-prosemian-small");
			partSequence.Add("kerbalism-container-radial-box-prosemian-normal");
			partSequence.Add("kerbalism-container-radial-box-prosemian-large");

			partSequence.Add("kerbalism-container-radial-pressurized-prosemian-small");
			partSequence.Add("kerbalism-container-radial-pressurized-prosemian-medium");
			partSequence.Add("kerbalism-container-radial-pressurized-prosemian-big");
			partSequence.Add("kerbalism-container-radial-pressurized-prosemian-huge");

			partSequence.Add("kerbalism-solenoid-short-small");
			partSequence.Add("kerbalism-solenoid-long-small");
			partSequence.Add("kerbalism-solenoid-short-large");
			partSequence.Add("kerbalism-solenoid-long-large");

			partSequence.Add("kerbalism-greenhouse");
			partSequence.Add("kerbalism-gravityring");
			partSequence.Add("kerbalism-activeshield");
			partSequence.Add("kerbalism-chemicalplant");


			Dictionary<string, float> iconScales = new Dictionary<string, float>();

			iconScales["kerbalism-container-inline-prosemian-full-0625"] = 0.6f;
			iconScales["kerbalism-container-radial-pressurized-prosemian-small"] = 0.6f;
			iconScales["kerbalism-container-radial-box-prosemian-small"] = 0.6f;

			iconScales["kerbalism-container-inline-prosemian-full-125"] = 0.85f;
			iconScales["kerbalism-container-inline-prosemian-half-125"] = 0.85f;
			iconScales["kerbalism-container-radial-pressurized-prosemian-medium"] = 0.85f;
			iconScales["kerbalism-container-radial-box-prosemian-normal"] = 0.85f;
			iconScales["kerbalism-solenoid-short-small"] = 0.85f;
			iconScales["kerbalism-solenoid-long-small"] = 0.85f;

			iconScales["kerbalism-container-inline-prosemian-full-250"] = 1.1f;
			iconScales["kerbalism-container-inline-prosemian-half-250"] = 1.1f;
			iconScales["kerbalism-container-radial-pressurized-prosemian-big"] = 1.1f;
			iconScales["kerbalism-container-radial-box-prosemian-large"] = 1.1f;

			iconScales["kerbalism-container-inline-prosemian-full-375"] = 1.33f;
			iconScales["kerbalism-container-inline-prosemian-half-375"] = 1.33f;
			iconScales["kerbalism-container-radial-pressurized-prosemian-huge"] = 1.33f;
			iconScales["kerbalism-solenoid-short-large"] = 1.33f;
			iconScales["kerbalism-solenoid-long-large"] = 1.33f;


			foreach (AvailablePart ap in PartLoader.LoadedPartsList)
			{
				// scale part icons of the radial container variants
				if (iconScales.ContainsKey(ap.name))
				{
					float scale = iconScales[ap.name];
					ap.iconPrefab.transform.GetChild(0).localScale *= scale;
					ap.iconScale *= scale;
				}

				// force a non-lexical order in the editor
				if (partSequence.Contains(ap.name))
				{
					int index = partSequence.IndexOf(ap.name);
					ap.title = String.BuildString("<size=1><color=#00000000>" + index.ToString("00") + "</color></size>", ap.title);
				}

				// recompile some part infos (this is normally done by KSP on loading, after each part prefab is compiled)
				// This is needed because :
				// - We can't check interdependent modules when OnLoad() is called, since the other modules may not be loaded yet
				// - The science DB needs the system/bodies to be instantiated, which is done after the part compilation
				bool partNeedsInfoRecompile = false;

				foreach (PartModule module in ap.partPrefab.Modules)
				{
					// we want to remove the editor part tooltip module infos widgets that are switchable trough the configure module
					// because the clutter the UI quite a bit. To do so, every module that implements IConfigurable is made to return
					// an empty string in their GetInfo() if the IConfigurable.ModuleIsConfigured() is ever called on them.
					if (module is Configure configure)
					{
						List<IConfigurable> configurables = configure.GetIConfigurableModules();

						if (configurables.Count > 0)
							partNeedsInfoRecompile = true;

						foreach (IConfigurable configurable in configurables)
							configurable.ModuleIsConfigured();
					}
					// note that the experiment modules on the prefab gets initialized from the scienceDB init, which also do
					// a LoadedPartsList loop to get the scienceDB module infos. So this has to be called after the scienceDB init.
					else if (module is Experiment)
					{
						partNeedsInfoRecompile = true;
					}
				}

				// for some reason this crashes on the EVA kerbals parts
				if (partNeedsInfoRecompile && !ap.name.StartsWith("kerbalEVA"))
				{
					ap.moduleInfos.Clear();
					ap.resourceInfos.Clear();
					try
					{
						Reflection.ReflectionCall(PartLoader.Instance, "CompilePartInfo", new Type[] { typeof(AvailablePart), typeof(Part) }, new object[] { ap, ap.partPrefab });
					}
					catch (Exception ex)
					{
						Logging.Log("Could not patch the moduleInfo for part " + ap.name + " - " + ex.Message + "\n" + ex.StackTrace);
					}
				}
			}
		}

		internal static void KeyboardInput()
		{
			// mute/unmute messages with keyboard
			if (Input.GetKeyDown(KeyCode.Pause))
			{
				if (!Message.IsMuted())
				{
					Message.Post(Local.Messagesmuted, Local.Messagesmuted_subtext);//"Messages muted""Be careful out there"
					Message.Mute();
				}
				else
				{
					Message.Unmute();
					Message.Post(Local.Messagesunmuted);//"Messages unmuted"
				}
			}

			// toggle body info window with keyboard
			if (MapView.MapIsEnabled && Input.GetKeyDown(KeyCode.B))
			{
				UI.Open(BodyInfo.Body_info);
			}

			// call action scripts
			// - avoid creating vessel data for invalid vessels
			Vessel v = FlightGlobals.ActiveVessel;
			if (v == null) return;
			VesselData vd = v.KerbalismData();
			if (!vd.IsSimulated) return;

			// call scripts with 1-5 key
			if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
			{ vd.computer.Execute(v, ScriptType.action1); }
			if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
			{ vd.computer.Execute(v, ScriptType.action2); }
			if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
			{ vd.computer.Execute(v, ScriptType.action3); }
			if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
			{ vd.computer.Execute(v, ScriptType.action4); }
			if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
			{ vd.computer.Execute(v, ScriptType.action5); }
		}

		// return true if the vessel is a rescue mission
		internal static bool IsRescueMission(Vessel v)
		{
			// avoid corner-case situation on the first update : rescue vessel handling code is called
			// after the VesselData creation, causing Vesseldata evaluation to be delayed, causing anything
			// that rely on it to fail on its first update or in OnStart
			if (v.situation == Vessel.Situations.PRELAUNCH)
				return false;

			// if at least one of the crew is flagged as rescue, consider it a rescue mission
			foreach (var c in Lib.CrewList(v))
			{
				if (DB.Kerbal(c.name).rescue)
					return true;
			}


			// not a rescue mission
			return false;
		}

		// kill a kerbal
		// note: you can't kill a kerbal while iterating over vessel crew list, do it outside the loop
		internal static void Kill(Vessel v, ProtoCrewMember c)
		{
			// if on pod
			if (!v.isEVA)
			{
				// if vessel is loaded
				if (v.loaded)
				{
					// find part
					Part part = null;
					foreach (Part p in v.parts)
					{
						if (p.protoModuleCrew.Find(k => k.name == c.name) != null)
						{ part = p; break; }
					}

					// remove kerbal from part
					part.RemoveCrewmember(c);

					// and from vessel
					v.RemoveCrew(c);

					// then kill it
					c.Die();
				}
				// if vessel is not loaded
				else
				{
					// find proto part
					ProtoPartSnapshot part = null;
					foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
					{
						if (p.HasCrew(c.name))
						{ part = p; break; }
					}

					// remove from part
					part.RemoveCrew(c.name);

					// and from vessel
					v.protoVessel.RemoveCrew(c);

					// flag as dead
					c.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
				}

				// forget kerbal data
				DB.KillKerbal(c.name, true);
			}
			// else it must be an eva death
			else
			{
				// flag as eva death
				DB.Kerbal(c.name).eva_dead = true;

				// rename vessel
				v.vesselName = c.name + "'s body";
			}

			// remove reputation
			if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
			{
				Reputation.Instance.AddReputation(-Settings.KerbalDeathReputationPenalty, TransactionReasons.Any);
			}
		}

		// trigger a random breakdown event
		internal static void Breakdown(Vessel v, ProtoCrewMember c)
		{
			// constants
			const double res_penalty = 0.1;        // proportion of food lost on 'depressed' and 'wrong_valve'

			// get a supply resource at random
			ResourceInfo res = null;
			if (Profile.supplies.Count > 0)
			{
				Supply supply = Profile.supplies[Random.RandomInt(Profile.supplies.Count)];
				res = ResourceCache.GetResource(v, supply.resource);
			}

			// compile list of events with condition satisfied
			List<KerbalBreakdown> events = new List<KerbalBreakdown>
			{
				KerbalBreakdown.mumbling //< do nothing, here so there is always something that can happen
			};
			if (Lib.HasData(v))
				events.Add(KerbalBreakdown.fat_finger);
			if (Reliability.CanMalfunction(v))
				events.Add(KerbalBreakdown.rage);
			if (res != null && res.Amount > double.Epsilon)
				events.Add(KerbalBreakdown.wrong_valve);

			// choose a breakdown event
			KerbalBreakdown breakdown = events[Random.RandomInt(events.Count)];

			// generate message
			string text = "";
			string subtext = "";
			switch (breakdown)
			{
				case KerbalBreakdown.mumbling:
					text = Local.Kerbalmumbling;//"$ON_VESSEL$KERBAL has been in space for too long"
					subtext = Local.Kerbalmumbling_subtext;//"Mumbling incoherently"
					break;
				case KerbalBreakdown.fat_finger:
					text = Local.Kerbalfatfinger_subtext;//"$ON_VESSEL$KERBAL is pressing buttons at random on the control panel"
					subtext = Local.Kerbalfatfinger_subtext;//"Science data has been lost"
					break;
				case KerbalBreakdown.rage:
					text = Local.Kerbalrage;//"$ON_VESSEL$KERBAL is possessed by a blind rage"
					subtext = Local.Kerbalrage_subtext;//"A component has been damaged"
					break;
				case KerbalBreakdown.wrong_valve:
					text = Local.Kerbalwrongvalve;//"$ON_VESSEL$KERBAL opened the wrong valve"
					subtext = res.ResourceName + " " + Local.Kerbalwrongvalve_subtext;//has been lost"
					break;
			}

			// post message first so this one is shown before malfunction message
			Message.Post(Severity.breakdown, String.ExpandMsg(text, v, c), subtext);

			// trigger the event
			switch (breakdown)
			{
				case KerbalBreakdown.mumbling:
					break; // do nothing
				case KerbalBreakdown.fat_finger:
					Lib.RemoveData(v);
					break;
				case KerbalBreakdown.rage:
					Reliability.CauseMalfunction(v);
					break;
				case KerbalBreakdown.wrong_valve:
					res.Consume(res.Amount * res_penalty, ResourceBroker.Generic);
					break;
			}

			// remove reputation
			if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
			{
				Reputation.Instance.AddReputation(-Settings.KerbalBreakdownReputationPenalty, TransactionReasons.Any);
			}
		}

		// breakdown events
		enum KerbalBreakdown
		{
			mumbling,         // do nothing (in case all conditions fail)
			fat_finger,       // data has been canceled
			rage,             // components have been damaged
			wrong_valve       // supply resource has been lost
		}
	}


} // KERBALISM
