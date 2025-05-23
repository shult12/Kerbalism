using System;
using System.Collections.Generic;
using System.Text;

namespace KERBALISM
{
	/// <summary>
	/// Stores information about an experiment_id or a subject_id
	/// Beware that subject information will be incomplete until the stock `ScienceSubject` is created in RnD
	/// </summary>
	sealed class ExperimentInfo
	{
		internal static StringBuilder ExpInfoSB = new StringBuilder();

		/// <summary> experiment definition </summary>
		ScienceExperiment stockDef;

		/// <summary> experiment identifier </summary>
		internal string ExperimentId { get; private set; }

		/// <summary> UI friendly name of the experiment </summary>
		internal string Title { get; private set; }

		/// <summary> mass of a full sample </summary>
		internal double SampleMass { get; private set; }

		internal BodyConditions ExpBodyConditions { get; private set; }

		/// <summary> size of a full file or sample</summary>
		internal double DataSize { get; private set; }

		internal bool IsSample { get; private set; }

		internal double MassPerMB { get; private set; }

		internal double DataScale => stockDef.dataScale;

		/// <summary> situation mask </summary>
		internal uint SituationMask { get; private set; }

		/// <summary> stock ScienceExperiment situation mask </summary>
		uint StockSituationMask => stockDef.situationMask;

		/// <summary> biome mask </summary>
		internal uint BiomeMask { get; private set; }

		/// <summary> stock ScienceExperiment biome mask </summary>
		uint StockBiomeMask => stockDef.biomeMask;

		/// <summary> virtual biomes mask </summary>
		internal uint VirtualBiomeMask { get; private set; }

		internal List<VirtualBiome> VirtualBiomes { get; private set; } = new List<VirtualBiome>();

		internal double ScienceCap => stockDef.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

		/// <summary> Cache the information returned by GetInfo() in the first found module using that experiment</summary>
		internal string ModuleInfo { get; private set; } = string.Empty;

		/// <summary> If true, subject completion will enable the stock resource map for the corresponding body</summary>
		internal bool UnlockResourceSurvey { get; private set; }

		internal bool IsROC { get; private set; }

		internal bool HasDBSubjects { get; private set; }

		internal bool IgnoreBodyRestrictions { get; private set; }

		/// <summary> List of experiments that will be collected automatically alongside this one</summary>
		internal List<ExperimentInfo> IncludedExperiments { get; private set; } = new List<ExperimentInfo>();

		string[] includedExperimentsId;

		internal ExperimentInfo(ScienceExperiment stockDef, ConfigNode expInfoNode)
		{
			// if we have a custom "KERBALISM_EXPERIMENT" definition for the experiment, load it, else just use an empty node to avoid nullrefs
			if (expInfoNode == null) expInfoNode = new ConfigNode();

			this.stockDef = stockDef;
			ExperimentId = stockDef.id;

			// We have some custom handling for breaking ground ROC experiments
			IsROC = ExperimentId.StartsWith("ROCScience");

			if (IsROC)
				Title = "ROC: " + stockDef.experimentTitle;	// group ROC together in the science archive (sorted by Title)
			else
				Title = stockDef.experimentTitle;

			// A new bool field was added in 1.7 for serenity : applyScienceScale
			// if not specified, the default is `true`, which is the case for all non-serenity science defs
			// serenity ground experiments and ROCs have applyScienceScale = false.
			// for ground experiment, baseValue = science generated per hour
			// for ROC experiments, it doesn't change anything because they are all configured with baseValue = scienceCap
			if (this.stockDef.applyScienceScale)
				DataSize = this.stockDef.baseValue * this.stockDef.dataScale;
			else
				DataSize = this.stockDef.scienceCap * this.stockDef.dataScale;

			// load the included experiments ids in a string array, we will populate the list after 
			// all ExperimentInfos are created. (can't do it here as they may not exist yet)
			includedExperimentsId = expInfoNode.GetValues("IncludeExperiment");

			UnlockResourceSurvey = Lib.ConfigValue(expInfoNode, "UnlockResourceSurvey", false);
			SampleMass = Lib.ConfigValue(expInfoNode, "SampleMass", 0.0);
			IsSample = SampleMass > 0.0;
			if (IsSample)
			{
				// make sure we don't produce NaN values down the line because of odd/wrong configs
				if (DataSize <= 0.0)
				{
					Logging.Log(ExperimentId + " has DataSize=" + DataSize + ", your configuration is broken!", Logging.LogLevel.Warning);
					DataSize = 1.0;
				}
				MassPerMB = SampleMass / DataSize;
			}
			else
			{
				MassPerMB = 0.0;
			}

			// Patch stock science def restrictions as BodyAllowed/BodyNotAllowed restrictions
			if (!(expInfoNode.HasValue("BodyAllowed") || expInfoNode.HasValue("BodyNotAllowed")))
			{
				if (IsROC)
				{
					// Parse the ROC definition name to find which body it's available on
					// This rely on the ROC definitions having the body name in the ExperimentId
					foreach (CelestialBody body in FlightGlobals.Bodies)
					{
						if (ExperimentId.IndexOf(body.name, StringComparison.OrdinalIgnoreCase) != -1)
						{
							expInfoNode.AddValue("BodyAllowed", body.name);
							break;
						}
					}
				}

				// parse the stock atmosphere restrictions into our own
				if (stockDef.requireAtmosphere)
					expInfoNode.AddValue("BodyAllowed", "Atmospheric");
				else if (stockDef.requireNoAtmosphere)
					expInfoNode.AddValue("BodyNotAllowed", "Atmospheric");
			}

			ExpBodyConditions = new BodyConditions(expInfoNode);

			foreach (string virtualBiomeStr in expInfoNode.GetValues("VirtualBiome"))
			{
				if (Enum.IsDefined(typeof(VirtualBiome), virtualBiomeStr))
				{
					VirtualBiomes.Add((VirtualBiome)Enum.Parse(typeof(VirtualBiome), virtualBiomeStr));
				}
				else
				{
					Logging.Log("Experiment definition `{0}` has unknown VirtualBiome={1}", Logging.LogLevel.Warning, ExperimentId, virtualBiomeStr);
				}
			}

			IgnoreBodyRestrictions = Lib.ConfigValue(expInfoNode, "IgnoreBodyRestrictions", false);

			uint situationMask = 0;
			uint biomeMask = 0;
			uint virtualBiomeMask = 0;
			// if defined, override stock situation / biome mask
			if (expInfoNode.HasValue("Situation"))
			{
				foreach (string situation in expInfoNode.GetValues("Situation"))
				{
					string[] sitAtBiome = situation.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
					if (sitAtBiome.Length == 0 || sitAtBiome.Length > 2)
						continue;

					ScienceSituation scienceSituation = ScienceSituationUtils.ScienceSituationDeserialize(sitAtBiome[0]);

					if (scienceSituation != ScienceSituation.None)
					{
						situationMask += scienceSituation.BitValue();

						if (sitAtBiome.Length == 2)
						{
							if (sitAtBiome[1].Equals("Biomes", StringComparison.OrdinalIgnoreCase))
							{
								biomeMask += scienceSituation.BitValue();
							}
							else if (sitAtBiome[1].Equals("VirtualBiomes", StringComparison.OrdinalIgnoreCase) && VirtualBiomes.Count > 0)
							{
								virtualBiomeMask += scienceSituation.BitValue();
							}
						}
					}
					else
					{
						Logging.Log("Experiment definition `{0}` has unknown situation : `{1}`", Logging.LogLevel.Warning, ExperimentId, sitAtBiome[0]);
					}
				}
			}
			else
			{
				situationMask = stockDef.situationMask;
				biomeMask = stockDef.biomeMask;
			}

			if (situationMask == 0)
			{
				Logging.Log("Experiment definition `{0}` : `0` situationMask is unsupported, patching to `BodyGlobal`", Logging.LogLevel.Message, ExperimentId);
				situationMask = ScienceSituation.BodyGlobal.BitValue();
				HasDBSubjects = false;
			}
			else
			{
				HasDBSubjects = !Lib.ConfigValue(expInfoNode, "IsGeneratingSubjects", false);
			}

			string error;
			uint stockSituationMask;
			uint stockBiomeMask;
			if (!ScienceSituationUtils.ValidateSituationBitMask(ref situationMask, biomeMask, out stockSituationMask, out stockBiomeMask, out error))
			{
				Logging.Log("Experiment definition `{0}` is incorrect :\n{1}", Logging.LogLevel.Error, ExperimentId, error);
			}

			SituationMask = situationMask;
			BiomeMask = biomeMask;
			VirtualBiomeMask = virtualBiomeMask;
			stockDef.situationMask = stockSituationMask;
			stockDef.biomeMask = stockBiomeMask;
		}

		internal void ParseIncludedExperiments()
		{
			foreach (string expId in includedExperimentsId)
			{
				ExperimentInfo includedInfo = ScienceDB.GetExperimentInfo(expId);
				if (includedInfo == null)
				{
					Logging.Log($"Experiment `{ExperimentId}` define a IncludedExperiment `{expId}`, but that experiment doesn't exist", Logging.LogLevel.Warning);
					continue;
				}
					
				// early prevent duplicated entries
				if (includedInfo.ExperimentId == ExperimentId || IncludedExperiments.Contains(includedInfo))
					continue;

				IncludedExperiments.Add(includedInfo);
			}
		}

		internal static void CheckIncludedExperimentsRecursion(ExperimentInfo expInfoToCheck, List<ExperimentInfo> chainedExperiments)
		{
			List<ExperimentInfo> loopedExperiments = new List<ExperimentInfo>();
			foreach (ExperimentInfo includedExp in expInfoToCheck.IncludedExperiments)
			{
				if (chainedExperiments.Contains(includedExp))
				{
					loopedExperiments.Add(includedExp);
				}

				chainedExperiments.Add(includedExp);
			}

			foreach (ExperimentInfo loopedExperiment in loopedExperiments)
			{
				expInfoToCheck.IncludedExperiments.Remove(loopedExperiment);
				Logging.Log($"IncludedExperiment `{loopedExperiment.ExperimentId}` in experiment `{expInfoToCheck.ExperimentId}` would result in an infinite loop in the chain and has been removed", Logging.LogLevel.Warning);
			}

			foreach (ExperimentInfo includedExp in expInfoToCheck.IncludedExperiments)
			{
				CheckIncludedExperimentsRecursion(includedExp, chainedExperiments);
			}
		}

		internal static void GetIncludedExperimentTitles(ExperimentInfo expinfo, List<string> includedExperiments)
		{
			foreach (ExperimentInfo includedExpinfo in expinfo.IncludedExperiments)
			{
				includedExperiments.Add(includedExpinfo.Title);
				GetIncludedExperimentTitles(includedExpinfo, includedExperiments);
			}
			includedExperiments.Sort((x, y) => x.CompareTo(y));
		}

		/// <summary>
		/// parts that have experiments can't get their module info (what is shown in the VAB tooltip) correctly setup
		/// because the ExperimentInfo database isn't available at loading time, so we recompile their info manually.
		/// </summary>
		internal void CompileModuleInfos()
		{
			if (PartLoader.LoadedPartsList == null)
			{
				Logging.Log("Dazed and confused: PartLoader.LoadedPartsList == null");
				return;
			}

			foreach (AvailablePart ap in PartLoader.LoadedPartsList)
			{
				if (ap == null || ap.partPrefab == null)
				{
					Logging.Log("AvailablePart is null or without prefab: " + ap);
					continue;
				}

				foreach (PartModule module in ap.partPrefab.Modules)
				{
					if (module is Experiment expModule)
					{
						// don't show configurable experiments
						if (!expModule.isConfigurable && expModule.experiment_id == ExperimentId)
						{
							expModule.ExpInfo = this;

							// get module info for the ExperimentInfo, once
							if (string.IsNullOrEmpty(ModuleInfo))
							{
								ModuleInfo = String.Color(Title, String.Kolor.Cyan, true);
								ModuleInfo += "\n";
								ModuleInfo += expModule.GetInfo();
							}
						}
					}

					if (!string.IsNullOrEmpty(ModuleInfo))
						continue;

					if (module is ModuleScienceExperiment stockExpModule)
					{
						if (stockExpModule.experimentID == ExperimentId)
						{
							ModuleInfo = String.Color(Title, String.Kolor.Cyan, true);
							ModuleInfo += "\n"+Local.Experimentinfo_Datasize +": ";//Data size
							ModuleInfo += HumanReadable.DataSize(DataSize);
							if (stockExpModule.xmitDataScalar < Science.maxXmitDataScalarForSample)
							{
								ModuleInfo += "\n"+Local.Experimentinfo_generatesample;//Will generate a sample.
								ModuleInfo += "\n" + Local.Experimentinfo_Samplesize + " ";//Sample size:
								ModuleInfo += HumanReadable.SampleSize(DataSize);
							}
							ModuleInfo += "\n\n";
							ModuleInfo += String.Color(Local.Experimentinfo_Situations, String.Kolor.Cyan, true);//"Situations:\n"

							foreach (string s in AvailableSituations())
								ModuleInfo += String.BuildString("• <b>", s, "</b>\n");

							ModuleInfo += "\n";
							ModuleInfo += stockExpModule.GetInfo();
						}
					}
					else if (module is ModuleGroundExperiment groundExpModule)
					{
						if (groundExpModule.experimentId == ExperimentId)
						{
							ModuleInfo = String.Color(Title, String.Kolor.Cyan, true);
							ModuleInfo += "\n" + Local.Experimentinfo_Datasize + ": ";//Data size
							ModuleInfo += HumanReadable.DataSize(DataSize);
							ModuleInfo += "\n\n";
							ModuleInfo += groundExpModule.GetInfo();
						}
					}
				}

				// special cases
				if (ExperimentId == "asteroidSample" || ExperimentId.StartsWith("cometSample_", StringComparison.Ordinal))
				{
					ModuleInfo = Local.Experimentinfo_Asteroid;//"Asteroid samples can be taken by kerbals on EVA"
					ModuleInfo += "\n"+Local.Experimentinfo_Samplesize +" ";//Sample size:
					ModuleInfo += HumanReadable.SampleSize(DataSize);
					ModuleInfo += "\n"+Local.Experimentinfo_Samplemass +" ";//Sample mass:
					ModuleInfo += HumanReadable.Mass(DataSize * Settings.AsteroidSampleMassPerMB);
				}
				else if (IsROC)
				{
					// This is a failsafe in case :
					// - a mod add a ROC definition but breaking grounds isn't installed
					// - an intermittent bug causing ROCManager.Instance to be null (seems caused by Kopernicus, see https://github.com/Kopernicus/Kopernicus/issues/499) 
					if (ROCManager.Instance == null)
					{
						Logging.Log($"Can't parse ModuleInfo for {ExperimentId} on part={ap.name} : ROCManager is null", Logging.LogLevel.Warning);
						continue;
					}

					string rocType = ExperimentId.Substring(ExperimentId.IndexOf('_') + 1);
					ROCDefinition rocDef = ROCManager.Instance.rocDefinitions.Find(p => p.type == rocType);
					if (rocDef != null)
					{
						ModuleInfo = String.Color(rocDef.displayName, String.Kolor.Cyan, true);
						ModuleInfo += "\n- " + Local.Experimentinfo_scannerarm;//Analyse with a scanner arm
						ModuleInfo += "\n  "+Local.Experimentinfo_Datasize +": ";//Data size
						ModuleInfo += HumanReadable.DataSize(DataSize);

						if (rocDef.smallRoc)
						{
							ModuleInfo += "\n- " + Local.Experimentinfo_smallRoc;//Collectable on EVA as a sample"
							ModuleInfo += "\n"+Local.Experimentinfo_Samplesize +" ";//Sample size:
							ModuleInfo += HumanReadable.SampleSize(DataSize);
						}
						else
						{
							ModuleInfo += "\n- "+Local.Experimentinfo_smallRoc2;//Can't be collected on EVA
						}

						foreach (RocCBDefinition body in rocDef.myCelestialBodies)
						{
							CelestialBody Body = FlightGlobals.GetBodyByName(body.name);
							ModuleInfo += String.Color("\n\n" + Local.Experimentinfo_smallRoc3.Format(Body.displayName.LocalizeRemoveGender()), String.Kolor.Cyan, true);//"Found on <<1>>'s :"
							foreach (string biome in body.biomes)
							{
								ModuleInfo += "\n- ";
								ModuleInfo += ScienceUtil.GetBiomedisplayName(Body, biome); // biome;
							}
						}
					}
				}
			}
		}

		/// <summary> UI friendly list of situations available for the experiment</summary>
		internal List<string> AvailableSituations()
		{
			List<string> result = new List<string>();

			foreach (ScienceSituation situation in ScienceSituationUtils.validSituations)
			{
				if (situation.IsAvailableForExperiment(this))
				{
					if (situation.IsBodyBiomesRelevantForExperiment(this))
					{
						result.Add(String.BuildString(situation.Title(), " ", Local.Situation_biomes));//(biomes)"
					}
					else if (situation.IsVirtualBiomesRelevantForExperiment(this))
					{
						foreach (VirtualBiome biome in VirtualBiomes)
						{
							result.Add(String.BuildString(situation.Title(), " (", biome.Title(),")"));
						}
					}
					else
					{
						result.Add(situation.Title());
					}
				}
			}

			return result;
		}

		internal class BodyConditions
		{
			static string typeNamePlus = typeof(BodyConditions).FullName + "+";

			internal bool HasConditions { get; private set; }
			List<BodyCondition> bodiesAllowed = new List<BodyCondition>();
			List<BodyCondition> bodiesNotAllowed = new List<BodyCondition>();

			internal BodyConditions(ConfigNode node)
			{
				foreach (string allowed in node.GetValues("BodyAllowed"))
				{
					BodyCondition bodyCondition = ParseCondition(allowed);
					if (bodyCondition != null)
						bodiesAllowed.Add(bodyCondition);
				}

				foreach (string notAllowed in node.GetValues("BodyNotAllowed"))
				{
					BodyCondition bodyCondition = ParseCondition(notAllowed);
					if (bodyCondition != null)
						bodiesNotAllowed.Add(bodyCondition);
				}

				HasConditions = bodiesAllowed.Count > 0 || bodiesNotAllowed.Count > 0;
			}

			BodyCondition ParseCondition(string condition)
			{
				Type type = Type.GetType(typeNamePlus + condition);
				if (type != null)
				{
					return (BodyCondition)Activator.CreateInstance(type);
				}
				else
				{
					foreach (CelestialBody body in FlightGlobals.Bodies)
						if (body.name.Equals(condition, StringComparison.OrdinalIgnoreCase))
							return new SpecificBody(body.name);
				}
				Logging.Log("Invalid BodyCondition : '" + condition + "' defined in KERBALISM_EXPERIMENT node.");
				return null;
			}

			internal bool IsBodyAllowed(CelestialBody body)
			{
				bool isAllowed;

				if (bodiesAllowed.Count > 0)
				{
					isAllowed = false;
					foreach (BodyCondition bodyCondition in bodiesAllowed)
						isAllowed |= bodyCondition.TestCondition(body);
				}
				else
				{
					isAllowed = true;
				}

				foreach (BodyCondition bodyCondition in bodiesNotAllowed)
					isAllowed &= !bodyCondition.TestCondition(body);

				return isAllowed;
			}

			internal string ConditionsToString()
			{
				ExpInfoSB.Length = 0;

				if (bodiesAllowed.Count > 0)
				{
					ExpInfoSB.Append(String.Color(Local.Experimentinfo_Bodiesallowed + "\n", String.Kolor.Cyan, true));//Bodies allowed:
					for (int i = bodiesAllowed.Count - 1; i >= 0; i--)
					{
						ExpInfoSB.Append(bodiesAllowed[i].Title);
						if (i > 0) ExpInfoSB.Append(", ");
					}

					if (bodiesNotAllowed.Count > 0)
						ExpInfoSB.Append("\n");
				}

				if (bodiesNotAllowed.Count > 0)
				{
					ExpInfoSB.Append(String.Color(Local.Experimentinfo_Bodiesnotallowed + "\n", String.Kolor.Cyan, true));//Bodies not allowed:
					for (int i = bodiesNotAllowed.Count - 1; i >= 0; i--)
					{
						ExpInfoSB.Append(bodiesNotAllowed[i].Title);
						if (i > 0) ExpInfoSB.Append(", ");
					}
				}

				return ExpInfoSB.ToString();
			}

			abstract class BodyCondition
			{
				internal abstract bool TestCondition(CelestialBody body);
				internal abstract string Title { get; }
			}

			class Atmospheric : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => body.atmosphere;
				internal override string Title => Local.Experimentinfo_BodyCondition1;//"atmospheric"
			}

			class NonAtmospheric : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => !body.atmosphere;
				internal override string Title => Local.Experimentinfo_BodyCondition2;//"non-atmospheric"
			}

			class Gaseous : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => !body.hasSolidSurface;
				internal override string Title => Local.Experimentinfo_BodyCondition3;//"gaseous"
			}

			class Solid : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => !body.hasSolidSurface;
				internal override string Title => Local.Experimentinfo_BodyCondition4;//"solid"
			}

			class Oceanic : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => body.ocean;
				internal override string Title => Local.Experimentinfo_BodyCondition5;//"oceanic"
			}

			class HomeBody : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => body.isHomeWorld;
				internal override string Title => Local.Experimentinfo_BodyCondition6;//"home body"
			}

			class HomeBodyAndMoons : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => body.isHomeWorld || body.referenceBody.isHomeWorld;
				internal override string Title => Local.Experimentinfo_BodyCondition7;//"home body and its moons"
			}

			class Planets : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => !Lib.IsSun(body) && Lib.IsSun(body.referenceBody);
				internal override string Title => Local.Experimentinfo_BodyCondition8;//"planets"
			}

			class Moons : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => !Lib.IsSun(body) && !Lib.IsSun(body.referenceBody);
				internal override string Title => Local.Experimentinfo_BodyCondition9;//"moons"
			}

			class Suns : BodyCondition
			{
				internal override bool TestCondition(CelestialBody body) => Lib.IsSun(body);
				internal override string Title => Local.Experimentinfo_BodyCondition10;//"suns"
			}

			class SpecificBody : BodyCondition
			{
				string bodyName;
				internal override bool TestCondition(CelestialBody body) => body.name == bodyName;
				internal override string Title => string.Empty;
				internal SpecificBody(string bodyName) { this.bodyName = bodyName; }
			}
		}
	}
} // KERBALISM

