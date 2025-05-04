using System;

namespace KERBALISM
{
	static class Parse
	{
		internal static bool ToBool(string s, bool def_value = false)
		{
			bool v;
			return s != null && bool.TryParse(s, out v) ? v : def_value;
		}

		internal static uint ToUInt(string s, uint def_value = 0)
		{
			uint v;
			return s != null && uint.TryParse(s, out v) ? v : def_value;
		}

		static Guid ToGuid(string s)
		{
			return new Guid(s);
		}

		internal static float ToFloat(string s, float def_value = 0.0f)
		{
			float v;
			return s != null && float.TryParse(s, out v) ? v : def_value;
		}

		internal static double ToDouble(string s, double def_value = 0.0)
		{
			double v;
			return s != null && double.TryParse(s, out v) ? v : def_value;
		}

		static bool TryParseColor(string s, out UnityEngine.Color c)
		{
			string[] split = s.Replace(" ", String.Empty).Split(',');
			if (split.Length < 3)
			{
				c = new UnityEngine.Color(0, 0, 0);
				return false;
			}
			if (split.Length == 4)
			{
				c = new UnityEngine.Color(ToFloat(split[0], 0f), ToFloat(split[1], 0f), ToFloat(split[2], 0f), ToFloat(split[3], 1f));
				return true;
			}
			c = new UnityEngine.Color(ToFloat(split[0], 0f), ToFloat(split[1], 0f), ToFloat(split[2], 0f));
			return true;
		}

		static UnityEngine.Color ToColor(string s, UnityEngine.Color def_value)
		{
			UnityEngine.Color v;
			return s != null && TryParseColor(s, out v) ? v : def_value;
		}
	}
}
