namespace KERBALISM
{
	static class Random
	{
		// store the random number generator
		static System.Random rng = new System.Random();

		///<summary>return random integer</summary>
		internal static int RandomInt(int max_value)
		{
			return rng.Next(max_value);
		}

		///<summary>return random float [0..1]</summary>
		internal static float RandomFloat()
		{
			return (float)rng.NextDouble();
		}

		///<summary>return random double [0..1]</summary>
		internal static double RandomDouble()
		{
			return rng.NextDouble();
		}


		static int fast_float_seed = 1;
		/// <summary>
		/// return random float in [-1,+1] range
		/// - it is less random than the c# RNG, but is way faster
		/// - the seed is meant to overflow! (turn off arithmetic overflow/underflow exceptions)
		/// </summary>
		internal static float FastRandomFloat()
		{
			fast_float_seed *= 16807;
			return fast_float_seed * 4.6566129e-010f;
		}
	}
}
