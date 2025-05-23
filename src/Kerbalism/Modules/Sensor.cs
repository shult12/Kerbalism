using System;


namespace KERBALISM
{

	// Add a specific environment reading to a part ui, and to the telemetry panel.
	class Sensor : PartModule, ISpecifics
	{
		// config
		[KSPField(isPersistant = true)] public string type;   // type of telemetry provided
		[KSPField] public string pin = string.Empty;        // pin animation

		// status
		[KSPField(guiActive = true, guiName = "_", groupName = "Sensors", groupDisplayName = "#KERBALISM_Group_Sensors", groupStartCollapsed = true)] public string Status;//Sensors

		// animations
		Animator pin_anim;

		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;

			// create animator
			pin_anim = new Animator(part, pin);

			// setup ui
			Fields["Status"].guiName = String.SpacesOnCaps(String.SpacesOnUnderscore(type));
		}


		void Update()
		{
			// in flight
			if (GameLogic.IsFlight())
			{
				// get info from cache
				VesselData vd = vessel.KerbalismData();

				// do nothing if vessel is invalid
				if (!vd.IsSimulated) return;

				// update status
				if (part.IsPAWVisible())
					Status = Telemetry_content(vessel, vd, type);

				// if there is a pin animation
				if (pin.Length > 0)
				{
					// still-play pin animation
					pin_anim.Still(Telemetry_pin(vessel, vd, type));
				}
			}
		}


		// part tooltip
		public override string GetInfo()
		{
			return Specs().Info();
		}

		// specifics support
		public Specifics Specs()
		{
			var specs = new Specifics();
			specs.Add(Local.Sensor_Type, type);//"Type"
			return specs;
		}


		// get readings value in [0,1] range, for pin animation
		internal static double Telemetry_pin(Vessel v, VesselData vd, string type)
		{
			switch (type)
			{
				case "temperature": return System.Math.Min(vd.EnvironmentTemperature / 11000.0, 1.0);
				case "radiation": return System.Math.Min(vd.EnvironmentRadiation * 3600.0 / 11.0, 1.0);
				case "habitat_radiation": return System.Math.Min(HabitatRadiation(vd) * 3600.0 / 11.0, 1.0);
				case "pressure": return System.Math.Min(v.mainBody.GetPressure(v.altitude) / Sim.PressureAtSeaLevel() / 11.0, 1.0);
				case "gravioli": return System.Math.Min(vd.EnvironmentGravioli, 1.0);
			}
			return 0.0;
		}

		// get readings value
		internal static double Telemetry_value(Vessel v, VesselData vd, string type)
		{
			switch (type)
			{
				case "temperature": return vd.EnvironmentTemperature;
				case "radiation": return vd.EnvironmentRadiation;
				case "habitat_radiation": return HabitatRadiation(vd);
				case "pressure": return v.mainBody.GetPressure(v.altitude);
				case "gravioli": return vd.EnvironmentGravioli;
			}
			return 0.0;
		}

		// get readings short text info
		internal static string Telemetry_content(Vessel v, VesselData vd, string type)
		{
			switch (type)
			{
				case "temperature": return HumanReadable.Temp(vd.EnvironmentTemperature);
				case "radiation": return HumanReadable.Radiation(vd.EnvironmentRadiation);
				case "habitat_radiation": return HumanReadable.Radiation(HabitatRadiation(vd));
				case "pressure": return HumanReadable.Pressure(v.mainBody.GetPressure(v.altitude));
				case "gravioli": return vd.EnvironmentGravioli < 0.33 ? Local.Sensor_shorttextinfo1 : vd.EnvironmentGravioli < 0.66 ? Local.Sensor_shorttextinfo2 : Local.Sensor_shorttextinfo3;//"nothing here""almost one""WOW!"
			}
			return string.Empty;
		}

		static double HabitatRadiation(VesselData vd)
		{
			return (1.0 - vd.Shielding) * vd.EnvironmentHabitatRadiation;
		}

		// get readings tooltip
		internal static string Telemetry_tooltip(Vessel v, VesselData vd, string type)
		{
			switch (type)
			{
				case "temperature":
					return String.BuildString
					(
						"<align=left />",
						string.Format("{0,-14}\t<b>{1}</b>\n", Local.Sensor_solarflux, HumanReadable.Flux(vd.EnvironmentSolarFluxTotal)),//"solar flux"
						string.Format("{0,-14}\t<b>{1}</b>\n", Local.Sensor_albedoflux, HumanReadable.Flux(vd.EnvironmentAlbedoFlux)),//"albedo flux"
						string.Format("{0,-14}\t<b>{1}</b>", Local.Sensor_bodyflux, HumanReadable.Flux(vd.EnvironmentBodyFlux))//"body flux"
					);

				case "radiation":
					return string.Empty;

				case "habitat_radiation":
					return String.BuildString
					(
						"<align=left />",
						string.Format("{0,-14}\t<b>{1}</b>\n", Local.Sensor_environment, HumanReadable.Radiation(vd.EnvironmentRadiation, false)),//"environment"
						string.Format("{0,-14}\t<b>{1}</b>", Local.Sensor_habitats, HumanReadable.Radiation(HabitatRadiation(vd), false))//"habitats"
					);

				case "pressure":
					return vd.EnvironmentUnderwater
					  ? Local.Sensor_insideocean//"inside <b>ocean</b>"
					  : vd.EnvironmentInAtmosphere
					  ? Local.Sensor_insideatmosphere.Format(vd.EnvironmentBreathable ? Local.Sensor_breathable : Local.Sensor_notbreathable)//"breathable""not breathable"                  //String.BuildString("inside <b>atmosphere</b> (", vd.EnvBreathable ? "breathable" : "not breathable", ")")
					  : Sim.InsideThermosphere(v)
					  ? Local.Sensor_insidethermosphere//"inside <b>thermosphere</b>""
					  : Sim.InsideExosphere(v)
					  ? Local.Sensor_insideexosphere//"inside <b>exosphere</b>"
					  : string.Empty;

				case "gravioli":
					return String.BuildString
					(
						Local.Sensor_Graviolidetection + " <b>" + vd.EnvironmentGravioli.ToString("F2") + "</b>\n\n",//"Gravioli detection events per-year: 
						"<i>", Local.Sensor_info1, "\n",//The elusive negative gravioli particle\nseems to be much harder to detect than expected.
						Local.Sensor_info2, "</i>"//" On the other\nhand there seems to be plenty\nof useless positive graviolis around."
					);
			}
			return string.Empty;
		}
	}


} // KERBALISM



