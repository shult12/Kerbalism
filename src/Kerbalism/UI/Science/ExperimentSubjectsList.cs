using KERBALISM.KsmGui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KERBALISM.ScienceDB;
using KSP.Localization;

namespace KERBALISM
{
	class ExperimentSubjectList : KsmGuiVerticalLayout
	{
		internal KsmGuiToggle KnownSubjectsToggle {get; private set;}
		internal List<BodyContainer> BodyContainers = new List<BodyContainer>();


		internal ExperimentSubjectList(KsmGuiBase parent, ExperimentInfo expInfo) : base(parent)
		{
			KnownSubjectsToggle = new KsmGuiToggle(this, Local.SCIENCEARCHIVE_Showonlyknownsubjects, true, ToggleKnownSubjects, null, -1, 21);//"Show only known subjects"

			KsmGuiBase listHeader = new KsmGuiBase(this);
			listHeader.SetLayoutElement(true, false, -1, 16);
			listHeader.AddImageComponentWithColor(KsmGuiStyle.boxColor);

			KsmGuiText rndHeaderText = new KsmGuiText(listHeader, Local.SCIENCEARCHIVE_RnD, Local.SCIENCEARCHIVE_RnD_desc, TextAlignmentOptions.Left);//"RnD""Science points\nretrieved in RnD"
			rndHeaderText.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Science);
			rndHeaderText.TextComponent.fontStyle = FontStyles.Bold;
			rndHeaderText.TopTransform.SetAnchorsAndPosition(TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, 10, 0);
			rndHeaderText.TopTransform.SetSizeDelta(50, 16);

			KsmGuiText flightHeaderText = new KsmGuiText(listHeader, Local.SCIENCEARCHIVE_Flight, Local.SCIENCEARCHIVE_Flight_desc, TextAlignmentOptions.Left);//"Flight""Science points\ncollected in all vessels"
			flightHeaderText.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Science);
			flightHeaderText.TextComponent.fontStyle = FontStyles.Bold;
			flightHeaderText.TopTransform.SetAnchorsAndPosition(TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, 60, 0);
			flightHeaderText.TopTransform.SetSizeDelta(50, 16);

			KsmGuiText valueHeaderText = new KsmGuiText(listHeader, Local.SCIENCEARCHIVE_Value, Local.SCIENCEARCHIVE_Value_desc, TextAlignmentOptions.Left);//"Value""Remaining science value\naccounting for data retrieved in RnD\nand collected in flight"
			valueHeaderText.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Science);
			valueHeaderText.TextComponent.fontStyle = FontStyles.Bold;
			valueHeaderText.TopTransform.SetAnchorsAndPosition(TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, 110, 0);
			valueHeaderText.TopTransform.SetSizeDelta(50, 16);

			KsmGuiText completedHeaderText = new KsmGuiText(listHeader, Local.SCIENCEARCHIVE_Completed, Local.SCIENCEARCHIVE_Completed_desc, TextAlignmentOptions.Left);//"Completed""How many times the subject\nhas been retrieved in RnD"
			completedHeaderText.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Yellow);
			completedHeaderText.TextComponent.fontStyle = FontStyles.Bold;
			completedHeaderText.TopTransform.SetAnchorsAndPosition(TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, 160, 0);
			completedHeaderText.TopTransform.SetSizeDelta(100, 16);

			KsmGuiVerticalScrollView scrollView = new KsmGuiVerticalScrollView(this);
			scrollView.SetLayoutElement(true, true, 320, -1, -1, 250);
			scrollView.ContentGroup.padding = new RectOffset(0, 5, 5, 5);

			BodiesSituationsBiomesSubject subjects = GetSubjectsForExperiment(expInfo);
			if (subjects != null)
			{
				foreach (ObjectPair<int, SituationsBiomesSubject> bodySubjects in GetSubjectsForExperiment(expInfo))
				{
					CelestialBody body = FlightGlobals.Bodies[bodySubjects.Key];
					BodyContainer bodyEntry = new BodyContainer(scrollView, body, bodySubjects.Value);
					BodyContainers.Add(bodyEntry);
				}
			}

			SetUpdateCoroutine(new KsmGuiUpdateCoroutine(Update));
			ForceExecuteCoroutine();
			ToggleKnownSubjects(true);
		}

		void ToggleKnownSubjects(bool onlyKnown)
		{
			foreach (BodyContainer body in BodyContainers)
			{
				if (onlyKnown && !body.isKnown)
				{
					body.Enabled = false;
					continue;
				}
				body.Enabled = true;
				body.ToggleBody(body.isKnown);

				foreach (SituationContainer situation in body.SubjectsContainer.Situations)
				{
					situation.Enabled = (onlyKnown && situation.isKnown) || !onlyKnown;

					foreach (SubjectLine subject in situation.SubjectLines)
					{
						subject.Enabled = (onlyKnown && subject.isKnown) || !onlyKnown;
					}
				}
			}
			RebuildLayout();
		}

		IEnumerator Update()
		{
			foreach (BodyContainer body in BodyContainers)
			{
				body.isKnown = false;

				foreach (SituationContainer situation in body.SubjectsContainer.Situations)
				{
					// check if unknown (non-DB) subjects have been created
					if (situation.DBLinesCount() != situation.SubjectLines.Count)
						situation.UpdateLines();

					situation.isKnown = false;
					foreach (SubjectLine subject in situation.SubjectLines)
					{
						if (subject.SubjectData.ScienceCollectedTotal > 0.0)
						{
							subject.isKnown = true;
							situation.isKnown |= true;
							body.isKnown |= true;
						}

						if (KnownSubjectsToggle.IsOn && subject.Enabled != subject.isKnown)
						{
							if (!body.SubjectsContainer.IsInstantiated)
								body.SubjectsContainer.InstantiateUIObjects();

							subject.Enabled = subject.isKnown;
							RebuildLayout();
						}

						subject.UpdateText();
					}

					if (KnownSubjectsToggle.IsOn && body.SubjectsContainer.IsInstantiated && situation.Enabled != situation.isKnown)
					{
						situation.Enabled = situation.isKnown;
						RebuildLayout();
					}

					// only do 1 situation per update
					yield return null;
				}

				if (KnownSubjectsToggle.IsOn && body.Enabled != body.isKnown)
				{
					body.Enabled = body.isKnown;
					RebuildLayout();
				}
			}
			yield break;
		}

		internal class BodyContainer: KsmGuiVerticalLayout
		{
			internal bool isKnown;
			internal SubjectsContainer SubjectsContainer { get; private set; }
			KsmGuiIconButton bodyToggle;

			internal BodyContainer(KsmGuiBase parent, CelestialBody body, SituationsBiomesSubject situationsAndSubjects) : base(parent)
			{
				KsmGuiHeader header = new KsmGuiHeader(this, body.name, KsmGuiStyle.boxColor);
				header.TextObject.TextComponent.fontStyle = FontStyles.Bold;
				header.TextObject.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Orange);
				header.TextObject.TextComponent.alignment = TextAlignmentOptions.Left;
				bodyToggle = new KsmGuiIconButton(header, Textures.KsmGuiTexHeaderArrowsUp, ToggleBody);
				bodyToggle.SetIconColor(Lib.Kolor.Orange);
				bodyToggle.MoveAsFirstChild();

				SubjectsContainer = new SubjectsContainer(this, situationsAndSubjects);
			}

			void ToggleBody()
			{
				ToggleBody(!SubjectsContainer.Enabled);
			}

			internal void ToggleBody(bool enable)
			{
				if (enable)
					SubjectsContainer.InstantiateUIObjects();

				SubjectsContainer.Enabled = enable;
				bodyToggle.SetIconTexture(enable ? Textures.KsmGuiTexHeaderArrowsUp : Textures.KsmGuiTexHeaderArrowsDown);
				RebuildLayout();
			}
		}

		internal class SubjectsContainer : KsmGuiVerticalLayout
		{
			internal List<SituationContainer> Situations { get; private set; } = new List<SituationContainer>();
			internal bool IsInstantiated { get; private set; } = false;

			internal SubjectsContainer(BodyContainer parent, SituationsBiomesSubject situationsSubjects) : base(parent)
			{
				foreach (ObjectPair<ScienceSituation, BiomesSubject> situation in situationsSubjects)
				{
					Situations.Add(new SituationContainer(situation));
				}
			}

			internal void InstantiateUIObjects()
			{
				if (IsInstantiated || Situations.Count == 0)
					return;

				IsInstantiated = true;

				foreach (SituationContainer situationContainer in Situations)
				{
					situationContainer.InstantiateUIObjects(this);
				}
			}

			internal void DestroyUIObjects()
			{
				if (IsInstantiated)
				{
					IsInstantiated = false;
					foreach (SituationContainer situationContainer in Situations)
					{
						situationContainer.DestroyUIObjects();
					}
				}
			}
		}

		internal class SituationContainer
		{
			internal bool isKnown;
			KsmGuiText situationText;
			bool IsInstantiated => situationText != null;
			ObjectPair<ScienceSituation, BiomesSubject> situationSubjects;

			internal bool Enabled
			{
				get => situationText != null ? situationText.Enabled : false;
				set
				{
					if (situationText != null)
						situationText.Enabled = value;
				}
			}

			internal List<SubjectLine> SubjectLines { get; private set; } = new List<SubjectLine>();

			internal SituationContainer(ObjectPair<ScienceSituation, BiomesSubject> situationSubjects)
			{
				this.situationSubjects = situationSubjects;
				foreach (ObjectPair<int, List<SubjectData>> subjects in situationSubjects.Value)
				{
					foreach (SubjectData subjectData in subjects.Value)
					{
						SubjectLines.Add(new SubjectLine(subjectData));
					}
				}
			}

			internal int DBLinesCount()
			{
				int count = 0;
				foreach (ObjectPair<int, List<SubjectData>> subjects in situationSubjects.Value)
					count += subjects.Value.Count;
				return count;
			}

			internal void UpdateLines()
			{
				SubjectLines.Clear();
				foreach (ObjectPair<int, List<SubjectData>> subjects in situationSubjects.Value)
				{
					foreach (SubjectData subjectData in subjects.Value)
					{
						SubjectLines.Add(new SubjectLine(subjectData));
					}
				}
			}

			internal void InstantiateUIObjects(SubjectsContainer parent)
			{
				isKnown = false;

				if (SubjectLines.Count == 0)
					return;

				situationText = new KsmGuiText(parent, SubjectLines[0].SubjectData.Situation.ScienceSituationTitle);
				situationText.TopTransform.SetAnchorsAndPosition(TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, 5);
				situationText.TopTransform.SetSizeDelta(150, 14);
				situationText.TextComponent.color = Lib.KolorToColor(Lib.Kolor.Yellow);
				situationText.TextComponent.fontStyle = FontStyles.Bold;

				foreach (SubjectLine subjectLine in SubjectLines)
				{
					subjectLine.InstantiateText(parent);

					if (subjectLine.SubjectData.ScienceCollectedTotal > 0.0)
					{
						subjectLine.isKnown = true;
						isKnown |= true;
					}
				}
			}

			internal void DestroyUIObjects()
			{
				if (situationText != null)
				{
					situationText.TopObject.DestroyGameObject();
					situationText = null;
				}

				foreach (SubjectLine subjectLine in SubjectLines)
					subjectLine.DestroyText();
			}
		}

		internal class SubjectLine
		{
			internal bool isKnown;
			internal SubjectData SubjectData { get; private set; }
			KsmGuiText subjectText;

			internal bool Enabled
			{
				get => subjectText != null ? subjectText.Enabled : false;
				set
				{
					if (subjectText != null)
						subjectText.Enabled = value;
				}
			}

			internal SubjectLine(SubjectData subject)
			{
				SubjectData = subject;
			}

			internal void InstantiateText(SubjectsContainer parent)
			{
				subjectText = new KsmGuiText(parent, GetText(), null, TextAlignmentOptions.TopLeft, false);
				subjectText.SetLayoutElement(true, false, -1, 14);
			}

			internal void DestroyText()
			{
				if (subjectText != null)
				{
					subjectText.TopObject.DestroyGameObject();
					subjectText = null;
				}
			}

			internal void UpdateText()
			{
				if (subjectText != null) subjectText.Text = GetText();
			}

			string GetText()
			{
				return Lib.BuildString
				(
					"<pos=10>",
					Lib.Color(Math.Round(SubjectData.ScienceRetrievedInKSC, 1).ToString("0.0;--;--"), Lib.Kolor.Science, true),
					"<pos=60>",
					Lib.Color(Math.Round(SubjectData.ScienceCollectedInFlight, 1).ToString("+0.0;--;--"), Lib.Kolor.Science, true),
					"<pos=110>",
					Lib.Color(Math.Round(SubjectData.ScienceRemainingTotal, 1).ToString("0.0;--;--"), Lib.Kolor.Science, true),
					"<pos=160>",
					Lib.Color(Math.Round(SubjectData.PercentRetrieved, 1).ToString("0.0x;--;--"), Lib.Kolor.Yellow, true),
					"<pos=200>",
					SubjectData.BiomeTitle
				);
			}
		}
	}




}
