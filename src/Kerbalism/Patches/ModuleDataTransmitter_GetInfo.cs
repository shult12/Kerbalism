using HarmonyLib;
using KSP.Localization;
using System.Globalization;


namespace KERBALISM
{
    /* Improves module info for stock data transmitter modules. */

    [HarmonyPatch(typeof(ModuleDataTransmitter))]
	[HarmonyPatch("GetInfo")]
	class ModuleDataTransmitter_GetInfo
	{
		static bool Prefix(ModuleDataTransmitter __instance, ref string __result)
		{
			// Patch only if science is enabled
			if (!Features.Science) return true;

			string text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(__instance.antennaType.displayDescription());

			// Antenna type: direct
			string result = Localizer.Format("#autoLOC_7001005", text);

			// Antenna rating: 500km
			result += Localizer.Format("#autoLOC_7001006", HumanReadable.Distance(__instance.antennaPower));
			result += "\n";

			var dsn1 = CommNet.CommNetScenario.RangeModel.GetMaximumRange(__instance.antennaPower, GameVariables.Instance.GetDSNRange(0f));
			var dsn2 = CommNet.CommNetScenario.RangeModel.GetMaximumRange(__instance.antennaPower, GameVariables.Instance.GetDSNRange(0.5f));
			var dsn3 = CommNet.CommNetScenario.RangeModel.GetMaximumRange(__instance.antennaPower, GameVariables.Instance.GetDSNRange(1f));
			result += Lib.BuildString(Localizer.Format("#autoLOC_236834"), " ", HumanReadable.Distance(dsn1));
			result += Lib.BuildString(Localizer.Format("#autoLOC_236835"), " ", HumanReadable.Distance(dsn2));
			result += Lib.BuildString(Localizer.Format("#autoLOC_236836"), " ", HumanReadable.Distance(dsn3));

			double ec = __instance.DataResourceCost * __instance.DataRate;

			Specifics specs = new Specifics();
			specs.Add(Local.DataTransmitter_ECidle, Lib.Color(SI.HumanOrSIRate(ec * Settings.TransmitterPassiveEcFactor, ResourceUnitInfo.ECResID), Lib.Kolor.Orange));//"EC (idle)"

			if (__instance.antennaType != AntennaType.INTERNAL) 
			{
				specs.Add(Local.DataTransmitter_ECTX, Lib.Color(SI.HumanOrSIRate(ec * Settings.TransmitterActiveEcFactor, ResourceUnitInfo.ECResID), Lib.Kolor.Orange));//"EC (transmitting)"
				specs.Add("");
				specs.Add(Local.DataTransmitter_Maxspeed, HumanReadable.DataRate(__instance.DataRate));//"Max. speed"
			}

			__result = Lib.BuildString(result, "\n\n", specs.Info());

			// don't call default implementation
			return false;
		}
	}
}
