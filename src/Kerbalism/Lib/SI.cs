namespace KERBALISM
{
	static class SI
	{
		internal static string HumanOrSIRate(double rate, string unit, int sigFigs = 3, string precision = "F3", bool longPrefix = false)
		{
			if (Settings.UseSIUnits)
				return SIRate(rate, unit, sigFigs, longPrefix);

			return Lib.HumanReadableRate(rate, precision);
		}

		internal static string HumanOrSIRate(double rate, int resID, int sigFigs = 3, string precision = "F3", bool longPrefix = false)
		{
			if (Settings.UseSIUnits && ResourceUnitInfo.GetResourceUnitInfo(resID) is ResourceUnitInfo rui)
			{
				if (rui.UseHuman)
					Lib.HumanReadableRate(rate, precision, rui.RateUnit);
				else
					return SIRate(rate, rui, sigFigs, longPrefix);
			}
			return Lib.HumanReadableRate(rate, precision);
		}

		internal static string HumanOrSIAmount(double amount, int resID, int sigFigs = 3, string append = "", bool longPrefix = false)
		{
			if (Settings.UseSIUnits && ResourceUnitInfo.GetResourceUnitInfo(resID) is ResourceUnitInfo rui)
			{
				if (rui.UseHuman)
					return Lib.HumanReadableAmount(amount, rui.AmountUnit);
				else
					return SIAmount(amount, rui, sigFigs, longPrefix);
			}
			return Lib.HumanReadableAmount(amount, append);
		}


		///<summary> Pretty-print a resource rate (rate is per second). Return an absolute value if a negative one is provided</summary>
		internal static string SIRate(double rate, string unit, int sigFigs = 3, bool longPrefix = false)
		{
			if (rate == 0.0) return Local.Generic_NONE;//"none"
			rate = System.Math.Abs(rate);

			return KSPUtil.PrintSI(rate, unit, sigFigs, longPrefix);
		}

		internal static string SIRate(double rate, int resID, int sigFigs = 3, bool longPrefix = false)
		{
			return SIRate(rate, ResourceUnitInfo.GetResourceUnitInfo(resID), sigFigs, longPrefix);
		}

		internal static string SIRate(double rate, ResourceUnitInfo rui, int sigFigs = 3, bool longPrefix = false)
		{
			return SIRate(rate * rui.MultiplierToUnit, rui.RateUnit, sigFigs, longPrefix);
		}


		internal static string SIAmount(double amount, string unit, int sigFigs = 3, bool longPrefix = false)
		{
			return KSPUtil.PrintSI(amount, unit, sigFigs, longPrefix);
		}

		internal static string SIAmount(double rate, int resID, int sigFigs = 3, bool longPrefix = false)
		{
			return SIAmount(rate, ResourceUnitInfo.GetResourceUnitInfo(resID), sigFigs, longPrefix);
		}

		internal static string SIAmount(double rate, ResourceUnitInfo rui, int sigFigs = 3, bool longPrefix = false)
		{
			return SIAmount(rate * rui.MultiplierToUnit, rui.AmountUnit, sigFigs, longPrefix);
		}


		///<summary> Pretty-print flux (value is in W/m^2)</summary>
		internal static string SIFlux(double flux)
		{
			return KSPUtil.PrintSI(flux, "W/m²");
		}

		///<summary> Pretty-print magnetic strength </summary>
		internal static string SIField(double strength)
		{
			return KSPUtil.PrintSI(strength * 0.000001d, "T"); //< strength is micro-tesla
		}

		///<summary> Pretty-print radiation rate (value is in rem) </summary>
		internal static string SIRadiation(double rad, bool nominal = true)
		{
			if (nominal && rad <= Radiation.Nominal) return Local.Generic_NOMINAL;//"nominal"

			rad *= 3600.0;
			var unit = "rem/h";

			if (Settings.RadiationInSievert)
			{
				rad /= 100.0;
				unit = "Sv/h";
			}
			return KSPUtil.PrintSI(rad, unit, 3);
		}

		///<summary> Pretty-print pressure (value is in kPa) </summary>
		internal static string SIPressure(double v)
		{
			v *= 1000d;
			return KSPUtil.PrintSI(v, "Pa");
		}
	}
}
