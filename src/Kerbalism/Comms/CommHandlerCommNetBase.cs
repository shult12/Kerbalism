using CommNet;
using HarmonyLib;
using KSP.Localization;
using System;
using System.Reflection;

namespace KERBALISM
{
	class CommHandlerCommNetBase : CommHandler
	{
		/// <summary> base data rate set in derived classes from UpdateTransmitters()</summary>
		protected double baseRate = 0.0;

		protected override bool NetworkIsReady => CommNetNetwork.Initialized && CommNetNetwork.Instance?.CommNet != null;

		protected override void UpdateNetwork(ConnectionInfo connection)
		{
			Vessel v = vd.Vessel;

			bool vIsNull = v == null || v.connection == null;

			connection.linked = !vIsNull && connection.powered && v.connection.IsConnected;

			if (!connection.linked)
			{
				connection.strength = 0.0;
				connection.rate = 0.0;
				connection.target_name = string.Empty;
				connection.control_path.Clear();

				if (!vIsNull && v.connection.InPlasma)
				{
					if (connection.storm)
						connection.Status = LinkStatus.storm;
					else
						connection.Status = LinkStatus.plasma;
				}
				else
				{
					connection.Status = LinkStatus.no_link;
				}

				return;
			}

			CommLink firstLink = v.connection.ControlPath.First;
			connection.Status = firstLink.hopType == HopType.Home ? LinkStatus.direct_link : LinkStatus.indirect_link;
			connection.strength = firstLink.signalStrength;

			connection.rate = baseRate * System.Math.Pow(firstLink.signalStrength, DataRateDampingExponent);

			connection.target_name = String.Ellipsis(Localizer.Format(v.connection.ControlPath.First.end.displayName).Replace("Kerbin", "DSN"), 20);

			if (connection.Status != LinkStatus.direct_link)
			{
				Vessel firstHop = CommNodeToVessel(v.Connection.ControlPath.First.end);
				// Get rate from the firstHop, each Hop will do the same logic, then we will have the min rate for whole path
				if (firstHop == null || !firstHop.TryGetVesselData(out VesselData vd))
					connection.rate = 0.0;
				else
					connection.rate = System.Math.Min(vd.Connection.rate, connection.rate);
			}

			connection.control_path.Clear();
			foreach (CommLink link in v.connection.ControlPath)
			{
				double antennaPower = link.end.isHome ? link.start.antennaTransmit.power + link.start.antennaRelay.power : link.start.antennaTransmit.power;
				double linkDistance = (link.start.position - link.end.position).magnitude;
				double linkMaxDistance = System.Math.Sqrt(antennaPower * link.end.antennaRelay.power);
				double signalStrength = 1 - (linkDistance / linkMaxDistance);
				signalStrength = (3 - (2 * signalStrength)) * System.Math.Pow(signalStrength, 2);
				signalStrength = System.Math.Pow(signalStrength, DataRateDampingExponent);

				string[] controlPoint = new string[3];

				// name
				controlPoint[0] = Localizer.Format(link.end.displayName);
				if (link.end.isHome)
					controlPoint[0] = controlPoint[0].Replace("Kerbin", "DSN");
				controlPoint[0] = String.Ellipsis(controlPoint[0], 35);

				// signal strength
				controlPoint[1] = HumanReadable.Percentage(System.Math.Ceiling(signalStrength * 10000) / 10000, "F2");

				// extra info
				controlPoint[2] = String.BuildString(
					"Distance: ", HumanReadable.Distance(linkDistance),
					" (Max: ", HumanReadable.Distance(linkMaxDistance), ")");

				connection.control_path.Add(controlPoint);
			}

			// set minimal data rate to what is defined in Settings (1 bit/s by default) 
			if (connection.rate > 0.0 && connection.rate * HumanReadable.bitsPerMB < Settings.DataRateMinimumBitsPerSecond)
				connection.rate = Settings.DataRateMinimumBitsPerSecond / HumanReadable.bitsPerMB;
		}

		static double dampingExponent = 0;
		static double DataRateDampingExponent
		{
			get
			{
				if (dampingExponent != 0)
					return dampingExponent;

				if (Settings.DampingExponentOverride != 0)
					return Settings.DampingExponentOverride;

				// KSP calculates the signal strength using a cubic formula based on distance (see below).
				// Based on that signal strength, we calculate a data rate. The goal is to get data rates that
				// are comparable to what NASA gets near Mars, depending on the distance between Earth and Mars
				// (~0.36 AU - ~2.73 AU).
				// The problem is that KSPs formula would be somewhat correct for signal strength in reality,
				// but the stock system is only 1/10th the size of the real solar system. Picture this: Jools
				// orbit is about as far removed from the sun as the real Mercury, which means that all other
				// planets would orbit the sun at a distance that is even smaller. In game, distance plays a
				// much smaller role than it would in reality, because the in-game distances are very small,
				// so signal strength just doesn't degrade fast enough with distance.
				//
				// We cannot change how KSP calculates signal strength, so we apply a damping formula
				// for the data rate. Basically, it goes like this:
				//
				// data rate = base rate * signal strength
				// (base rate would be the max. rate at 0 distance)
				//
				// To degrade the data rate with distance, Kerbalism will do this instead:
				//
				// data rate = base rate * (signal strength ^ damping exponent)
				// (this works because signal strength will always be in the range [0..1])
				//
				// The problem is, we don't know which solar system we'll be in, and how big it will be.
				// Popular systems like JNSQ are 2.7 times bigger than stock, RSS is 10 times bigger.
				// So we try to find a damping exponent that gives good results for the solar system we're in,
				// based on the distance of the home planet to the sun (1 AU).

				// range of DSN at max. level
				var maxDsnRange = GameVariables.Instance.GetDSNRange(1f);

				// signal strength at ~ average earth - mars distance
				var strengthAt2AU = SignalStrength(maxDsnRange, 2 * Sim.AU);

				// For our estimation, we assume a base rate similar to the stock communotron 88-88
				var baseRate = 0.48;

				// At 2 AU, this is the rate we want to get out of it
				// Value selected so we match pre-comms refactor damping exponent of ~6 in stock
				var desiredRateAt2AU = 0.3925;

				// dataRate = baseRate * (strengthAt2AU ^ exponent)
				// so...
				// exponent = log_strengthAt2AU(dataRate / baseRate)
				dampingExponent = System.Math.Log(desiredRateAt2AU / baseRate, strengthAt2AU);

				Logging.Log($"Calculated DataRateDampingExponent: {dampingExponent.ToString("F4")} (max. DSN range: {maxDsnRange.ToString("F0")}, strength at 2 AU: {strengthAt2AU.ToString("F3")})");

				return dampingExponent;
			}
		}

		static Vessel CommNodeToVessel(CommNode node)
		{
			return node?.transform?.gameObject.GetComponent<Vessel>();
		}

		internal static void ApplyHarmonyPatches()
		{
			MethodInfo CommNetVessel_OnNetworkPreUpdate_Info = AccessTools.Method(typeof(CommNetVessel), nameof(CommNetVessel.OnNetworkPreUpdate));

			Loader.HarmonyInstance.Patch(CommNetVessel_OnNetworkPreUpdate_Info,
				new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase), nameof(CommNetVessel_OnNetworkPreUpdate_Prefix))));

			Loader.HarmonyInstance.Patch(CommNetVessel_OnNetworkPreUpdate_Info,
				null, new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase), nameof(CommNetVessel_OnNetworkPreUpdate_Postfix))));

			MethodInfo CommNetVessel_OnNetworkPostUpdate_Info = AccessTools.Method(typeof(CommNetVessel), nameof(CommNetVessel.OnNetworkPostUpdate));

			Loader.HarmonyInstance.Patch(CommNetVessel_OnNetworkPostUpdate_Info,
				new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase), nameof(CommNetVessel_OnNetworkPostUpdate_Prefix))));

			MethodInfo CommNetVessel_GetSignalStrengthModifier_Info = AccessTools.Method(typeof(CommNetVessel), nameof(CommNetVessel.GetSignalStrengthModifier));

			Loader.HarmonyInstance.Patch(CommNetVessel_GetSignalStrengthModifier_Info,
				new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase), nameof(CommNetVessel_GetSignalStrengthModifier_Prefix))));
		}

		// ensure unloadedDoOnce is true for unloaded vessels
		static void CommNetVessel_OnNetworkPreUpdate_Prefix(CommNetVessel __instance, ref bool ___unloadedDoOnce)
		{
			if (!__instance.Vessel.loaded && __instance.CanComm)
				___unloadedDoOnce = true;
		}


		// ensure unloadedDoOnce is true for unloaded vessels
		static void CommNetVessel_OnNetworkPostUpdate_Prefix(CommNetVessel __instance, ref bool ___unloadedDoOnce)
		{
			if (!__instance.Vessel.loaded && __instance.CanComm)
				___unloadedDoOnce = true;
		}


		// apply storm radiation factor to the comm strength multiplier used by stock for plasma blackout
		static void CommNetVessel_OnNetworkPreUpdate_Postfix(CommNetVessel __instance, ref bool ___inPlasma, ref double ___plasmaMult)
		{
			if (!__instance.CanComm || !__instance.Vessel.TryGetVesselData(out VesselData vd))
				return;

			if (vd.EnvStormRadiation > 0.0)
			{
				___inPlasma = true;
				___plasmaMult = vd.EnvStormRadiation * 2.0 / PreferencesRadiation.Instance.StormRadiation; // We should probably have a threshold setting instead of this hardcoded formula
				___plasmaMult = System.Math.Max(1.0 - ___plasmaMult, 0.0);
			}
		}

		// apply storm radiation factor to the comm strength multiplier used by stock for plasma blackout
		static bool CommNetVessel_GetSignalStrengthModifier_Prefix(CommNetVessel __instance, bool ___canComm, bool ___inPlasma, double ___plasmaMult, out double __result)
		{
			if (!___canComm)
			{
				__result = 0.0;
				return false;
			}

			if (!___inPlasma)
			{
				__result = 1.0;
				return false;
			}

			if (__instance.Vessel.TryGetVesselData(out VesselData vd) && vd.EnvStormRadiation > 0.0)
			{
				__result = ___plasmaMult;
				return false;
			}

			__result = 0.0;
			return true;
		}
	}
}
