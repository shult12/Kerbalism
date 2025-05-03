using System;
using System.Collections.Generic;


namespace KERBALISM
{
    static class DB
    {
		internal static void Load(ConfigNode node)
        {
            // get version (or use current one for new savegames)
            string versionStr = Lib.ConfigValue(node, "version", Lib.KerbalismVersion.ToString());
            // sanitize old saves (pre 3.1) format (X.X.X.X) to new format (X.X)
            if (versionStr.Split('.').Length > 2) versionStr = versionStr.Split('.')[0] + "." + versionStr.Split('.')[1];
            version = new Version(versionStr);

            // if this is an unsupported version, print warning
            if (version <= new Version(1, 2)) Logging.Log("loading save from unsupported version " + version);

            // get unique id (or generate one for new savegames)
            uid = Lib.ConfigValue(node, "uid", Lib.RandomInt(int.MaxValue));

			// load kerbals data
			kerbals = new Dictionary<string, KerbalData>();
            if (node.HasNode("kerbals"))
            {
                foreach (var kerbal_node in node.GetNode("kerbals").GetNodes())
                {
                    kerbals.Add(From_safe_key(kerbal_node.name), new KerbalData(kerbal_node));
                }
            }

			// load the science database, has to be before vessels are loaded
			ScienceDB.Load(node);

			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Load.Vessels");
			vessels.Clear();
			// flightstate will be null when first creating the game
			if (HighLogic.CurrentGame.flightState != null)
			{
				ConfigNode vesselsNode = node.GetNode("vessels2");
				if (vesselsNode == null)
					vesselsNode = new ConfigNode();
				// HighLogic.CurrentGame.flightState.protoVessels is what is used by KSP to persist vessels
				// It is always available and synchronized in OnLoad, no matter the scene, excepted on the first OnLoad in a new game
				foreach (ProtoVessel pv in HighLogic.CurrentGame.flightState.protoVessels)
				{
					if (pv.vesselID == Guid.Empty)
					{
						// It seems flags are saved with an empty GUID. skip them.
						Logging.LogDebug("Skipping VesselData load for vessel with empty GUID :" + pv.vesselName);
						continue;
					}

					VesselData vd = new VesselData(pv, vesselsNode.GetNode(pv.vesselID.ToString()));
					vessels.Add(pv.vesselID, vd);
					Logging.LogDebug("VesselData loaded for vessel " + pv.vesselName);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();

			// for compatibility with old saves, convert drives data (it's now saved in PartData)
			if (node.HasNode("drives"))
			{
				Dictionary<uint, PartData> allParts = new Dictionary<uint, PartData>();
				foreach (VesselData vesselData in vessels.Values)
				{
					foreach (PartData partData in vesselData.PartDatas)
					{
						// we had a case of someone having a save with multiple parts having the same flightID
						// 5 duplicates, all were asteroids.
						if (!allParts.ContainsKey(partData.FlightId))
						{
							allParts.Add(partData.FlightId, partData);
						}
					}
				}

				foreach (var drive_node in node.GetNode("drives").GetNodes())
				{
					uint driveId = Lib.Parse.ToUInt(drive_node.name);
					if (allParts.ContainsKey(driveId))
					{
						allParts[driveId].Drive = new Drive(drive_node);
					}
				}
			}

			// load bodies data
			storms = new Dictionary<string, StormData>();
            if (node.HasNode("bodies"))
            {
                foreach (var body_node in node.GetNode("bodies").GetNodes())
                {
                    storms.Add(From_safe_key(body_node.name), new StormData(body_node));
                }
            }

            // load landmark data
            if (node.HasNode("landmarks"))
            {
                landmarks = new LandmarkData(node.GetNode("landmarks"));
            }
            else
            {
                landmarks = new LandmarkData();
            }

            // load ui data
            if (node.HasNode("ui"))
            {
                ui = new UIData(node.GetNode("ui"));
            }
            else
            {
                ui = new UIData();
            }

			// if an old savegame was imported, log some debug info
			if (version != Lib.KerbalismVersion) Logging.Log("savegame converted from version " + version + " to " + Lib.KerbalismVersion);
        }

		internal static void Save(ConfigNode node)
        {
            // save version
            node.AddValue("version", Lib.KerbalismVersion.ToString());

            // save unique id
            node.AddValue("uid", uid);

			// save kerbals data
			var kerbals_node = node.AddNode("kerbals");
            foreach (var p in kerbals)
            {
                p.Value.Save(kerbals_node.AddNode(To_safe_key(p.Key)));
            }

			// only persist vessels that exists in KSP own vessel persistence
			// this prevent creating junk data without going into the mess of using gameevents
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Save.Vessels");
			ConfigNode vesselsNode = node.AddNode("vessels2");
			foreach (ProtoVessel pv in HighLogic.CurrentGame.flightState.protoVessels)
			{
				if (pv.vesselID == Guid.Empty)
				{
					// It seems flags are saved with an empty GUID. skip them.
					Logging.LogDebug("Skipping VesselData save for vessel with empty GUID :" + pv.vesselName);
					continue;
				}

				VesselData vd = pv.KerbalismData();
				ConfigNode vesselNode = vesselsNode.AddNode(pv.vesselID.ToString());
				vd.Save(vesselNode);
			}
			UnityEngine.Profiling.Profiler.EndSample();

			// save the science database
			ScienceDB.Save(node);

            // save bodies data
            var bodies_node = node.AddNode("bodies");
            foreach (var p in storms)
            {
                p.Value.Save(bodies_node.AddNode(To_safe_key(p.Key)));
            }

            // save landmark data
            landmarks.Save(node.AddNode("landmarks"));

            // save ui data
            ui.Save(node.AddNode("ui"));
        }


		internal static KerbalData Kerbal(string name)
        {
            if (!kerbals.ContainsKey(name))
            {
                kerbals.Add(name, new KerbalData());
            }
            return kerbals[name];
        }

		internal static VesselData KerbalismData(this Vessel vessel)
		{
			VesselData vd;
			if (!vessels.TryGetValue(vessel.id, out vd))
			{
				Logging.LogDebug("Creating Vesseldata for new vessel " + vessel.vesselName);
				vd = new VesselData(vessel);
				vessels.Add(vessel.id, vd);
			}
			return vd;
		}

		internal static VesselData KerbalismData(this ProtoVessel protoVessel)
		{
			VesselData vd;
			if (!vessels.TryGetValue(protoVessel.vesselID, out vd))
			{
				Logging.Log("VesselData for protovessel " + protoVessel.vesselName + ", ID=" + protoVessel.vesselID + " doesn't exist !", Logging.LogLevel.Warning);
				vd = new VesselData(protoVessel, null);
				vessels.Add(protoVessel.vesselID, vd);
			}
			return vd;
		}

		/// <summary>shortcut for VesselData.IsValid. False in the following cases : asteroid, debris, flag, deployed ground part, dead eva, rescue</summary>
		internal static bool KerbalismIsValid(this Vessel vessel)
        {
            return KerbalismData(vessel).IsSimulated;
        }

		internal static Dictionary<Guid, VesselData>.ValueCollection VesselDatas => vessels.Values;

		internal static StormData Storm(string name)
        {
            if (!storms.ContainsKey(name))
            {
                storms.Add(name, new StormData(null));
            }
            return storms[name];
        }

		internal static Boolean ContainsKerbal(string name)
        {
            return kerbals.ContainsKey(name);
        }

		/// <summary>
		/// Remove a Kerbal and his lifetime data from the database
		/// </summary>
		internal static void KillKerbal(String name, bool reallyDead)
        {
            if (reallyDead)
            {
                kerbals.Remove(name);
            }
            else
            {
                // called when a vessel is destroyed. don't remove the kerbal just yet,
                // check with the roster if the kerbal is dead or not
                Kerbal(name).Recover();
            }
        }

		/// <summary>
		/// Resets all process data of a kerbal, except lifetime data
		/// </summary>
		internal static void RecoverKerbal(string name)
        {
            if (ContainsKerbal(name))
            {
                if (Kerbal(name).eva_dead)
                {
                    kerbals.Remove(name);
                }
                else
                {
                    Kerbal(name).Recover();
                }
            }
        }

		internal static Dictionary<string, KerbalData> Kerbals()
        {
            return kerbals;
        }

		internal static string To_safe_key(string key) { return key.Replace(" ", "___"); }
		internal static string From_safe_key(string key) { return key.Replace("___", " "); }

        static Version version;                         // savegame version
		internal static int uid;                                 // savegame unique id
        static Dictionary<string, KerbalData> kerbals; // store data per-kerbal
        static Dictionary<Guid, VesselData> vessels = new Dictionary<Guid, VesselData>();    // store data per-vessel
        static Dictionary<string, StormData> storms;     // store data per-body
		internal static LandmarkData landmarks;                  // store landmark data
		internal static UIData ui;                               // store ui data

		#region VESSELDATA METHODS

		internal static bool TryGetVesselDataTemp(this Vessel vessel, out VesselData vesselData)
		{
			if (!vessels.TryGetValue(vessel.id, out vesselData))
			{
				Logging.LogStack($"Could not get VesselData for vessel {vessel.vesselName}", Logging.LogLevel.Error);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Get the VesselData for this vessel, if it exists. Typically, you will need this in a Foreach on FlightGlobals.Vessels
		/// </summary>
		internal static bool TryGetVesselData(this Vessel vessel, out VesselData vesselData)
		{
			if (!vessels.TryGetValue(vessel.id, out vesselData))
				return false;

			return true;
		}

		/// <summary>
		/// Get the VesselData for this vessel. Will return null if that vessel isn't yet created in the DB, which can happen if this is called too early. <br/>
		/// Typically it's safe to use from partmodules FixedUpdate() and OnStart(), but not in Awake() and probably not from Update()<br/>
		/// Also, don't use this in a Foreach on FlightGlobals.Vessels, check the result of TryGetVesselData() instead
		/// </summary>
		internal static VesselData GetVesselData(this Vessel vessel)
		{
			if (!vessels.TryGetValue(vessel.id, out VesselData vesselData))
			{
				Logging.LogStack($"Could not get VesselData for vessel {vessel.vesselName}");
				return null;
			}
			return vesselData;
		}

		internal static bool TryGetVesselData(this ProtoVessel protoVessel, out VesselData vesselData)
		{
			return vessels.TryGetValue(protoVessel.vesselID, out vesselData);
		}

		#endregion
	}


} // KERBALISM



