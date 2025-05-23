using KSP.Localization;
using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class SubjectData
	{
		public ExperimentInfo ExpInfo { get; protected set; }

		public Situation Situation { get; protected set; }

		public List<SubjectData> IncludedSubjects { get; protected set; }

		/// <summary> [SERIALIZED] percentage [0;x] of science retrieved, can be > 1 if subject has been retrieved more than once</summary>
		public virtual double PercentRetrieved { get; protected set; }

		/// <summary> how many times the subject has been fully retrieved in RnD </summary>
		public virtual int TimesCompleted { get; protected set; }

		internal bool ExistsInRnD => RnDSubject != null;

		public ScienceSubject RnDSubject { get; protected set; }

		/// <summary> our int-based identifier </summary>
		public virtual string Id { get; protected set; }

		/// <summary> stock subject identifier ("experimentId@situation") </summary>
		public virtual string StockSubjectId { get; protected set; }

		/// <summary> full description of the subject </summary>
		internal virtual string FullTitle => String.BuildString(ExpInfo.Title, " (", SituationTitle, ")");

		internal virtual string ExperimentTitle => ExpInfo.Title;

		internal virtual string SituationTitle => Situation.GetTitleForExperiment(ExpInfo);

		internal virtual string BiomeTitle => Situation.BiomeTitle;

		/// <summary> science points collected in all vessels but not yet recovered or transmitted </summary>
		public double ScienceCollectedInFlight { get; protected set; }

		/// <summary> total science value of the subject.  </summary>
		internal double ScienceMaxValue => ExpInfo.ScienceCap * Situation.SituationMultiplier;

		internal double SciencePerMB => ScienceMaxValue / ExpInfo.DataSize;

		/// <summary> science points recovered or transmitted </summary>
		// Note : this code is a bit convoluted to avoid "never completed" issues due to float <> double conversions
		internal double ScienceRetrievedInKSC => ExistsInRnD ? (RnDSubject.scienceCap - RnDSubject.science <= 0f) ? ScienceMaxValue : RnDSubject.science : 0.0;

		/// <summary> all science points recovered, transmitted or collected in flight </summary>
		internal double ScienceCollectedTotal => ScienceCollectedInFlight + ScienceRetrievedInKSC;

		/// <summary> science value remaining to collect. </summary>
		internal double ScienceRemainingToCollect => System.Math.Max(ScienceMaxValue - ScienceCollectedTotal, 0.0);

		/// <summary> science value remaining to retrieve. </summary>
		internal double ScienceRemainingToRetrieve => System.Math.Max(ScienceMaxValue - ScienceRetrievedInKSC, 0.0);

		/// <summary> science value remaining (accounting for retrieved in KSC and collected in flight) </summary>
		internal double ScienceRemainingTotal => System.Math.Max(ScienceMaxValue - ScienceCollectedTotal, 0.0);

		/// <summary> percentage [0;1] of science collected. </summary>
		internal double PercentCollectedTotal => ScienceMaxValue == 0.0 ? 0.0 : (ScienceCollectedInFlight / ScienceMaxValue) + PercentRetrieved;

		internal string DebugStateInfo => $"{FullTitle} :\nExistsInRnD={ExistsInRnD} - ScienceMaxValue={ScienceMaxValue} - SciencePerMB={SciencePerMB} - ScienceCollectedInFlight={ScienceCollectedInFlight} - RnDSubject.science={RnDSubject?.science}";

		/// <summary> science value for the given data size </summary>
		internal double ScienceValue(double dataSize, bool clampByScienceRetrieved = false, bool clampByScienceRetrievedAndCollected = false)
		{
			if (clampByScienceRetrievedAndCollected)
				return System.Math.Min(dataSize * SciencePerMB, ScienceRemainingToCollect);
			if (clampByScienceRetrieved)
				return System.Math.Min(dataSize * SciencePerMB, ScienceRemainingToRetrieve);

			return dataSize * SciencePerMB;
		}

		internal SubjectData(ExperimentInfo expInfo, Situation situation)
		{
			ExpInfo = expInfo;
			Situation = situation;
			Id = String.BuildString(ExpInfo.ExperimentId, "@", Situation.Id.ToString());
			StockSubjectId = String.BuildString(ExpInfo.ExperimentId, "@", Situation.GetStockIdForExperiment(ExpInfo));
			IncludedSubjects = new List<SubjectData>();
		}

		internal void CheckRnD()
		{
			if (Science.GameHasRnD)
			{
				RnDSubject = ResearchAndDevelopment.GetSubjectByID(StockSubjectId);
			}
			else
			{
				ScienceSubject savedSubject;
				if (ScienceDB.sandboxSubjects.TryGetValue(StockSubjectId, out savedSubject))
					RnDSubject = savedSubject;
			}

			if (RnDSubject == null)
			{
				PercentRetrieved = 0.0;
				TimesCompleted = 0;
			}
			else
			{
				PercentRetrieved = RnDSubject.science / RnDSubject.scienceCap;
				TimesCompleted = GetTimesCompleted(PercentRetrieved);
				ScienceDB.persistedSubjects.Add(this);
			}
		}

		internal void Load(ConfigNode node)
		{
			PercentRetrieved = Lib.ConfigValue(node, "percentRetrieved", 0.0);
			TimesCompleted = GetTimesCompleted(PercentRetrieved);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("percentRetrieved", PercentRetrieved);
		}

		int GetTimesCompleted(double percentRetrieved)
		{
			double decimalPart = percentRetrieved - System.Math.Truncate(percentRetrieved);
			return ((int)percentRetrieved) + (decimalPart < 1.0 - (Science.scienceLeftForSubjectCompleted / ScienceMaxValue) ? 0 : 1);
		}

		internal void CreateSubjectInRnD()
		{
			if (ExistsInRnD)
				return;

			Dictionary<string, ScienceSubject> subjectsDB;

			if (Science.GameHasRnD)
			{
				if (ResearchAndDevelopment.Instance == null)
					return;

				// get subjects dictionary using reflection
				subjectsDB = Reflection.ReflectionValue<Dictionary<string, ScienceSubject>>
				(
					ResearchAndDevelopment.Instance,
					"scienceSubjects"
				);

				// try to get the subject, might be already created in some corner-case situations
				RnDSubject = ResearchAndDevelopment.GetSubjectByID(StockSubjectId);
			}
			else
			{
				subjectsDB = ScienceDB.sandboxSubjects;

				// try to get the subject, might be already created in some corner-case situations
				ScienceSubject savedSubject;
				if (subjectsDB.TryGetValue(StockSubjectId, out savedSubject))
					RnDSubject = savedSubject;
			}

			if (RnDSubject != null)
			{
				Logging.Log("CreateSubjectInRnD : ScienceSubject " + StockSubjectId + "exists already, this should not be happening !");
			}
			else
			{
				// create new subject
				RnDSubject = new ScienceSubject
				(
					StockSubjectId,
					FullTitle,
					(float)ExpInfo.DataScale,
					(float)Situation.SituationMultiplier,
					(float)ScienceMaxValue
				);

				// add it to RnD or sandbox DB
				subjectsDB.Add(StockSubjectId, RnDSubject);
			}

			SetAsPersistent();
		}

		/// <summary> add data to the in-flight collected science </summary>
		internal void AddDataCollectedInFlight(double dataAmount)
		{
			ScienceCollectedInFlight += dataAmount * SciencePerMB;
		}

		/// <summary> remove data from the in-flight collected science </summary>
		internal void RemoveDataCollectedInFlight(double dataAmount)
		{
			ScienceCollectedInFlight -= dataAmount * SciencePerMB;
			if (ScienceCollectedInFlight < 0.0) ScienceCollectedInFlight = 0.0;
		}

		/// <summary> remove science points from the in-flight collected science </summary>
		internal void RemoveScienceCollectedInFlight(double credits)
		{
			ScienceCollectedInFlight -= credits;
			if (ScienceCollectedInFlight < 0.0) ScienceCollectedInFlight = 0.0;
		}

		internal void ClearDataCollectedInFlight() => ScienceCollectedInFlight = 0.0;

		/// <summary>
		/// update our subject completion database.
		/// if the subject was just completed, return the amount of times it has ever been completed.
		/// otherwise return -1
		/// </summary>
		internal int UpdateSubjectCompletion(double scienceAdded)
		{
			PercentRetrieved = ((PercentRetrieved * ScienceMaxValue) + scienceAdded) / ScienceMaxValue;
			int newTimesCompleted = GetTimesCompleted(PercentRetrieved);
			if (newTimesCompleted > TimesCompleted)
			{
				TimesCompleted = newTimesCompleted;
				return TimesCompleted;
			}
			return -1;
		}

		internal void SetAsPersistent()
		{
			ScienceDB.persistedSubjects.Add(this);
		}

		/// <summary>
		/// Add science points to the RnD stock subject (create it if necessary), do it recursively for any included subject, then credit the total science gained.
		/// </summary>
		/// <param name="scienceValue">science point amount</param>
		/// <param name="showMessage">if true, the "subject completed" message will be shown on screen if scienceValue is enough to complete the subject</param>
		/// <param name="fromVessel">passed to the OnScienceRecieved gameevent on subject completion. Can be null if not available</param>
		/// <param name="file">if not null, the "subject completed" completed message will use the result text stored in the file. If null, it will be a generic message</param>
		/// <returns>The amount of science credited, accounting for the subject + included subjects remaining science value</returns>
		internal double RetrieveScience(double scienceValue, bool showMessage = false, ProtoVessel fromVessel = null, File file = null)
		{
			if (!ExistsInRnD)
				CreateSubjectInRnD();

			double scienceRetrieved = System.Math.Min(ScienceRemainingToRetrieve, scienceValue);

			if (!API.preventScienceCrediting)
				ScienceDB.uncreditedScience += scienceRetrieved;

			// fire subject completed events
			int timesCompleted = UpdateSubjectCompletion(scienceValue);
			if (timesCompleted > 0)
				OnSubjectCompleted(showMessage, fromVessel, file);

			RnDSubject.science = System.Math.Min((float)(RnDSubject.science + scienceValue), RnDSubject.scienceCap);
			RnDSubject.scientificValue = ResearchAndDevelopment.GetSubjectValue(RnDSubject.science, RnDSubject);

			if (API.subjectsReceivedEventEnabled)
			{
				bool exists = false;
				for (int i = 0; i < ScienceDB.subjectsReceivedBuffer.Count; i++)
				{
					if (ScienceDB.subjectsReceivedBuffer[i].id == RnDSubject.id)
					{
						ScienceDB.subjectsReceivedValueBuffer[i] += scienceRetrieved;
						exists = true;
						break;
					}
				}
				if (!exists)
				{
					ScienceDB.subjectsReceivedBuffer.Add(RnDSubject);
					ScienceDB.subjectsReceivedValueBuffer.Add(scienceRetrieved);
				}
			}

			foreach (SubjectData overridenSubject in IncludedSubjects)
				scienceRetrieved += overridenSubject.RetrieveScience(scienceValue, showMessage && overridenSubject.TimesCompleted == 0, fromVessel);

			return scienceRetrieved;
		}

		void OnSubjectCompleted(bool showMessage = false, ProtoVessel fromVessel = null, File file = null)
		{
			// fire science transmission game event. This is used by stock contracts and a few other things.
			// note : in stock, it is fired with a null protovessel in some cases, so doing it should be safe.

			if (API.preventScienceCrediting)
			{
				// passing 0 as a value breaks the stock "recover science from Kerbin" contracts (one of the
				// fist 4 contracts at the beginning of the game). Bureaucracy ignores all values < 0.1, so passing
				// 0.01 here should be OK (see https://github.com/Kerbalism/Kerbalism/issues/630)
				GameEvents.OnScienceRecieved.Fire(0.01f, RnDSubject, fromVessel, false);
			}
			else
				GameEvents.OnScienceRecieved.Fire(TimesCompleted == 1 ? (float)ScienceMaxValue : 0f, RnDSubject, fromVessel, false);

			if (ExpInfo.UnlockResourceSurvey)
			{
				ResourceMap.Instance.UnlockPlanet(Situation.Body.flightGlobalsIndex);
				Message.Post(Localizer.Format("#autoLOC_259361", Situation.BodyTitle) + "</color>");
			}

			if (!showMessage)
				return;

			// notify the player
			string subjectResultText;
			if (file == null || string.IsNullOrEmpty(file.resultText))
			{
				subjectResultText = String.TextVariant(
					Local.SciencresultText1,//"Our researchers will jump on it right now"
					Local.SciencresultText2,//"This cause some excitement"
					Local.SciencresultText3,//"These results are causing a brouhaha in R&D"
					Local.SciencresultText4,//"Our scientists look very confused"
					Local.SciencresultText5);//"The scientists won't believe these readings"
			}
			else
			{
				subjectResultText = file.resultText;
			}
			subjectResultText = String.WordWrapAtLength(subjectResultText, 70);
			Message.Post(String.BuildString(
				FullTitle,
				" ", Local.Scienctransmitted_title, "\n",//transmitted
				TimesCompleted == 1 ? HumanReadable.Science(ScienceMaxValue, false) : String.Color(Local.Nosciencegain, String.Kolor.Orange, true)),//"no science gain : we already had this data"
				subjectResultText);
		}
	}

	/// <summary>
	/// this is meant to handle subjects created by the stock system with the
	/// ResearchAndDevelopment.GetExperimentSubject overload that take a "sourceUId" string (asteroid samples)
	/// It will also be used for subjects created by mods that use a custom format we can't interpret.
	/// </summary>
	class UnknownSubjectData : SubjectData
	{
		string extraSituationInfo;

		internal UnknownSubjectData(ExperimentInfo expInfo, Situation situation, string subjectId, ScienceSubject stockSubject = null, string extraSituationInfo = "") : base(expInfo, situation)
		{
			StockSubjectId = subjectId;
			this.extraSituationInfo = extraSituationInfo;
			ExpInfo = expInfo;
			Situation = situation;
			RnDSubject = stockSubject;
			ScienceCollectedInFlight = 0.0;

			TimesCompleted = ExistsInRnD ? (int)(RnDSubject.science / (RnDSubject.scienceCap - Science.scienceLeftForSubjectCompleted)) : 0;
			PercentRetrieved = ExistsInRnD ? RnDSubject.science / ScienceMaxValue : 0.0;
		}

		public override string Id => StockSubjectId;

		internal override string FullTitle =>
			ExistsInRnD
			? RnDSubject.title
			: String.BuildString(ExpInfo.Title, " (", SituationTitle, ")");

		internal override string SituationTitle =>
			string.IsNullOrEmpty(extraSituationInfo)
			? base.SituationTitle
			: String.BuildString(base.SituationTitle, " from ", extraSituationInfo);

		internal override string BiomeTitle =>
			string.IsNullOrEmpty(Situation.BiomeTitle)
			? extraSituationInfo
			: string.IsNullOrEmpty(extraSituationInfo)
			? Situation.BiomeTitle
			: String.BuildString(Situation.BiomeTitle, " - ", extraSituationInfo);
	}
}
