using System;
using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	static class FileManager
	{

		/// <summary>
		/// If short_strings parameter is true then the strings used for display of the data will be shorter when inflight.
		/// </summary>
		internal static void Fileman(this Panel p, Vessel v, bool short_strings = false)
		{
			// avoid corner-case when this is called in a lambda after scene changes
			v = FlightGlobals.FindVessel(v.id);

			// if vessel doesn't exist anymore, leave the panel empty
			if (v == null) return;

			// get info from the cache
			VesselData vd = v.KerbalismData();

			// if not a valid vessel, leave the panel empty
			if (!vd.IsSimulated) return;

			// set metadata
			p.Title(String.BuildString(String.Ellipsis(v.vesselName, Styles.ScaleStringLength(40)), " ", String.Color(Local.FILEMANAGER_title, String.Kolor.LightGrey)));//"FILE MANAGER"
			p.Width(Styles.ScaleWidthFloat(465.0f));
			p.paneltype = Panel.PanelType.data;

 			// time-out simulation
			if (!Lib.IsControlUnit(v) && p.Timeout(vd)) return;

			List<ObjectPair<uint, Drive>> drives = new List<ObjectPair<uint, Drive>>();

			int filesCount = 0;
			double usedDataCapacity = 0;
			double totalDataCapacity = 0;

			int samplesCount = 0;
			int usedSlots = 0;
			int totalSlots = 0;
			double totalMass = 0;
			bool unlimitedData = false;
			bool unlimitedSamples = false;

			foreach (PartData partData in vd.PartDatas)
			{
				Drive drive = partData.Drive;
				if (drive == null)
					continue;

				drives.Add(new ObjectPair<uint, Drive>(partData.FlightId, drive));

				if (!drive.is_private)
				{
					usedDataCapacity += drive.FilesSize();
					totalDataCapacity += drive.dataCapacity;

					unlimitedData |= drive.dataCapacity < 0;
					unlimitedSamples |= drive.sampleCapacity < 0;

					usedSlots += drive.SamplesSize();
					totalSlots += drive.sampleCapacity;
				}

				filesCount += drive.files.Count;
				samplesCount += drive.samples.Count;
				foreach (var sample in drive.samples.Values) totalMass += sample.mass;
			}

			if(filesCount > 0 || totalDataCapacity > 0)
			{
				var title = Local.FILEMANAGER_DataCapacity + " " + HumanReadable.DataSize(usedDataCapacity);//"DATA " 
				if (!unlimitedData) title += Local.FILEMANAGER_DataAvailable.Format(HumanReadable.Percentage((totalDataCapacity - usedDataCapacity) / totalDataCapacity));//String.BuildString(" (", HumanReadable.Percentage((totalDataCapacity - usedDataCapacity) / totalDataCapacity), " available)");
				p.AddSection(title);

				foreach (var drive in drives)
				{
					foreach (File file in drive.Value.files.Values)
					{
						Render_file(p, drive.Key, file, drive.Value, short_strings && GameLogic.IsFlight(), v);
					}
				}

				if(filesCount == 0) p.AddContent("<i>"+Local.FILEMANAGER_nofiles +"</i>", string.Empty);//no files
			}

			if(samplesCount > 0 || totalSlots > 0)
			{
				var title = Local.FILEMANAGER_SAMPLESMass.Format(HumanReadable.Mass(totalMass)) + " " + HumanReadable.SampleSize(usedSlots);//"SAMPLES " + 
				if (totalSlots > 0 && !unlimitedSamples) title += ", " + HumanReadable.SampleSize(totalSlots) + " "+ Local.FILEMANAGER_SAMPLESAvailable;//available
				p.AddSection(title);

				foreach (var drive in drives)
				{
					foreach (Sample sample in drive.Value.samples.Values)
					{
						Render_sample(p, drive.Key, sample, drive.Value, short_strings && GameLogic.IsFlight());
					}
				}

				if (samplesCount == 0) p.AddContent("<i>"+Local.FILEMANAGER_nosamples+"</i>", string.Empty);//no samples
			}
		}

		static void Render_file(Panel p, uint partId, File file, Drive drive, bool short_strings, Vessel v)
		{
			// render experiment name
			string exp_label = String.BuildString
			(
			  "<b>",
			  String.Ellipsis(file.subjectData.ExperimentTitle, Styles.ScaleStringLength(short_strings ? 24 : 38)),
			  "</b> <size=", Styles.ScaleInteger(10).ToString(), ">",
			  String.Ellipsis(file.subjectData.SituationTitle, Styles.ScaleStringLength((short_strings ? 32 : 62) - String.Ellipsis(file.subjectData.ExperimentTitle, Styles.ScaleStringLength(short_strings ? 24 : 38)).Length)),
			  "</size>"
			);
			string exp_tooltip = String.BuildString
			(
			  file.subjectData.ExperimentTitle, "\n",
			  String.Color(file.subjectData.SituationTitle, String.Kolor.LightGrey)
			);

			double exp_value = file.size * file.subjectData.SciencePerMB;
			if (file.subjectData.ScienceRemainingToRetrieve > 0f && file.size > 0.0)
				exp_tooltip = String.BuildString(exp_tooltip, "\n<b>", HumanReadable.Science(exp_value, false), "</b>");
			if (file.transmitRate > 0.0)
			{
				if (file.size > 0.0)
					exp_tooltip = String.Color(String.BuildString(exp_tooltip, "\n", Local.FILEMANAGER_TransmittingRate.Format(HumanReadable.DataRate(file.transmitRate)), " : <i>", HumanReadable.Countdown(file.size / file.transmitRate), "</i>"), String.Kolor.Cyan);//Transmitting at <<1>>
				else
					exp_tooltip = String.Color(String.BuildString(exp_tooltip, "\n", Local.FILEMANAGER_TransmittingRate.Format(HumanReadable.DataRate(file.transmitRate))), String.Kolor.Cyan);//Transmitting at <<1>>
			}
			else if (v.KerbalismData().Connection.rate > 0.0)
				exp_tooltip = String.BuildString(exp_tooltip, "\n", Local.FILEMANAGER_Transmitduration, "<i>", HumanReadable.Duration(file.size / v.KerbalismData().Connection.rate), "</i>");//Transmit duration : 
			if (!string.IsNullOrEmpty(file.resultText))
				exp_tooltip = String.BuildString(exp_tooltip, "\n", String.WordWrapAtLength(file.resultText, 50));

			string size;
			if (file.transmitRate > 0.0 )
			{
				if (file.size == 0.0)
					size = String.Color(String.BuildString("↑ ", HumanReadable.DataRate(file.transmitRate)), String.Kolor.Cyan);
				else
					size = String.Color(String.BuildString("↑ ", HumanReadable.DataSize(file.size)), String.Kolor.Cyan);
			}
			else
			{
				size = HumanReadable.DataSize(file.size);
			}

			p.AddContent(exp_label, size, exp_tooltip, (Action)null, () => Highlighter.Set(partId, Color.cyan));

			bool send = drive.GetFileSend(file.subjectData.Id);
			p.AddRightIcon(send ? Textures.send_cyan : Textures.send_black, Local.FILEMANAGER_send, () => { drive.Send(file.subjectData.Id, !send); });//"Flag the file for transmission to <b>DSN</b>"
			p.AddRightIcon(Textures.toggle_red, Local.FILEMANAGER_Delete, () =>//"Delete the file"
				{
					UI.Popup(Local.FILEMANAGER_Warning_title,//"Warning!"
						Local.FILEMANAGER_DeleteConfirm.Format(file.subjectData.FullTitle),//String.BuildString(, "?"),//"Do you really want to delete <<1>>", 
				        new DialogGUIButton(Local.FILEMANAGER_DeleteConfirm_button1, () => drive.Delete_file(file.subjectData)),//"Delete it"
						new DialogGUIButton(Local.FILEMANAGER_DeleteConfirm_button2, () => { }));//"Keep it"
				}
			);
		}

		static void Render_sample(Panel p, uint partId, Sample sample, Drive drive, bool short_strings)
		{
			// render experiment name
			string exp_label = String.BuildString
			(
			  "<b>",
			  String.Ellipsis(sample.subjectData.ExperimentTitle, Styles.ScaleStringLength(short_strings ? 24 : 38)),
			  "</b> <size=", Styles.ScaleInteger(10).ToString(), ">",
			  String.Ellipsis(sample.subjectData.SituationTitle, Styles.ScaleStringLength((short_strings ? 32 : 62) - String.Ellipsis(sample.subjectData.ExperimentTitle, Styles.ScaleStringLength(short_strings ? 24 : 38)).Length)),
			  "</size>"
			);
			string exp_tooltip = String.BuildString
			(
			  sample.subjectData.ExperimentTitle, "\n",
			  String.Color(sample.subjectData.SituationTitle, String.Kolor.LightGrey)
			);

			double exp_value = sample.size * sample.subjectData.SciencePerMB;
			if (exp_value >= 0.1) exp_tooltip = String.BuildString(exp_tooltip, "\n<b>", HumanReadable.Science(exp_value, false), "</b>");
			if (sample.mass > Double.Epsilon) exp_tooltip = String.BuildString(exp_tooltip, "\n<b>", HumanReadable.Mass(sample.mass), "</b>");
			if (!string.IsNullOrEmpty(sample.resultText)) exp_tooltip = String.BuildString(exp_tooltip, "\n", String.WordWrapAtLength(sample.resultText, 50));

			p.AddContent(exp_label, HumanReadable.SampleSize(sample.size), exp_tooltip, (Action)null, () => Highlighter.Set(partId, Color.cyan));
			p.AddRightIcon(sample.analyze ? Textures.lab_cyan : Textures.lab_black, Local.FILEMANAGER_analysis, () => { sample.analyze = !sample.analyze; });//"Flag the file for analysis in a <b>laboratory</b>"
			p.AddRightIcon(Textures.toggle_red, Local.FILEMANAGER_Dumpsample, () =>//"Dump the sample"
				{
					UI.Popup(Local.FILEMANAGER_Warning_title,//"Warning!"
						Local.FILEMANAGER_DumpConfirm.Format(sample.subjectData.FullTitle),//"Do you really want to dump <<1>>?", 
						new DialogGUIButton(Local.FILEMANAGER_DumpConfirm_button1, () => drive.Delete_sample(sample.subjectData)),//"Dump it"
							  new DialogGUIButton(Local.FILEMANAGER_DumpConfirm_button2, () => { }));//"Keep it"
				}
			);
		}
	}


} // KERBALISM
