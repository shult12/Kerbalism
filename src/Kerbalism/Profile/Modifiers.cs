using KERBALISM.Planner;
using System;
using System.Collections.Generic;


namespace KERBALISM
{


	static class Modifiers
	{
		///<summary> Modifiers Evaluate method used for the Monitors background and current vessel simulation </summary>
		internal static double Evaluate(Vessel v, VesselData vd, VesselResources resources, List<string> modifiers)
		{
			double k = 1.0;
			foreach (string mod in modifiers)
			{
				switch (mod)
				{
					case "zerog":
						k *= vd.EnvironmentZeroG ? 1.0 : 0.0;
						break;

					case "landed":
						k *= vd.EnvironmentLanded ? 1.0 : 0.0;
						break;

					case "breathable":
						k *= vd.EnvironmentBreathable ? 1.0 : 0.0;
						break;

					case "non_breathable":
						k *= vd.EnvironmentBreathable ? 0.0 : 1.0;
						break;

					case "temperature":
						k *= vd.EnvironmentTemperatureDifference;
						break;

					case "radiation":
						k *= vd.EnvironmentHabitatRadiation;
						break;

					case "shielding":
						k *= 1.0 - vd.Shielding;
						break;

					case "volume":
						k *= vd.Volume;
						break;

					case "surface":
						k *= vd.Surface;
						break;

					case "living_space":
						k /= vd.LivingSpace;
						break;

					case "comfort":
						k /= vd.Comforts.factor;
						break;

					case "pressure":
						k *= vd.Pressure > Settings.PressureThreshold ? 1.0 : Settings.PressureFactor;
						break;

					case "poisoning":
						k *= vd.Poisoning > Settings.PoisoningThreshold ? 1.0 : Settings.PoisoningFactor;
						break;

					case "per_capita":
						k /= (double)System.Math.Max(vd.CrewCount, 1);
						break;

					default:
						k *= resources.GetResource(v, mod).Amount;
						break;
				}
			}
			return k;
		}


		///<summary> Modifiers Evaluate method used for the Planners vessel simulation in the VAB/SPH </summary>
		internal static double Evaluate(EnvironmentAnalyzer env, VesselAnalyzer va, ResourceSimulator sim, List<string> modifiers)
		{
			double k = 1.0;
			foreach (string mod in modifiers)
			{
				switch (mod)
				{
					case "zerog":
						k *= env.zerog ? 1.0 : 0.0;
						break;

					case "landed":
						k *= env.landed ? 1.0 : 0.0;
						break;

					case "breathable":
						k *= env.breathable ? 1.0 : 0.0;
						break;

					case "non_breathable":
						k *= env.breathable ? 0.0 : 1.0;
						break;

					case "temperature":
						k *= env.temp_diff;
						break;

					case "radiation":
						k *= System.Math.Max(Radiation.Nominal, (env.landed ? env.surface_rad : env.magnetopause_rad) + va.emitted);
						break;

					case "shielding":
						k *= 1.0 - va.shielding;
						break;

					case "volume":
						k *= va.volume;
						break;

					case "surface":
						k *= va.surface;
						break;

					case "living_space":
						k /= va.living_space;
						break;

					case "comfort":
						k /= va.comforts.factor;
						break;

					case "pressure":
						k *= va.pressurized ? 1.0 : Settings.PressureFactor;
						break;

					case "poisoning":
						k *= !va.scrubbed ? 1.0 : Settings.PoisoningFactor;
						break;

					case "per_capita":
						k /= (double)System.Math.Max(va.crew_count, 1);
						break;

					default:
						k *= sim.Resource(mod).amount;
						break;
				}
			}
			return k;
		}
	}


} // KERBALISM
