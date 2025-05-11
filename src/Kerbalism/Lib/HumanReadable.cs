namespace KERBALISM
{
	static class HumanReadable
	{
		internal const string InlineSpriteScience = "<sprite=\"CurrencySpriteAsset\" name=\"Science\" color=#6DCFF6>";
		internal const string InlineSpriteFunds = "<sprite=\"CurrencySpriteAsset\" name=\"Funds\" color=#B4D455>";
		internal const string InlineSpriteReputation = "<sprite=\"CurrencySpriteAsset\" name=\"Reputation\" color=#E0D503>";
		internal const string InlineSpriteFlask = "<sprite=\"CurrencySpriteAsset\" name=\"Flask\" color=#CE5DAE>";

		///<summary> Pretty-print a resource rate (rate is per second). Return an absolute value if a negative one is provided</summary>
		internal static string Rate(double rate, string precision = "F3", string unit = "")
		{
			if (rate == 0.0) return Local.Generic_NONE;//"none"
			rate = System.Math.Abs(rate);
			if (rate >= 0.01) return String.BuildString(rate.ToString(precision), unit, Local.Generic_perSecond);//"/s"
			rate *= 60.0; // per-minute
			if (rate >= 0.01) return String.BuildString(rate.ToString(precision), unit, Local.Generic_perMinute);//"/m"
			rate *= 60.0; // per-hour
			if (rate >= 0.01) return String.BuildString(rate.ToString(precision), unit, Local.Generic_perHour);//"/h"
			rate *= Time.HoursInDay;  // per-day
			if (rate >= 0.01) return String.BuildString(rate.ToString(precision), unit, Local.Generic_perDay);//"/d"
			return String.BuildString((rate * Time.DaysInYear).ToString(precision), unit, Local.Generic_perYear);//"/y"
		}

		///<summary> Pretty-print a duration (duration is in seconds, must be positive) </summary>
		internal static string Duration(double d, bool fullprecison = false)
		{
			if (!fullprecison)
			{
				if (double.IsInfinity(d) || double.IsNaN(d)) return Local.Generic_PERPETUAL;//"perpetual"
				d = System.Math.Round(d);
				if (d <= 0.0) return Local.Generic_NONE;//"none"

				ulong hours_in_day = (ulong)Time.HoursInDay;
				ulong days_in_year = (ulong)Time.DaysInYear;
				ulong duration_seconds = (ulong)d;

				// seconds
				if (d < 60.0)
				{
					ulong seconds = duration_seconds % 60ul;
					return String.BuildString(seconds.ToString(), "s");
				}
				// minutes + seconds
				if (d < 3600.0)
				{
					ulong seconds = duration_seconds % 60ul;
					ulong minutes = (duration_seconds / 60ul) % 60ul;
					return String.BuildString(minutes.ToString(), "m ", seconds.ToString("00"), "s");
				}
				// hours + minutes
				if (d < 3600.0 * Time.HoursInDay)
				{
					ulong minutes = (duration_seconds / 60ul) % 60ul;
					ulong hours = (duration_seconds / 3600ul) % hours_in_day;
					return String.BuildString(hours.ToString(), "h ", minutes.ToString("00"), "m");
				}
				ulong days = (duration_seconds / (3600ul * hours_in_day)) % days_in_year;
				// days + hours
				if (d < 3600.0 * Time.HoursInDay * Time.DaysInYear)
				{
					ulong hours = (duration_seconds / 3600ul) % hours_in_day;
					return String.BuildString(days.ToString(), "d ", hours.ToString(), "h");
				}
				// years + days
				ulong years = duration_seconds / (3600ul * hours_in_day * days_in_year);
				return String.BuildString(years.ToString(), "y ", days.ToString(), "d");
			}
			else
			{
				if (double.IsInfinity(d) || double.IsNaN(d)) return Local.Generic_NEVER;//"never"
				d = System.Math.Round(d);
				if (d <= 0.0) return Local.Generic_NONE;//"none"

				double hours_in_day = Time.HoursInDay;
				double days_in_year = Time.DaysInYear;

				long duration = (long)d;
				long seconds = duration % 60;
				duration /= 60;
				long minutes = duration % 60;
				duration /= 60;
				long hours = duration % (long)hours_in_day;
				duration /= (long)hours_in_day;
				long days = duration % (long)days_in_year;
				long years = duration / (long)days_in_year;

				string result = string.Empty;
				if (years > 0) result += years + "y ";
				if (years > 0 || days > 0) result += days + "d ";
				if (years > 0 || days > 0 || hours > 0) result += hours.ToString("D2") + ":";
				if (years > 0 || days > 0 || hours > 0 || minutes > 0) result += minutes.ToString("D2") + ":";
				result += seconds.ToString("D2");

				return result;
			}
		}

		internal static string Countdown(double duration, bool compact = false)
		{
			return String.BuildString("T-", Duration(duration, !compact));
		}

		///<summary> Pretty-print a range (range is in meters) </summary>
		internal static string Distance(double distance)
		{
			if (distance == 0.0) return Local.Generic_NONE;//"none"
			if (distance < 0.0) return String.BuildString("-", Distance(-distance));
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " m");
			distance /= 1000.0;
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " Km");
			distance /= 1000.0;
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " Mm");
			distance /= 1000.0;
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " Gm");
			distance /= 1000.0;
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " Tm");
			distance /= 1000.0;
			if (distance < 1000.0) return String.BuildString(distance.ToString("F1"), " Pm");
			distance /= 1000.0;
			return String.BuildString(distance.ToString("F1"), " Em");
		}

		///<summary> Pretty-print a speed (in meters/sec) </summary>
		internal static string Speed(double speed)
		{
			return String.BuildString(Distance(speed), "/s");
		}

		///<summary> Pretty-print temperature </summary>
		internal static string Temp(double temp)
		{
			return String.BuildString(temp.ToString("F1"), " K");
		}

		///<summary> Pretty-print angle </summary>
		internal static string Angle(double angle)
		{
			return String.BuildString(angle >= 0.0001 ? angle.ToString("F1") : "0", " °");
		}

		///<summary> Pretty-print flux </summary>
		internal static string Flux(double flux)
		{
			if (Settings.UseSIUnits)
				return SI.SIFlux(flux);

			return String.BuildString(flux >= 0.0001 ? flux.ToString("F1") : flux.ToString(), " W/m²");
		}

		///<summary> Pretty-print magnetic strength </summary>
		internal static string Field(double strength)
		{
			if (Settings.UseSIUnits)
				return SI.SIField(strength);

			return String.BuildString(strength.ToString("F1"), " uT"); //< micro-tesla
		}

		///<summary> Pretty-print radiation rate </summary>
		internal static string Radiation(double rad, bool nominal = true)
		{
			if (Settings.UseSIUnits)
				return SI.SIRadiation(rad, nominal);

			if (nominal && rad <= KERBALISM.Radiation.Nominal) return Local.Generic_NOMINAL;//"nominal"

			rad *= 3600.0;
			var unit = "rad/h";
			var prefix = "";

			if (Settings.RadiationInSievert)
			{
				rad /= 100.0;
				unit = "Sv/h";
			}

			if (rad < 0.00001)
			{
				rad *= 1000000;
				prefix = "μ";
			}
			else if (rad < 0.01)
			{
				rad *= 1000;
				prefix = "m";
			}

			return String.BuildString((rad).ToString("F3"), " ", prefix, unit);
		}

		///<summary> Pretty-print percentage </summary>
		internal static string Percentage(double v, string format = "F0")
		{
			return String.BuildString((v * 100.0).ToString(format), "%");
		}

		///<summary> Pretty-print pressure (value is in kPa) </summary>
		internal static string Pressure(double v)
		{
			if (Settings.UseSIUnits)
				return SI.SIPressure(v);

			return String.BuildString(v.ToString("F1"), " kPa");
		}

		///<summary> Pretty-print volume (value is in m^3) </summary>
		internal static string Volume(double v)
		{
			return String.BuildString(v.ToString("F2"), " m³");
		}

		///<summary> Pretty-print surface (value is in m^2) </summary>
		internal static string Surface(double v)
		{
			return String.BuildString(v.ToString("F2"), " m²");
		}

		///<summary> Pretty-print mass </summary>
		internal static string Mass(double v)
		{
			if (v <= double.Epsilon) return "0 kg";
			if (v > 1) return String.BuildString(v.ToString("F3"), " t");
			v *= 1000;
			if (v > 1) return String.BuildString(v.ToString("F2"), " kg");
			v *= 1000;
			return String.BuildString(v.ToString("F2"), " g");
		}

		///<summary> Pretty-print cost </summary>
		internal static string Cost(double v)
		{
			return String.BuildString(v.ToString("F0"), " $");
		}

		///<summary> Format a value to 2 decimal places, or return 'none' </summary>
		internal static string Amount(double value, string append = "")
		{
			return (System.Math.Abs(value) <= double.Epsilon ? Local.Generic_NONE : String.BuildString(value.ToString("F2"), append));//"none"
		}

		///<summary> Format an integer value, or return 'none' </summary>
		internal static string Integer(uint value, string append = "")
		{
			return (System.Math.Abs(value) <= 0 ? Local.Generic_NONE : String.BuildString(value.ToString("F0"), append));//"none"
		}
		// Note : config / code base unit for data rate / size is in megabyte (1000^2 bytes)
		// For UI purposes we use the decimal units (B/kB/MB...), not the binary (1024^2 bytes) units

		internal const double bitsPerMB = 1000.0 * 1000.0 * 8.0;

		const double bPerMB = 1000.0 * 1000.0 * 8;
		const double BPerMB = 1000.0 * 1000.0;
		const double kBPerMB = 1000.0;
		const double GBPerMB = 1.0 / 1000.0;
		const double TBPerMB = 1.0 / (1000.0 * 1000.0);

		const double MBPerBitTenth = 1.0 / (1000.0 * 1000.0 * 10.0 * 8.0);
		const double MBPerB = 1.0 / (1000.0 * 1000.0);
		const double MBPerkB = 1.0 / 1000.0;
		const double MBPerGB = 1000.0;
		const double MBPerTB = 1000.0 * 1000.0;

		///<summary> Format data size, the size parameter is in MB (megabytes) </summary>
		internal static string DataSize(double size)
		{
			if (size < MBPerBitTenth)  // min size is 0.1 bit
				return Local.Generic_NONE;//"none"
			if (size < MBPerB)
				return (size * bPerMB).ToString("0.0 b");
			if (size < MBPerkB)
				return (size * BPerMB).ToString("0.0 B");
			if (size < 1.0)
				return (size * kBPerMB).ToString("0.00 kB");
			if (size < MBPerGB)
				return size.ToString("0.00 MB");
			if (size < MBPerTB)
				return (size * GBPerMB).ToString("0.00 GB");

			return (size * TBPerMB).ToString("0.00 TB");
		}

		///<summary> Format data rate, the rate parameter is in MB/s </summary>
		internal static string DataRate(double rate)
		{
			if (rate < MBPerBitTenth)  // min rate is 0.1 bit/s
				return Local.Generic_NONE;//"none"
			if (rate < MBPerB)
				return (rate * bPerMB).ToString("0.0 b/s");
			if (rate < MBPerkB)
				return (rate * BPerMB).ToString("0.0 B/s");
			if (rate < 1.0)
				return (rate * kBPerMB).ToString("0.00 kB/s");
			if (rate < MBPerGB)
				return rate.ToString("0.00 MB/s");
			if (rate < MBPerTB)
				return (rate * GBPerMB).ToString("0.00 GB/s");

			return (rate * TBPerMB).ToString("0.00 TB/s");
		}

		internal static string SampleSize(double size)
		{
			return SampleSize(SampleSizeToSlots(size));
		}

		internal static string SampleSize(int slots)
		{
			if (slots <= 0) return String.BuildString(Local.Generic_NO, Local.Generic_SLOT);//"no "

			return String.BuildString(slots.ToString(), " ", slots > 1 ? Local.Generic_SLOTS : Local.Generic_SLOT);
		}

		internal static int SampleSizeToSlots(double size)
		{
			int result = (int)(size / 1024);
			if (result * 1024 < size) ++result;
			return result;
		}

		internal static double SlotsToSampleSize(int slots)
		{
			return slots * 1024;
		}

		///<summary> Format science credits </summary>
		internal static string Science(double value, bool compact = true)
		{
			if (compact)
				return String.Color(value.ToString("F1"), String.Kolor.Science, true);
			else
				return String.Color(String.BuildString(value.ToString("F1"), " ", Local.SCIENCEARCHIVE_CREDITS), String.Kolor.Science);//CREDITS
		}
	}
}
