using System.Diagnostics;

namespace KERBALISM
{
	static class Time
	{
		static double hoursInDay = -1.0;
		///<summary>return hours in a day</summary>
		internal static double HoursInDay
		{
			get
			{
				if (!GameSettings.KERBIN_TIME)
					return 24.0;

				if (hoursInDay == -1.0)
				{
					if (FlightGlobals.ready || GameLogic.IsEditor())
					{
						var homeBody = FlightGlobals.GetHomeBody();
						hoursInDay = System.Math.Round(homeBody.rotationPeriod / 3600, 0);
					}
					else
					{
						return 6.0;
					}

				}
				return hoursInDay;
			}
		}

		static double daysInYear = -1.0;
		///<summary>return year length</summary>
		internal static double DaysInYear
		{
			get
			{
				if (!GameSettings.KERBIN_TIME)
					return 365.0;

				if (daysInYear == -1.0)
				{
					if (FlightGlobals.ready || GameLogic.IsEditor())
					{
						var homeBody = FlightGlobals.GetHomeBody();
						daysInYear = System.Math.Floor(homeBody.orbit.period / (HoursInDay * 60.0 * 60.0));
					}
					else
					{
						return 426.0;
					}
				}
				return daysInYear;
			}
		}


		///<summary>stop time warping</summary>
		internal static void StopWarp(double maxSpeed = 0)
		{
			var warp = TimeWarp.fetch;
			warp.CancelAutoWarp();
			int maxRate = 0;
			for (int i = 0; i < warp.warpRates.Length; ++i)
			{
				if (warp.warpRates[i] < maxSpeed)
					maxRate = i;
			}
			TimeWarp.SetRate(maxRate, true, false);
		}

		///<summary>disable time warping above a specified level</summary>
		internal static void DisableWarp(uint max_level)
		{
			for (uint i = max_level + 1u; i < 8; ++i)
			{
				TimeWarp.fetch.warpRates[i] = TimeWarp.fetch.warpRates[max_level];
			}
		}

		///<summary>get current time</summary>
		internal static ulong Clocks()
		{
			return (ulong)Stopwatch.GetTimestamp();
		}

		///<summary>convert from clocks to microseconds</summary>
		internal static double Microseconds(ulong clocks)
		{
			return clocks * 1000000.0 / Stopwatch.Frequency;
		}


		internal static double Milliseconds(ulong clocks)
		{
			return clocks * 1000.0 / Stopwatch.Frequency;
		}


		internal static double Seconds(ulong clocks)
		{
			return clocks / (double)Stopwatch.Frequency;
		}

		///<summary>return human-readable timestamp of planetarium time</summary>
		internal static string PlanetariumTimestamp()
		{
			double t = Planetarium.GetUniversalTime();
			const double len_min = 60.0;
			const double len_hour = len_min * 60.0;
			double len_day = len_hour * HoursInDay;
			double len_year = len_day * DaysInYear;

			double year = System.Math.Floor(t / len_year);
			t -= year * len_year;
			double day = System.Math.Floor(t / len_day);
			t -= day * len_day;
			double hour = System.Math.Floor(t / len_hour);
			t -= hour * len_hour;
			double min = System.Math.Floor(t / len_min);

			return String.BuildString
			(
			  "[",
			  ((uint)year + 1).ToString("D4"),
			  "/",
			  ((uint)day + 1).ToString("D2"),
			  " ",
			  ((uint)hour).ToString("D2"),
			  ":",
			  ((uint)min).ToString("D2"),
			  "]"
			);
		}

		///<summary>return true half the time</summary>
		internal static int Alternate(int seconds, int elements)
		{
			return ((int)UnityEngine.Time.realtimeSinceStartup / seconds) % elements;
		}
	}
}
