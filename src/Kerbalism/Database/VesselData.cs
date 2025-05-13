using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class VesselData
	{
		// references
		internal Guid VesselID { get; private set; }
		internal Vessel Vessel { get; private set; }

		// validity
		/// <summary> True if the vessel exists in FlightGlobals. will be false in the editor</summary>
		bool ExistsInFlight { get; set; }
		internal bool IsVessel;              // true if this is a valid vessel
		bool IsRescue;              // true if this is a rescue mission vessel
		bool IsEvaDead;

		/// <summary>False in the following cases : asteroid, debris, flag, deployed ground part, dead eva, rescue</summary>
		internal bool IsSimulated { get; private set; }

		/// <summary>Set to true after evaluation has finished. Used to avoid triggering of events from an uninitialized status</summary>
		bool Evaluated = false;

		// time since last update
		double secondsSinceLastEvaluation;

		/// <summary>
		/// Comms handler for this vessel, evaluate and expose data about the vessel antennas and comm link
		/// </summary>
		internal CommHandler CommHandler { get; private set; }

		internal Drive TransmitBufferDrive { get; private set; }

		#region non-evaluated non-persisted fields
		// there are probably a lot of candidates for this in the current codebase

		/// <summary>name of last file being transmitted, or empty if nothing is being transmitted</summary>
		internal List<File> filesTransmitted;

		#endregion

		#region non-evaluated persisted fields
		// user defined persisted fields
		internal bool configElectricCharge;           // enable/disable message: ec level
		internal bool configSupplies;       // enable/disable message: supplies level
		internal bool configSignal;       // enable/disable message: link status
		internal bool configMalfunctions;  // enable/disable message: malfunctions
		internal bool configStorms;        // enable/disable message: storms
		internal bool configScripts;       // enable/disable message: scripts
		internal bool configHighlights;   // show/hide malfunction highlights
		bool configShowLink;     // show/hide link line
		internal bool configShowVessel;         // show/hide vessel in monitor
		internal Computer computer;     // store scripts
		internal bool deviceTransmit;   // vessel wide automation : enable/disable data transmission

		// other persisted fields
		List<ResourceUpdateDelegate> resourceUpdateDelegates = null; // all part modules that have a ResourceUpdate method
		Dictionary<uint, PartData> parts; // all parts by flightID
		internal Dictionary<uint, PartData>.ValueCollection PartDatas => parts.Values;
		internal PartData GetPartData(uint flightID)
		{
			PartData partData;
			// in some cases (KIS added parts), we might try to get partdata before it is added by part-adding events
			// so we implement a fallback here
			if (!parts.TryGetValue(flightID, out partData))
			{
				foreach (Part part in Vessel.parts)
				{
					if (part.flightID == flightID)
					{
						partData = new PartData(part);
						parts.Add(flightID, partData);
						Logging.LogDebug($"VesselData : newly created part '{part.partInfo.title}' added to vessel '{Vessel.vesselName}'");
					}
				}
			}
			return partData;
		}

		internal bool messageSignal;       // message flag: link status
		internal bool messageBelt;         // message flag: crossing radiation belt
		internal StormData stormData;
		Dictionary<string, SupplyData> supplies; // supplies data
		internal List<uint> scansatID; // used to remember scansat sensors that were disabled
		internal double scienceTransmitted;

		internal Dictionary<Process, DumpSpecs.ActiveValve> dumpValves;

		// persist that so we don't have to do an expensive check every time
		bool IsSerenityGroundController => isSerenityGroundController; bool isSerenityGroundController;
		#endregion

		#region evaluated environment properties
		// Things like vessel situation, sunlight, temperature, radiation, 

		/// <summary>
		/// [environment] true when timewarping faster at 10000x or faster. When true, some fields are updated more frequently
		/// and their evaluation is changed to an analytic, timestep-independant and vessel-position-independant mode.
		/// </summary>
		internal bool EnvironmentIsAnalytic => isAnalytic; bool isAnalytic;

		/// <summary> [environment] true if inside ocean</summary>
		internal bool EnvironmentUnderwater => underwater; bool underwater;

		/// <summary> [environment] true if inside breathable atmosphere</summary>
		internal bool EnvironmentBreathable => breathable; bool breathable;

		/// <summary> [environment] true if on the surface of a body</summary>
		internal bool EnvironmentLanded => landed; bool landed;

		/// <summary> Is the vessel inside an atmosphere ?</summary>
		internal bool EnvironmentInAtmosphere => inAtmosphere; bool inAtmosphere;

		/// <summary> [environment] true if in zero g</summary>
		internal bool EnvironmentZeroG => zeroG; bool zeroG;

		/// <summary> [environment] solar flux reflected from the nearest body</summary>
		internal double EnvironmentAlbedoFlux => albedoFlux; double albedoFlux;

		/// <summary> [environment] infrared radiative flux from the nearest body</summary>
		internal double EnvironmentBodyFlux => bodyFlux; double bodyFlux;

		/// <summary> [environment] total flux at vessel position</summary>
		double EnvironmentTotalFlux => totalFlux; double totalFlux;

		/// <summary> [environment] temperature ar vessel position</summary>
		internal double EnvironmentTemperature => temperature; double temperature;

		/// <summary> [environment] difference between environment temperature and survival temperature</summary>// 
		internal double EnvironmentTemperatureDifference => tempDiff; double tempDiff;

		/// <summary> [environment] radiation at vessel position</summary>
		internal double EnvironmentRadiation => radiation; double radiation;

		/// <summary> [environment] radiation effective for habitats/EVAs</summary>
		internal double EnvironmentHabitatRadiation => shieldedRadiation; double shieldedRadiation;

		/// <summary> [environment] true if vessel is inside a magnetopause (except the heliosphere)</summary>
		internal bool EnvironmentMagnetosphere => magnetosphere; bool magnetosphere;

		/// <summary> [environment] true if vessel is inside a radiation belt</summary>
		internal bool EnvironmentInnerBelt => innerBelt; bool innerBelt;

		/// <summary> [environment] true if vessel is inside a radiation belt</summary>
		internal bool EnvironmentOuterBelt => outerBelt; bool outerBelt;

		/// <summary> [environment] true if vessel is outside sun magnetopause</summary>
		internal bool EnvironmentInterstellar => interstellar; bool interstellar;

		/// <summary> [environment] true if the vessel is inside a magnetopause (except the sun) and under storm</summary>
		internal bool EnvironmentBlackout => blackout; bool blackout;

		/// <summary> [environment] true if vessel is inside thermosphere</summary>
		bool EnvironmentThermosphere => thermosphere; bool thermosphere;

		/// <summary> [environment] true if vessel is inside exosphere</summary>
		bool EnvironmentExosphere => exosphere; bool exosphere;

		/// <summary> [environment] true if vessel is inside exosphere</summary>
		internal bool EnvironmentStorm => inStorm; bool inStorm;

		/// <summary> [environment] true if vessel currently experienced a solar storm</summary>
		internal double EnvironmentStormRadiation => stormRadiation; double stormRadiation;

		/// <summary> [environment] proportion of ionizing radiation not blocked by atmosphere</summary>
		double EnvironmentGammaTransparency => gammaTransparency; double gammaTransparency;

		/// <summary> [environment] gravitation gauge particles detected (joke)</summary>
		internal double EnvironmentGravioli => gravioli; double gravioli;

		/// <summary> [environment] Bodies whose apparent diameter from the vessel POV is greater than ~10 arcmin (~0.003 radians)</summary>
		// real apparent diameters at earth : sun/moon =~ 30 arcmin, Venus =~ 1 arcmin
		internal List<CelestialBody> EnvironmentVisibleBodies => visibleBodies; List<CelestialBody> visibleBodies;

		/// <summary> [environment] Sun that send the highest nominal solar flux (in W/m²) at vessel position</summary>
		internal SunInfo EnvironmentMainSun => mainSun; SunInfo mainSun;

		/// <summary> [environment] Angle of the main sun on the surface at vessel position</summary>
		internal double EnvironmentSunBodyAngle => sunBodyAngle; double sunBodyAngle;

		/// <summary>
		///  [environment] total solar flux from all stars at vessel position in W/m², include atmospheric absorption if inside an atmosphere (atmo_factor)
		/// <para/> zero when the vessel is in shadow while evaluation is non-analytic (low timewarp rates)
		/// <para/> in analytic evaluation, this include fractional sunlight factor
		/// </summary>
		internal double EnvironmentSolarFluxTotal => solarFluxTotal; double solarFluxTotal;

		/// <summary> similar to solar flux total but doesn't account for atmo absorbtion nor occlusion</summary>
		double rawSolarFluxTotal;

		/// <summary> [environment] Average time spend in sunlight, including sunlight from all suns/stars. Each sun/star influence is pondered by its flux intensity</summary>
		internal double EnvironmentSunlightFactor => sunlightFactor; double sunlightFactor;

		/// <summary> [environment] true if the vessel is currently in sunlight, or at least half the time when in analytic mode</summary>
		bool EnvironmentInSunlight => sunlightFactor > 0.49;

		/// <summary> [environment] true if the vessel is currently in shadow, or least 90% of the time when in analytic mode</summary>
		// this threshold is also used to ignore light coming from distant/weak stars 
		internal bool EnvironmentInFullShadow => sunlightFactor < 0.1;

		/// <summary> List of all habitats and their relevant sun shielding parts </summary>
		internal VesselHabitatInfo EnvironmentHabitatInfo => habitatInfo; VesselHabitatInfo habitatInfo;

		/// <summary> [environment] List of all stars/suns and the related data/calculations for the current vessel</summary>
		internal List<SunInfo> EnvironmentSunsInfo => sunsInfo; List<SunInfo> sunsInfo;

		internal VesselSituations VesselSituations => vesselSituations; VesselSituations vesselSituations;

		internal class SunInfo
		{
			/// <summary> reference to the sun/star</summary>
			internal Sim.SunData SunData => sunData; Sim.SunData sunData;

			/// <summary> normalized vector from vessel to sun</summary>
			internal Vector3d Direction => direction; Vector3d direction;

			/// <summary> distance from vessel to sun surface</summary>
			internal double Distance => distance; double distance;

			/// <summary>
			/// return 1.0 when the vessel is in direct sunlight, 0.0 when in shadow
			/// <para/> in analytic evaluation, this is a scalar of representing the fraction of time spent in sunlight
			/// </summary>
			// current limitations :
			// - the result is dependant on the vessel altitude at the time of evaluation, 
			//   consequently it gives inconsistent behavior with highly eccentric orbits
			// - this totally ignore the orbit inclinaison, polar orbits will be treated as equatorial orbits
			internal double SunlightFactor => sunlightFactor; double sunlightFactor;

			/// <summary>
			/// solar flux at vessel position in W/m², include atmospheric absorption if inside an atmosphere (atmo_factor)
			/// <para/> zero when the vessel is in shadow while evaluation is non-analytic (low timewarp rates)
			/// <para/> in analytic evaluation, this include fractional sunlight / atmo absorbtion
			/// </summary>
			internal double SolarFlux => solarFlux; double solarFlux;

			/// <summary>
			/// scalar for solar flux absorbtion by atmosphere at vessel position, not meant to be used directly (use solar_flux instead)
			/// <para/> if integrated over orbit (analytic evaluation), average atmospheric absorption factor over the daylight period (not the whole day)
			/// </summary>
			double AtmoFactor => atmoFactor; double atmoFactor;

			/// <summary> proportion of this sun flux in the total flux at the vessel position (ignoring atmoshere and occlusion) </summary>
			internal double FluxProportion => fluxProportion; double fluxProportion;

			/// <summary> similar to solar flux but doesn't account for atmo absorbtion nor occlusion</summary>
			double rawSolarFlux;

            SunInfo(Sim.SunData sunData) => this.sunData = sunData;

			/// <summary>
			/// Update the 'sunsInfo' list and the 'mainSun', 'solarFluxTotal' variables.
			/// Uses discrete or analytic (for high timewarp speeds) evaluation methods based on the isAnalytic bool.
			/// Require the 'visibleBodies' variable to be set.
			/// </summary>
			// at the two highest timewarp speed, the number of sun visibility samples drop to the point that
			// the quantization error first became noticeable, and then exceed 100%, to solve this:
			// - we switch to an analytical estimation of the sunlight/shadow period
			// - atmo_factor become an average atmospheric absorption factor over the daylight period (not the whole day)
            internal static void UpdateSunsInfo(VesselData vesselData, Vector3d vesselPosition, double elapsedSeconds)
			{
				Vessel vessel = vesselData.Vessel;
				double lastSolarFlux = 0.0;

				vesselData.sunsInfo = new List<SunInfo>(Sim.suns.Count);
				vesselData.solarFluxTotal = 0.0;
				vesselData.rawSolarFluxTotal = 0.0;

				foreach (Sim.SunData sunData in Sim.suns)
				{
					SunInfo sunInfo = new SunInfo(sunData);

					if (vesselData.isAnalytic)
					{
						// get sun direction and distance
						Lib.DirectionAndDistance(vesselPosition, sunInfo.sunData.body, out sunInfo.direction, out sunInfo.distance);

						if (Settings.UseSamplingSunFactor)
							// sampling estimation of the portion of orbit that is in sunlight
							// until we will calculate again
							sunInfo.sunlightFactor = Sim.SampleSunFactor(vessel, elapsedSeconds, sunData.body);
						else
							// analytical estimation of the portion of orbit that was in sunlight.
							// it has some limitations, see the comments on Sim.EclipseFraction
							sunInfo.sunlightFactor = 1.0 - Sim.EclipseFraction(vessel, sunData.body, sunInfo.direction);


						// get atmospheric absorbtion
						// for atmospheric bodies whose rotation period is less than 120 hours,
						// determine analytic atmospheric absorption over a single body revolution instead
						// of using a discrete value that would be unreliable at large timesteps :
						if (vesselData.inAtmosphere)
							sunInfo.atmoFactor = Sim.AtmosphereFactorAnalytic(vessel.mainBody, vesselPosition, sunInfo.direction);
						else
							sunInfo.atmoFactor = 1.0;
					}
					else
					{
						// determine if in sunlight, calculate sun direction and distance
						sunInfo.sunlightFactor = Sim.IsBodyVisible(vessel, vesselPosition, sunData.body, vesselData.visibleBodies, out sunInfo.direction, out sunInfo.distance) ? 1.0 : 0.0;
						// get atmospheric absorbtion
						sunInfo.atmoFactor = Sim.AtmosphereFactor(vessel.mainBody, vesselPosition, sunInfo.direction);
					}

					// get resulting solar flux in W/m²
					sunInfo.rawSolarFlux = sunInfo.sunData.SolarFlux(sunInfo.distance);
					sunInfo.solarFlux = sunInfo.rawSolarFlux * sunInfo.sunlightFactor * sunInfo.atmoFactor;

					// increment total flux from all stars
					vesselData.rawSolarFluxTotal += sunInfo.rawSolarFlux;
					vesselData.solarFluxTotal += sunInfo.solarFlux;

					// add the star to the list
					vesselData.sunsInfo.Add(sunInfo);

					// the most powerful star will be our "default" sun. Uses raw flux before atmo / sunlight factor
					if (sunInfo.rawSolarFlux > lastSolarFlux)
					{
						lastSolarFlux = sunInfo.rawSolarFlux;
						vesselData.mainSun = sunInfo;
					}
				}

				vesselData.sunlightFactor = 0.0;
				foreach (SunInfo sunInfo in vesselData.sunsInfo)
				{
					sunInfo.fluxProportion = sunInfo.rawSolarFlux / vesselData.rawSolarFluxTotal;
					vesselData.sunlightFactor += sunInfo.SunlightFactor * sunInfo.fluxProportion;
				}
				// avoid rounding errors
				if (vesselData.sunlightFactor > 0.99) vesselData.sunlightFactor = 1.0;
			}
		}
		#endregion

		#region evaluated vessel state information properties
		// things like
		// TODO : change all those fields to { get; private set; } properties
		/// <summary>number of crew on the vessel</summary>
		internal int CrewCount => crewCount; int crewCount;

		/// <summary>crew capacity of the vessel</summary>
		internal int CrewCapacity => crewCapacity; int crewCapacity;

		/// <summary>true if at least a component has malfunctioned or had a critical failure</summary>
		internal bool Malfunction => malfunction; bool malfunction;

		/// <summary>true if at least a component had a critical failure</summary>
		internal bool Critical => critical; bool critical;

		/// <summary>connection info</summary>
		internal ConnectionInfo Connection => connection; ConnectionInfo connection;

		/// <summary>enabled volume in m^3</summary>
		internal double Volume => volume; double volume;

		/// <summary>enabled surface in m^2</summary> 
		internal double Surface => surface; double surface;

		/// <summary>normalized pressure</summary>
		internal double Pressure => pressure; double pressure;

		/// <summary>number of EVA's using available Nitrogen</summary>
		internal uint Evas => evas; uint evas;

		/// <summary>waste atmosphere amount versus total atmosphere amount</summary>
		internal double Poisoning => poisoning; double poisoning;

		/// <summary>shielding level</summary>
		internal double Shielding => shielding; double shielding;

		/// <summary>living space factor</summary>
		internal double LivingSpace => livingSpace; double livingSpace;

		/// <summary>Available volume per crew</summary>
		internal double VolumePerCrew => volumePerCrew; double volumePerCrew;

		/// <summary>comfort info</summary>
		internal Comforts Comforts => comforts; Comforts comforts;

		/// <summary>some data about greenhouses</summary>
		internal List<Greenhouse.Data> Greenhouses => greenhouses; List<Greenhouse.Data> greenhouses;

		/// <summary>true if vessel is powered</summary>
		internal bool Powered => powered; bool powered;

		/// <summary>free data storage available data capacity of all public drives</summary>
		internal double DrivesFreeSpace => drivesFreeSpace; double drivesFreeSpace = 0.0;

		/// <summary>data capacity of all public drives</summary>
		internal double DrivesCapacity => drivesCapacity; double drivesCapacity = 0.0;

		/// <summary>evaluated on loaded vessels based on the data pushed by SolarPanelFixer. This doesn't change for unloaded vessel, so the value is persisted</summary>
		internal double SolarPanelsAverageExposure => solarPanelsAverageExposure; double solarPanelsAverageExposure = -1.0;


		List<double> solarPanelsExposure = new List<double>(); // values are added by SolarPanelFixer, then cleared by VesselData once solarPanelsAverageExposure has been computed
		internal void SaveSolarPanelExposure(double exposure) => solarPanelsExposure.Add(exposure); // meant to be called by SolarPanelFixer

		List<ReliabilityInfo> reliabilityStatus;
		internal List<ReliabilityInfo> ReliabilityStatus()
		{
			if (reliabilityStatus != null)
				return reliabilityStatus;

			reliabilityStatus = ReliabilityInfo.BuildList(Vessel);
			return reliabilityStatus;
		}

        internal void ResetReliabilityStatus() => reliabilityStatus = null;

		#endregion

		#region core update handling

		/// <summary> Garanteed to be called for every VesselData in DB before any other method (FixedUpdate/Evaluate) is called </summary>
        internal void EarlyUpdate() => ExistsInFlight = false;

		/// <summary>Called every FixedUpdate for all existing flightglobal vessels </summary>
        internal void Update(Vessel vessel)
		{
			bool isInit = Vessel == null; // debug

			Vessel = vessel;
			ExistsInFlight = true;

			if (!ExistsInFlight || !CheckIfSimulated())
			{
				IsSimulated = false;
			}
			else
			{
				// if vessel wasn't simulated previously : update everything immediately.
				if (!IsSimulated)
				{
					Logging.LogDebug($"VesselData : id '{VesselID}' ({Vessel.vesselName}) is now simulated (wasn't previously)");
					IsSimulated = true;
					Evaluate(true, Random.RandomDouble());
				}
			}

			if (isInit)
				Logging.LogDebug($"Init complete : IsSimulated={IsSimulated}, IsVessel={IsVessel}, IsRescue={IsRescue}, IsEvaDead={IsEvaDead} ({Vessel.vesselName})");
			}

		bool CheckIfSimulated()
		{
			// determine if this is a valid vessel
			IsVessel = Lib.IsVessel(Vessel);

			// determine if this is a rescue mission vessel
			IsRescue = Misc.IsRescueMission(Vessel);

			// dead EVA are not valid vessels
			IsEvaDead = EVA.IsDeadEVA(Vessel);

			return IsVessel && !IsRescue && !IsEvaDead;
		}

		/// <summary>
		/// Evaluate Status and Conditions. Called from Kerbalism.FixedUpdate :
		/// <para/> - for loaded vessels : every gametime second 
		/// <para/> - for unloaded vessels : at the beginning of every background update
		/// </summary>
		internal void Evaluate(bool forced, double elapsedSeconds)
		{
			if (!IsSimulated) return;

			secondsSinceLastEvaluation += elapsedSeconds;

			// don't update more than every second of game time
			if (!forced && secondsSinceLastEvaluation < 1.0)
			{
				UpdateTransmitBufferDrive(elapsedSeconds);
				return;
			}
			
			EvaluateEnvironment(secondsSinceLastEvaluation);
			EvaluateStatus();
			UpdateTransmitBufferDrive(elapsedSeconds);
			secondsSinceLastEvaluation = 0.0;
			Evaluated = true;
		}

        void UpdateTransmitBufferDrive(double elapsedSeconds) => TransmitBufferDrive.dataCapacity = deviceTransmit ? connection.rate * elapsedSeconds : 0.0;

		/// <summary>
		/// Call ResourceUpdate on all part modules that have that method
		/// </summary>
        internal void ResourceUpdate(VesselResources resources, double elapsedSeconds)
		{
			// only do this for loaded vessels. unloaded vessels will be handled in Background.cs
			if (!Vessel.loaded)
				return;

			if (resourceUpdateDelegates == null)
			{
				resourceUpdateDelegates = new List<ResourceUpdateDelegate>();
				foreach(Part part in Vessel.parts)
				{
					foreach(PartModule partModule in part.Modules)
					{
						if (!partModule.isEnabled)
							continue;
						ResourceUpdateDelegate resourceUpdateDelegate = ResourceUpdateDelegate.Instance(partModule);

						if (resourceUpdateDelegate != null)
							resourceUpdateDelegates.Add(resourceUpdateDelegate);
					}
				}
			}

			if (resourceUpdateDelegates.Count == 0)
				return;

			List<ResourceInfo> allResources = resources.GetAllResources(Vessel); // there might be some performance to be gained by caching the list of all resource

			Dictionary<string, double> availableResources = new Dictionary<string, double>();

			foreach (ResourceInfo resource in allResources)
				availableResources[resource.ResourceName] = resource.Amount;

			List<KeyValuePair<string, double>> resourceChangeRequests = new List<KeyValuePair<string, double>>();

			foreach(ResourceUpdateDelegate resourceUpdateDelegate in resourceUpdateDelegates)
			{
				resourceChangeRequests.Clear();
				string title = resourceUpdateDelegate.invoke(availableResources, resourceChangeRequests);
				ResourceBroker broker = ResourceBroker.GetOrCreate(title);
				foreach (KeyValuePair<string, double> resourceChangeRequest in resourceChangeRequests)
				{
					if (resourceChangeRequest.Value > 0)
						resources.Produce(Vessel, resourceChangeRequest.Key, resourceChangeRequest.Value * elapsedSeconds, broker);
					if (resourceChangeRequest.Value < 0)
						resources.Consume(Vessel, resourceChangeRequest.Key, -resourceChangeRequest.Value * elapsedSeconds, broker);
				}
			}
		}

		#endregion

		#region events handling

		void UpdateOnVesselModified()
		{
			if (!IsSimulated)
				return;

			resourceUpdateDelegates = null;
			ResetReliabilityStatus();
			habitatInfo = new VesselHabitatInfo(null);
			EvaluateStatus();
			CommHandler.ResetPartTransmitters();

			Logging.LogDebug($"VesselData updated on vessel modified event ({Vessel.vesselName})");
		}

		/// <summary> Called by GameEvents.onVesselsUndocking, just after 2 vessels have undocked </summary>
		internal static void OnDecoupleOrUndock(Vessel oldVessel, Vessel newVessel)
		{
			Logging.LogDebug($"Decoupling vessel '{newVessel.vesselName}' from vessel '{oldVessel.vesselName}'");

			VesselData oldVesselData = oldVessel.KerbalismData();
			VesselData newVesselData = newVessel.KerbalismData();

			// remove all partdata on the new vessel
			newVesselData.parts.Clear();

			foreach (Part part in newVessel.Parts)
			{
				PartData partData;
				// for all parts in the new vessel, move the corresponding partdata from the old vessel to the new vessel
				if (oldVesselData.parts.TryGetValue(part.flightID, out partData))
				{
					newVesselData.parts.Add(part.flightID, partData);
					oldVesselData.parts.Remove(part.flightID);
				}
			}

			newVesselData.UpdateOnVesselModified();
			oldVesselData.UpdateOnVesselModified();

			Logging.LogDebug($"Decoupling complete for new vessel, newVesselData.parts.Count={newVesselData.parts.Count}, newVessel.parts.Count={newVessel.parts.Count} ({newVessel.vesselName})");
			Logging.LogDebug($"Decoupling complete for old vessel, oldVesselData.parts.Count={oldVesselData.parts.Count}, oldVessel.parts.Count={oldVessel.parts.Count} ({oldVessel.vesselName})");
		}

		// This is for mods (KIS), won't be used in a stock game (the docking is handled in the OnDock method)
		internal static void OnPartCouple(GameEvents.FromToAction<Part, Part> data)
		{
			Logging.LogDebug($"Coupling part '{data.from.partInfo.title}' from vessel '{data.from.vessel.vesselName}' to vessel '{data.to.vessel.vesselName}'");

			Vessel fromVessel = data.from.vessel;
			Vessel toVessel = data.to.vessel;

			VesselData fromVesselData = fromVessel.KerbalismData();
			VesselData toVesselData = toVessel.KerbalismData();

			// GameEvents.onPartCouple may be fired by mods (KIS) that add new parts to an existing vessel
			// In the case of KIS, the part vessel is already set to the destination vessel when the event is fired
			// so we just add the part.
			if (fromVesselData == toVesselData)
			{
				if (!toVesselData.parts.ContainsKey(data.from.flightID))
				{
					toVesselData.parts.Add(data.from.flightID, new PartData(data.from));					
					Logging.LogDebug($"VesselData : newly created part '{data.from.partInfo.title}' added to vessel '{data.to.vessel.vesselName}'");
				}
				return;
			}

			// add all partdata of the docking vessel to the docked to vessel
			foreach (PartData partData in fromVesselData.parts.Values)
			{
				toVesselData.parts.Add(partData.FlightId, partData);
			}
			// remove all partdata from the docking vessel
			fromVesselData.parts.Clear();

			// reset a few things on the docked to vessel
			toVesselData.supplies.Clear();
			toVesselData.scansatID.Clear();
			toVesselData.UpdateOnVesselModified();

			Logging.LogDebug($"Coupling complete to vessel, toVesselData.parts.Count={toVesselData.parts.Count}, toVessel.parts.Count={toVessel.parts.Count} ({toVessel.vesselName})");
			Logging.LogDebug($"Coupling complete from vessel, fromVesselData.parts.Count={fromVesselData.parts.Count}, fromVessel.parts.Count={fromVessel.parts.Count} ({fromVessel.vesselName})");
		}

		internal static void OnPartWillDie(Part part)
		{
			VesselData vesselData = part.vessel.KerbalismData();

			vesselData.parts[part.flightID].OnPartWillDie();
			vesselData.parts.Remove(part.flightID);
			vesselData.UpdateOnVesselModified();

			Logging.LogDebug($"Removing dead part, vesselData.parts.Count={vesselData.parts.Count}, part.vessel.parts.Count={part.vessel.parts.Count} (part '{part.partInfo.title}' in vessel '{part.vessel.vesselName}')");
		}

		#endregion

		#region ctor / init / persistence

		/// <summary> This ctor is to be used for newly created vessels </summary>
		internal VesselData(Vessel vessel)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.Ctor");

			ExistsInFlight = true;	// vessel exists
			IsSimulated = false;	// will be evaluated in next fixedupdate

			Vessel = vessel;
			VesselID = Vessel.id;
			
			parts = new Dictionary<uint, PartData>();
			if (Vessel.loaded)
			{
				foreach (Part part in Vessel.Parts)
					parts.Add(part.flightID, new PartData(part));
			}
			else
			{
				// vessels can be created unloaded, asteroids for example
				foreach (ProtoPartSnapshot protopart in Vessel.protoVessel.protoPartSnapshots)
					parts.Add(protopart.flightID, new PartData(protopart));
			}
			FieldsDefaultInit(vessel.protoVessel);
			InitializeCommHandler();

			Logging.LogDebug($"VesselData ctor (new vessel) : id '{VesselID}' ({Vessel.vesselName}), part count : {parts.Count}");
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <summary>
		/// This ctor is meant to be used in OnLoad only, but can be used as a fallback
		/// with a null ConfigNode to create VesselData from a protovessel. 
		/// The Vessel reference will be acquired in the next fixedupdate
		/// </summary>
		internal VesselData(ProtoVessel protoVessel, ConfigNode node)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.Ctor");
			ExistsInFlight = false;
			IsSimulated = false;

			VesselID = protoVessel.vesselID;

			parts = new Dictionary<uint, PartData>();
			foreach (ProtoPartSnapshot protopart in protoVessel.protoPartSnapshots)
				parts.Add(protopart.flightID, new PartData(protopart));

			if (node == null)
			{
				FieldsDefaultInit(protoVessel);
				Logging.LogDebug($"VesselData ctor (created from protovessel) : id '{VesselID}' ({protoVessel.vesselName}), part count : {parts.Count}");
			}
			else
			{
				Load(node);
				Logging.LogDebug($"VesselData ctor (loaded from database) : id '{VesselID}' ({protoVessel.vesselName}), part count : {parts.Count}");
			}

			InitializeCommHandler();

			UnityEngine.Profiling.Profiler.EndSample();
		}

		// note : this method should work even with a null ProtoVessel
		void FieldsDefaultInit(ProtoVessel protoVessel)
		{
			messageSignal = false;
			messageBelt = false;
			configElectricCharge = PreferencesMessages.Instance.ec;
			configSupplies = PreferencesMessages.Instance.supply;
			configSignal = PreferencesMessages.Instance.signal;
			configMalfunctions = PreferencesMessages.Instance.malfunction;
			configStorms = Features.SpaceWeather && PreferencesMessages.Instance.storm && Lib.CrewCount(protoVessel) > 0;
			configScripts = PreferencesMessages.Instance.script;
			configHighlights = PreferencesReliability.Instance.highlights;
			configShowLink = true;
			configShowVessel = true;
			deviceTransmit = true;

			// note : we check that at vessel creation and persist it, as the vesselType can be changed by the player
			isSerenityGroundController = protoVessel != null && protoVessel.vesselType == VesselType.DeployedScienceController;

			stormData = new StormData(null);
			habitatInfo = new VesselHabitatInfo(null);
			computer = new Computer(null);
			supplies = new Dictionary<string, SupplyData>();
			dumpValves = new Dictionary<Process, DumpSpecs.ActiveValve>();
			scansatID = new List<uint>();
			filesTransmitted = new List<File>();
			vesselSituations = new VesselSituations(this);
		}

		void InitializeCommHandler()
		{
			connection = new ConnectionInfo();
			TransmitBufferDrive = new Drive("buffer drive", 0, 0);
			CommHandler = CommHandler.GetHandler(this, isSerenityGroundController);
		}

		void Load(ConfigNode node)
		{
			messageSignal = Lib.ConfigValue(node, "msg_signal", false);
			messageBelt = Lib.ConfigValue(node, "msg_belt", false);
			configElectricCharge = Lib.ConfigValue(node, "cfg_ec", PreferencesMessages.Instance.ec);
			configSupplies = Lib.ConfigValue(node, "cfg_supply", PreferencesMessages.Instance.supply);
			configSignal = Lib.ConfigValue(node, "cfg_signal", PreferencesMessages.Instance.signal);
			configMalfunctions = Lib.ConfigValue(node, "cfg_malfunction", PreferencesMessages.Instance.malfunction);
			configStorms = Lib.ConfigValue(node, "cfg_storm", PreferencesMessages.Instance.storm);
			configScripts = Lib.ConfigValue(node, "cfg_script", PreferencesMessages.Instance.script);
			configHighlights = Lib.ConfigValue(node, "cfg_highlights", PreferencesReliability.Instance.highlights);
			configShowLink = Lib.ConfigValue(node, "cfg_showlink", true);
			configShowVessel = Lib.ConfigValue(node, "cfg_show", true);

			isSerenityGroundController = Lib.ConfigValue(node, "isGroundCtrl", false);

			deviceTransmit = Lib.ConfigValue(node, "deviceTransmit", true);

			solarPanelsAverageExposure = Lib.ConfigValue(node, "solarPanelsAverageExposure", -1.0);
			scienceTransmitted = Lib.ConfigValue(node, "scienceTransmitted", 0.0);

			stormData = new StormData(node.GetNode("StormData"));
			habitatInfo = new VesselHabitatInfo(node.GetNode("SunShielding"));
			computer = new Computer(node.GetNode("computer"));

			supplies = new Dictionary<string, SupplyData>();
			ConfigNode suppliesNode = node.GetNode("supplies");
			if (suppliesNode != null)
			{
				foreach (ConfigNode supplyNode in suppliesNode.nodes)
				{
					supplies.Add(DB.From_safe_key(supplyNode.name), new SupplyData(supplyNode));
				}
			}

			dumpValves = new Dictionary<Process, DumpSpecs.ActiveValve>();
			ConfigNode dumpSpecsNode = node.GetNode("dump_specs");
			if (dumpSpecsNode != null)
			{
				foreach (ConfigNode.Value dumpValue in node.GetNode("dump_specs").values)
				{
					Process process = Profile.processes.Find(x => x.name == dumpValue.name);
					if (process == null || !int.TryParse(dumpValue.value, out int dumpIndex))
						continue;

					DumpSpecs.ActiveValve valve = new DumpSpecs.ActiveValve(process.dump)
					{
						ValveIndex = dumpIndex
					};

					dumpValves.Add(process, valve);
				}
			}

			scansatID = new List<uint>();
			foreach (string ID in node.GetValues("scansat_id"))
			{
				scansatID.Add(Parse.ToUInt(ID));
			}

			ConfigNode partsNode = new ConfigNode();
			if (node.TryGetNode("parts", ref partsNode))
			{
				foreach (ConfigNode partDataNode in partsNode.nodes)
				{
					PartData partData;
					if (parts.TryGetValue(Parse.ToUInt(partDataNode.name), out partData))
						partData.Load(partDataNode);
				}
			}

			filesTransmitted = new List<File>();
			vesselSituations = new VesselSituations(this);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("msg_signal", messageSignal);
			node.AddValue("msg_belt", messageBelt);
			node.AddValue("cfg_ec", configElectricCharge);
			node.AddValue("cfg_supply", configSupplies);
			node.AddValue("cfg_signal", configSignal);
			node.AddValue("cfg_malfunction", configMalfunctions);
			node.AddValue("cfg_storm", configStorms);
			node.AddValue("cfg_script", configScripts);
			node.AddValue("cfg_highlights", configHighlights);
			node.AddValue("cfg_showlink", configShowLink);
			node.AddValue("cfg_show", configShowVessel);

			node.AddValue("isGroundCtrl", isSerenityGroundController);

			node.AddValue("deviceTransmit", deviceTransmit);

			node.AddValue("solarPanelsAverageExposure", solarPanelsAverageExposure);
			node.AddValue("scienceTransmitted", scienceTransmitted);

			stormData.Save(node.AddNode("StormData"));
			computer.Save(node.AddNode("computer"));

			ConfigNode suppliesNode = node.AddNode("supplies");
			foreach (KeyValuePair<string, SupplyData> supplyNode in supplies)
			{
				supplyNode.Value.Save(suppliesNode.AddNode(DB.To_safe_key(supplyNode.Key)));
			}

			ConfigNode dumpNode = node.AddNode("dump_specs");
			foreach (KeyValuePair<Process, DumpSpecs.ActiveValve> dumpSpec in dumpValves)
			{
				dumpNode.AddValue(dumpSpec.Key.name, dumpSpec.Value.ValveIndex);
			}

			foreach (uint ID in scansatID)
			{
				node.AddValue("scansat_id", ID.ToString());
			}

			EnvironmentHabitatInfo.Save(node.AddNode("SunShielding"));

			ConfigNode partsNode = node.AddNode("parts");
			foreach (PartData partData in parts.Values)
			{
				// currently we only use partdata for drives, so optimize it a bit
				if (partData.Drive != null)
				{
					ConfigNode partNode = partsNode.AddNode(partData.FlightId.ToString());
					partData.Save(partNode);
				}
			}

			if (Vessel != null)
				Logging.LogDebug($"VesselData saved for vessel {Vessel.vesselName}");
			else
				Logging.LogDebug("VesselData saved for vessel (Vessel is null)");

		}
		#endregion

		internal SupplyData Supply(string name)
		{
			if (!supplies.ContainsKey(name))
				supplies.Add(name, new SupplyData());
			
			return supplies[name];
		}

		#region vessel state evaluation
		void EvaluateStatus()
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.EvaluateStatus");
			// determine if there is enough EC for a powered state
			powered = Lib.IsPowered(Vessel);

			// calculate crew info for the vessel
			crewCount = Lib.CrewCount(Vessel);
			crewCapacity = Lib.CrewCapacity(Vessel);

			// malfunction stuff
			malfunction = Reliability.HasMalfunction(Vessel);
			critical = Reliability.HasCriticalFailure(Vessel);

			// communications info
			CommHandler.UpdateConnection(connection);

			// habitat data
			habitatInfo.Update(Vessel);
			volume = Habitat.Tot_volume(Vessel);
			surface = Habitat.Tot_surface(Vessel);
			pressure = System.Math.Min(Habitat.Pressure(Vessel), habitatInfo.MaxPressure);

			evas = (uint)(System.Math.Max(0, ResourceCache.GetResource(Vessel, "Nitrogen").Amount - 330) / Settings.LifeSupportAtmoLoss);
			poisoning = Habitat.Poisoning(Vessel);
			shielding = Habitat.Shielding(Vessel);
			livingSpace = Habitat.Living_space(Vessel);
			volumePerCrew = Habitat.Volume_per_crew(Vessel);
			comforts = new Comforts(Vessel, EnvironmentLanded, crewCount > 1, connection.linked && connection.rate > double.Epsilon);

			// data about greenhouses
			greenhouses = Greenhouse.Greenhouses(Vessel);

			Drive.GetCapacity(this, out drivesFreeSpace, out drivesCapacity);

			// solar panels data
			if (Vessel.loaded)
			{
				solarPanelsAverageExposure = SolarPanelFixer.GetSolarPanelsAverageExposure(solarPanelsExposure);
				solarPanelsExposure.Clear();
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}
		#endregion

		#region environment evaluation
		void EvaluateEnvironment(double elapsedSeconds)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.EvaluateStatus");
			// we use analytic mode if more than 2 minutes of game time has passed since last evaluation (~ x6000 timewarp speed)
			isAnalytic = elapsedSeconds > 120.0;

			// get vessel position
			Vector3d position = Lib.VesselPosition(Vessel);

			// this should never happen again
			if (Vector3d.Distance(position, Vessel.mainBody.position) < 1.0)
				throw new Exception("Shit hit the fan for vessel " + Vessel.vesselName);

			// situation
			underwater = Sim.Underwater(Vessel);
			breathable = Sim.Breathable(Vessel, EnvironmentUnderwater);
			landed = Lib.Landed(Vessel);
			
			inAtmosphere = Vessel.mainBody.atmosphere && Vessel.altitude < Vessel.mainBody.atmosphereDepth;
			zeroG = !EnvironmentLanded && !inAtmosphere;

			visibleBodies = Sim.GetLargeBodies(position);

			// get solar info (with multiple stars / Kopernicus support)
			// get the 'visibleBodies' and 'sunsInfo' lists, the 'mainSun', 'solarFluxTotal' variables.
			// require the situation variables to be evaluated first
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.Sunlight");
			SunInfo.UpdateSunsInfo(this, position, elapsedSeconds);
			UnityEngine.Profiling.Profiler.EndSample();
			sunBodyAngle = Sim.SunBodyAngle(Vessel, position, mainSun.SunData.body);

			// temperature at vessel position
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.Temperature");
			temperature = Sim.Temperature(Vessel, position, solarFluxTotal, out albedoFlux, out bodyFlux, out totalFlux);
			tempDiff = Sim.TempDiff(EnvironmentTemperature, Vessel.mainBody, EnvironmentLanded);
			UnityEngine.Profiling.Profiler.EndSample();

			// radiation
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.VesselData.Radiation");
			gammaTransparency = Sim.GammaTransparency(Vessel.mainBody, Vessel.altitude);

			bool newInnerBelt, newOuterBelt, newMagnetosphere;
			radiation = Radiation.Compute(Vessel, position, EnvironmentGammaTransparency, mainSun.SunlightFactor, out blackout, out newMagnetosphere, out newInnerBelt, out newOuterBelt, out interstellar, out shieldedRadiation);

			if (newInnerBelt != innerBelt || newOuterBelt != outerBelt || newMagnetosphere != magnetosphere)
			{
				innerBelt = newInnerBelt;
				outerBelt = newOuterBelt;
				magnetosphere = newMagnetosphere;

				if (Evaluated)
					API.OnRadiationFieldChanged.Notify(Vessel, innerBelt, outerBelt, magnetosphere);
			}
			UnityEngine.Profiling.Profiler.EndSample();

			thermosphere = Sim.InsideThermosphere(Vessel);
			exosphere = Sim.InsideExosphere(Vessel);
			inStorm = Storm.InProgress(Vessel);

			if (inStorm)
			{
				double sunActivity = Radiation.Info(mainSun.SunData.body).SolarActivity(false) / 2.0;
				stormRadiation = PreferencesRadiation.Instance.StormRadiation * mainSun.SunlightFactor * (sunActivity + 0.5);
			}
			else
			{
				stormRadiation = 0.0;
			}

			vesselSituations.Update();

			// other stuff
			gravioli = Sim.Graviolis(Vessel);
			UnityEngine.Profiling.Profiler.EndSample();
		}
		#endregion
	}
} // KERBALISM
