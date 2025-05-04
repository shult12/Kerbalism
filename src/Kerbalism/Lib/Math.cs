namespace KERBALISM
{
	static class Math
	{
		///<summary>clamp a value</summary>
		internal static int Clamp(int value, int min, int max)
		{
			return System.Math.Max(min, System.Math.Min(value, max));
		}

		///<summary>clamp a value</summary>
		internal static float Clamp(float value, float min, float max)
		{
			return System.Math.Max(min, System.Math.Min(value, max));
		}

		///<summary>clamp a value</summary>
		internal static double Clamp(double value, double min, double max)
		{
			return System.Math.Max(min, System.Math.Min(value, max));
		}

		///<summary>blend between two values</summary>
		internal static float Mix(float a, float b, float k)
		{
			return a * (1.0f - k) + b * k;
		}

		///<summary>blend between two values</summary>
		internal static double Mix(double a, double b, double k)
		{
			return a * (1.0 - k) + b * k;
		}
	}
}
