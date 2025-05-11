using System;

namespace KERBALISM
{
	/// <summary>
	/// Stores a body, science situation and biome combination, intended as a replacement for
	/// the second part (after the "@") of the stock string-based subject id.
	/// generate an unique int number to be used as a key in one of the scienceDB dictionaries
	/// </summary>
	class Situation : IEquatable<Situation>
	{
		const int agnosticBiomeIndex = byte.MaxValue;

		internal CelestialBody Body { get; private set; }
		internal ScienceSituation ScienceSituation { get; private set; }
		CBAttributeMapSO.MapAttribute Biome { get; set; }
		VirtualBiome VirtualBiome { get; set; }

		/// <summary>
		/// Store the situation fields as a unique 32-bit array (int) :
		/// - 16 first bits : body index (ushort)
		/// - 8 next bits : vessel situation (byte)
		/// - 8 last bits : biome index (byte)
		/// </summary>
		internal int Id { get; private set; }

		internal Situation(int bodyIndex, ScienceSituation situation, int biomeIndex = -1)
		{
			ScienceSituation = situation;
			Body = FlightGlobals.Bodies[bodyIndex];

			if (biomeIndex >= 0)
			{
				if (biomeIndex >= ScienceSituationUtils.minVirtualBiome)
				{
					VirtualBiome = (VirtualBiome)biomeIndex;
				}
				else if (Body.BiomeMap != null)
				{
					Biome = Body.BiomeMap.Attributes[biomeIndex];
				}
			}

			Id = FieldsToId(bodyIndex, situation, biomeIndex);
		}

		/// <summary> garanteed to be unique for each body/situation/biome combination</summary>
		public override int GetHashCode() => Id;

		public bool Equals(Situation other) => other != null ? Id == other.Id : false;

		public static bool operator == (Situation a, Situation b) { return a.Equals(b); }
		public static bool operator != (Situation a, Situation b) { return !a.Equals(b); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			Situation objAs = obj as Situation;

			if (objAs == null)
				return false;

			return Equals(objAs);
		}

		static int FieldsToId(int bodyIndex, ScienceSituation situation, int biomeIndex = -1)
		{
			if (biomeIndex < 0)
				biomeIndex = agnosticBiomeIndex;

			return ((byte)biomeIndex << 8 | (byte)situation) << 16 | (ushort)bodyIndex;
		}

		internal static void IdToFields(int id, out int bodyIndex, out int situation, out int biomeIndex)
		{
			biomeIndex = (byte)(id >> 24);
			situation = (byte)(id >> 16);
			bodyIndex = (ushort)id;
			if (biomeIndex == agnosticBiomeIndex) biomeIndex = -1;
		}

		internal static int GetBiomeAgnosticIdForExperiment(int situationId, ExperimentInfo expInfo)
		{
			ScienceSituation sit = (ScienceSituation)(byte)(situationId >> 16);
			if (!sit.IsBiomesRelevantForExperiment(expInfo))
			{
				return situationId | (agnosticBiomeIndex << 24);
			}
			return situationId;
		}

		internal int GetBiomeAgnosticId()
		{
			return Id | (agnosticBiomeIndex << 24);
		}

		Situation GetBiomeAgnosticSituation()
		{
			return new Situation((ushort)Id, (ScienceSituation)(byte)(Id >> 16));
		}

		string Title =>
			Biome != null
			? String.BuildString(BodyTitle, " ", ScienceSituationTitle, " ", BiomeTitle)
			: String.BuildString(BodyTitle, " ", ScienceSituationTitle);

		internal string BodyTitle => Body.name;
		internal string BiomeTitle => Biome != null ? Biome.displayname : VirtualBiome != VirtualBiome.None ? VirtualBiome.Title() : string.Empty;
		internal string ScienceSituationTitle => ScienceSituation.Title();

		string BodyName => Body.name;
		string BiomeName => Biome != null ? Biome.name.Replace(" ", string.Empty) : VirtualBiome != VirtualBiome.None ? VirtualBiome.Serialize() : string.Empty;
		string ScienceSituationName => ScienceSituation.Serialize();
		string StockScienceSituationName => ScienceSituation.ToValidStockSituation().Serialize();

		public override string ToString()
		{
			return String.BuildString(BodyName, ScienceSituationName, BiomeName);
		}

		internal double SituationMultiplier => ScienceSituation.BodyMultiplier(Body);

		internal string GetTitleForExperiment(ExperimentInfo expInfo)
		{
			if (ScienceSituation.IsBiomesRelevantForExperiment(expInfo))
				return String.BuildString(BodyTitle, " ", ScienceSituationTitle, " ", BiomeTitle);
			else
				return String.BuildString(BodyTitle, " ", ScienceSituationTitle);
		}

		internal string GetStockIdForExperiment(ExperimentInfo expInfo)
		{
			if (ScienceSituation.IsBiomesRelevantForExperiment(expInfo))
				return String.BuildString(BodyName, StockScienceSituationName, BiomeName);
			else
				return String.BuildString(BodyName, StockScienceSituationName);
		}

		internal bool AtmosphericFlight()
		{
			switch (ScienceSituation)
			{
				case ScienceSituation.FlyingLow:
				case ScienceSituation.FlyingHigh:
				case ScienceSituation.Flying:
					return true;
				default:
					return false;
			}
		}
	}
}
