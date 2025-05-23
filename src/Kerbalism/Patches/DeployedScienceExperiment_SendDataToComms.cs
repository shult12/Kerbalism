using Expansions.Serenity.DeployedScience.Runtime;
using HarmonyLib;
using System.Collections.Generic;

namespace KERBALISM
{
	[HarmonyPatch(typeof(DeployedScienceExperiment))]
	[HarmonyPatch("SendDataToComms")]
	class DeployedScienceExperiment_SendDataToComms {
		static bool Prefix(DeployedScienceExperiment __instance, ref bool __result)
		{

			// Don't mess with anything is science is disabled
			if (!Features.Science)
				return true;

			// get private vars
			ScienceSubject subject = Reflection.ReflectionValue<ScienceSubject>(__instance, "subject");
			float storedScienceData = Reflection.ReflectionValue<float>(__instance, "storedScienceData");
			float transmittedScienceData = Reflection.ReflectionValue<float>(__instance, "transmittedScienceData");
			Vessel ControllerVessel = Reflection.ReflectionValue<Vessel>(__instance, "ControllerVessel");
			//Logging.Log("SendDataToComms!: " + subject.title);
			if (__instance.Experiment != null && !(__instance.ExperimentVessel == null) && subject != null && !(__instance.Cluster == null) && __instance.sciencePart.Enabled && !(storedScienceData <= 0f) && __instance.ExperimentSituationValid) {
			/*	if (!__instance.TimeToSendStoredData())
				{
					__result = true;
					Logging.Log(String.BuildString("BREAKING GROUND bailout 1"));
					return false;
				} */
				
				if(ControllerVessel == null && __instance.Cluster != null)
				{
					Reflection.ReflectionCall(__instance, "SetControllerVessel");
					ControllerVessel = Reflection.ReflectionValue<Vessel>(__instance, "ControllerVessel");
				}

				/*
				Part control;
				FlightGlobals.FindLoadedPart(__instance.Cluster.ControlModulePartId, out control);
				if(control == null) {
					//Logging.Log("DeployedScienceExperiment: couldn't find control module");
					__result = true;
					Logging.Log(String.BuildString("BREAKING GROUND bailout 2"));
					return false;
				}
				*/

				List<Drive> drives = Drive.GetDrives(ControllerVessel, false);
				SubjectData subjectData = ScienceDB.GetSubjectDataFromStockId(subject.id);
				double sciencePerMB = subjectData.SciencePerMB;
				if (sciencePerMB == 0.0)
				{
					Logging.Log($"SciencePerMB is 0 for {subjectData.FullTitle} !", Logging.LogLevel.Error);
					__result = false;
					return false;
				}
				float scienceValue = storedScienceData * subject.subjectValue;
				double dataSize = scienceValue / subjectData.SciencePerMB;
				foreach (Drive drive in drives) {
					//Logging.Log(String.BuildString("BREAKING GROUND -- ", subject.id, " | ", storedScienceData.ToString()));
					if(drive.Record_file(subjectData, dataSize, true))
					{
						//Logging.Log("BREAKING GROUND -- file recorded!");
						Reflection.ReflectionValue<float>(__instance, "transmittedScienceData", transmittedScienceData + scienceValue);
						Reflection.ReflectionValue<float>(__instance, "storedScienceData", 0f);
						break;
					}
					else
					{
						//Logging.Log("BREAKING GROUND -- file NOT recorded!");
						__result = true;
						return false;
					}
				}
				__result = false;
			}
			return false; // always return false so we don't continue to the original code
		}
	}
}
