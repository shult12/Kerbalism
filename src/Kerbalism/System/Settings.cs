using System.Collections.Generic;


namespace KERBALISM
{


    enum UnlinkedCtrl
    {
        none,     // disable all controls
        limited,  // disable all controls except full/zero throttle and staging
        full      // do not disable controls at all
    }


	static class Settings
	{
		static string MODS_INCOMPATIBLE = "TacLifeSupport,Snacks,KolonyTools,USILifeSupport";
		static string MODS_WARNING = "RemoteTech,CommNetAntennasInfo";
		static string MODS_SCIENCE = "KEI,[x] Science!";

		internal static void Parse()
		{
			var kerbalismConfigNodes = GameDatabase.Instance.GetConfigs("Kerbalism");
			if (kerbalismConfigNodes.Length < 1) return;
			ConfigNode cfg = kerbalismConfigNodes[0].config;

			// profile used
			Profile = Lib.ConfigValue(cfg, "Profile", string.Empty);

			// user-defined features
			Reliability = Lib.ConfigValue(cfg, "Reliability", false);
			Deploy = Lib.ConfigValue(cfg, "Deploy", false);
			Science = Lib.ConfigValue(cfg, "Science", false);
			SpaceWeather = Lib.ConfigValue(cfg, "SpaceWeather", false);
			Automation = Lib.ConfigValue(cfg, "Automation", false);

			// pressure
			PressureFactor = Lib.ConfigValue(cfg, "PressureFactor", 10.0);
			PressureThreshold = Lib.ConfigValue(cfg, "PressureThreshold", 0.9);

			// poisoning
			PoisoningFactor = Lib.ConfigValue(cfg, "PoisoningFactor", 0.0);
			PoisoningThreshold = Lib.ConfigValue(cfg, "PoisoningThreshold", 0.02);

			// signal
			UnlinkedControl = Lib.ConfigEnum(cfg, "UnlinkedControl", UnlinkedCtrl.none);
			DataRateMinimumBitsPerSecond = Lib.ConfigValue(cfg, "DataRateMinimumBitsPerSecond", 1.0f);
			DataRateSurfaceExperiment = Lib.ConfigValue(cfg, "DataRateSurfaceExperiment", 0.3f);
			TransmitterActiveEcFactor = Lib.ConfigValue(cfg, "TransmitterActiveEcFactor", 1.5);
			TransmitterPassiveEcFactor = Lib.ConfigValue(cfg, "TransmitterPassiveEcFactor", 0.04);
			TransmitterActiveEcFactorRT = Lib.ConfigValue(cfg, "TransmitterActiveEcFactorRT", 1.0);
			TransmitterPassiveEcFactorRT = Lib.ConfigValue(cfg, "TransmitterPassiveEcFactorRT", 1.0);
			DampingExponentOverride = Lib.ConfigValue(cfg, "DampingExponentOverride", 0.0);

			// science
			ScienceDialog = Lib.ConfigValue(cfg, "ScienceDialog", true);
			AsteroidSampleMassPerMB = Lib.ConfigValue(cfg, "AsteroidSampleMassPerMB", 0.00002);

			// reliability
			QualityScale = Lib.ConfigValue(cfg, "QualityScale", 4.0);

			// crew level
			LaboratoryCrewLevelBonus = Lib.ConfigValue(cfg, "LaboratoryCrewLevelBonus", 0.2);
			MaxLaborartoryBonus = Lib.ConfigValue(cfg, "MaxLaborartoryBonus", 2.0);
			HarvesterCrewLevelBonus = Lib.ConfigValue(cfg, "HarvesterCrewLevelBonus", 0.1);
			MaxHarvesterBonus = Lib.ConfigValue(cfg, "MaxHarvesterBonus", 2.0);

			// misc
			EnforceCoherency = Lib.ConfigValue(cfg, "EnforceCoherency", true);
			HeadLampsCost = Lib.ConfigValue(cfg, "HeadLampsCost", 0.002);
			LowQualityRendering = Lib.ConfigValue(cfg, "LowQualityRendering", false);
			UIScale = Lib.ConfigValue(cfg, "UIScale", 1.0f);
			UIPanelWidthScale = Lib.ConfigValue(cfg, "UIPanelWidthScale", 1.0f);
			KerbalDeathReputationPenalty = Lib.ConfigValue(cfg, "KerbalDeathReputationPenalty", 100.0f);
			KerbalBreakdownReputationPenalty = Lib.ConfigValue(cfg, "KerbalBreakdownReputationPenalty", 30f);

			// save game settings presets
			LifeSupportAtmoLoss = Lib.ConfigValue(cfg, "LifeSupportAtmoLoss", 50);
			LifeSupportSurvivalTemperature = Lib.ConfigValue(cfg, "LifeSupportSurvivalTemperature", 295);
			LifeSupportSurvivalRange = Lib.ConfigValue(cfg, "LifeSupportSurvivalRange", 5);

			ComfortLivingSpace = Lib.ConfigValue(cfg, "ComfortLivingSpace", 20);
			ComfortFirmGround = Lib.ConfigValue(cfg, "ComfortFirmGround", 0.1f);
			ComfortExercise = Lib.ConfigValue(cfg, "ComfortExercise", 0.2f);
			ComfortNotAlone = Lib.ConfigValue(cfg, "ComfortNotAlone", 0.3f);
			ComfortCallHome = Lib.ConfigValue(cfg, "ComfortCallHome", 0.2f);
			ComfortPanorama = Lib.ConfigValue(cfg, "ComfortPanorama", 0.1f);
			ComfortPlants = Lib.ConfigValue(cfg, "ComfortPlants", 0.1f);

			StormFrequency = Lib.ConfigValue(cfg, "StormFrequency", 0.4f);
			StormRadiation = Lib.ConfigValue(cfg, "StormRadiation", 5.0f);
			StormDurationHours = Lib.ConfigValue(cfg, "StormDurationHours", 2);
			StormEjectionSpeed = Lib.ConfigValue(cfg, "StormEjectionSpeed", 0.33f);
			ShieldingEfficiency = Lib.ConfigValue(cfg, "ShieldingEfficiency", 0.9f);
			ShieldingEfficiencyEasyMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyEasyMult", 1.1f);
			ShieldingEfficiencyModerateMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyModerateMult", 0.9f);
			ShieldingEfficiencyHardMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyHardMult", 0.8f);
			ExternRadiation = Lib.ConfigValue(cfg, "ExternRadiation", 0.04f);
			RadiationInSievert = Lib.ConfigValue(cfg, "RadiationInSievert", false);
			UseSIUnits = Lib.ConfigValue(cfg, "UseSIUnits", false);

			ModsIncompatible = Lib.ConfigValue(cfg, "ModsIncompatible", MODS_INCOMPATIBLE);
			ModsWarning = Lib.ConfigValue(cfg, "ModsWarning", MODS_WARNING);
			ModsScience = Lib.ConfigValue(cfg, "ModsScience", MODS_SCIENCE);
			CheckForCRP = Lib.ConfigValue(cfg, "CheckForCRP", true);

			UseSamplingSunFactor = Lib.ConfigValue(cfg, "UseSamplingSunFactor", false);
			UseResourcePriority = Lib.ConfigValue(cfg, "UseResourcePriority", false);

			// debug / logging
			VolumeAndSurfaceLogging = Lib.ConfigValue(cfg, "VolumeAndSurfaceLogging", false);

			loaded = true;
		}

		// profile used
		internal static string Profile;

		internal static List<string> IncompatibleMods()
		{
			var result = Lib.Tokenize(ModsIncompatible.ToLower(), ',');
			return result;
		}

		internal static List<string> WarningMods()
		{
			var result = Lib.Tokenize(ModsWarning.ToLower(), ',');
			if (Features.Science) result.AddRange(Lib.Tokenize(ModsScience.ToLower(), ','));
			return result;
		}

		// name of profile to use, if any

		// user-defined features
		internal static bool Reliability;                         // component malfunctions and critical failures
		internal static bool Deploy;                              // add EC cost to keep module working/animation, add EC cost to Extend\Retract
		internal static bool Science;                             // science data storage, transmission and analysis
		internal static bool SpaceWeather;                        // coronal mass ejections
		internal static bool Automation;                          // control vessel components using scripts

		// pressure
		internal static double PressureFactor;                    // pressurized modifier value for vessels below the threshold
		internal static double PressureThreshold;                 // level of atmosphere resource that determine pressurized status

		// poisoning
		internal static double PoisoningFactor;                   // poisoning modifier value for vessels below threshold
		internal static double PoisoningThreshold;                // level of waste atmosphere resource that determine co2 poisoning status

		// signal
		internal static UnlinkedCtrl UnlinkedControl;             // available control for unlinked vessels: 'none', 'limited' or 'full'
		internal static float DataRateMinimumBitsPerSecond;       // as long as there is a control connection, the science data rate will never go below this.
		internal static float DataRateSurfaceExperiment;          // transmission rate for surface experiments (Serenity DLC)
		internal static double TransmitterActiveEcFactor;         // how much of the configured EC rate is used while transmitter is active
		internal static double TransmitterPassiveEcFactor;        // how much of the configured EC rate is used while transmitter is passive
		internal static double TransmitterActiveEcFactorRT;       // RemoteTech, how much of the configured EC rate is used while transmitter is active, (transmitting)
		internal static double TransmitterPassiveEcFactorRT;      // RemoteTech, how much of the configured EC rate is used while transmitter is passive, (idle)
		internal static double DampingExponentOverride;           // Kerbalism will calculate a damping exponent to achieve good data communication rates (see log file, search for DataRateDampingExponent). If the calculated value is not good for you, you can set your own.
																// science
		internal static bool ScienceDialog;                       // keep showing the stock science dialog
		internal static double AsteroidSampleMassPerMB;           // When taking an asteroid sample, mass (in t) per MB of sample (baseValue * dataScale). default of 0.00002 => 34 Kg in stock

		// reliability
		internal static double QualityScale;                      // scale applied to MTBF for high-quality components


		// crew level
		internal static double LaboratoryCrewLevelBonus;          // factor for laboratory rate speed gain per crew level above minimum
		internal static double MaxLaborartoryBonus;               // max bonus to be gained by having skilled crew on a laboratory
		internal static double HarvesterCrewLevelBonus;           // factor for harvester speed gain per engineer level above minimum
		internal static double MaxHarvesterBonus;                 // max bonus to be gained by having skilled engineers on a mining rig

		// misc
		internal static bool EnforceCoherency;                    // detect and avoid issues at high timewarp in external modules
		internal static double HeadLampsCost;                     // EC/s cost if eva headlamps are on
		internal static bool LowQualityRendering;                 // use less particles to render the magnetic fields
		internal static float UIScale;                            // scale UI elements by this factor, relative to KSP scaling settings, useful for high PPI screens
		internal static float UIPanelWidthScale;                  // scale UI Panel Width by this factor, relative to KSP scaling settings, useful for high PPI screens
		internal static float KerbalDeathReputationPenalty;       // Reputation penalty when Kerbals dies
		internal static float KerbalBreakdownReputationPenalty;   // Reputation removed when Kerbals loose their marbles in space


		// presets for save game preferences

		internal static int LifeSupportAtmoLoss;
		internal static int LifeSupportSurvivalTemperature;
		internal static int LifeSupportSurvivalRange;
		internal static int ComfortLivingSpace;
		internal static float ComfortFirmGround;
		internal static float ComfortExercise;
		internal static float ComfortNotAlone;
		internal static float ComfortCallHome;
		internal static float ComfortPanorama;
		internal static float ComfortPlants;

		internal static float StormFrequency;
		internal static int StormDurationHours;
		internal static float StormEjectionSpeed;
		internal static float ShieldingEfficiency;
		internal static float ShieldingEfficiencyEasyMult;
		internal static float ShieldingEfficiencyModerateMult;
		internal static float ShieldingEfficiencyHardMult;
		internal static float StormRadiation;
		internal static float ExternRadiation;
		internal static bool RadiationInSievert; // use Sievert iso. rad
		internal static bool UseSIUnits; // use SI units instead of human-readable pretty-printing when available

		// sanity check settings
		static string ModsIncompatible;
		static string ModsWarning;
		static string ModsScience;
		internal static bool CheckForCRP;

		internal static bool UseSamplingSunFactor;
		internal static bool UseResourcePriority;

		// debug / logging
		internal static bool VolumeAndSurfaceLogging;

		internal static bool loaded { get; private set; } = false;
	}


} // KERBALISM
