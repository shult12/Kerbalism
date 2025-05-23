using KSP.UI.Screens.Flight.Dialogs;
using System;
using UnityEngine;

namespace KERBALISM
{


	// Remove the data from experiments (and set them inoperable) as soon as the
	// science dialog is opened, and store the data in the vessel drive.
	// This method support any module that set an appropriate OnDiscardData() callback
	// when opening the science dialog, this include stock science experiments and others.
	// Hiding the science dialog can be used by who doesn't want it.
	sealed class MiniHijacker : MonoBehaviour
	{
		void Start()
		{
			// get dialog
			dialog = gameObject.GetComponentInParent<ExperimentsResultDialog>();
			if (dialog == null) { Destroy(gameObject); return; }

			// prevent rendering
			dialog.gameObject.SetActive(false);

			// for each page
			// - some mod may collect multiple experiments at once
			while (dialog.pages.Count > 0)
			{
				// get page
				var page = dialog.pages[0];

				// get science data
				ScienceData data = page.pageData;

				// collect and deduce all info necessary
				MetaData meta = new MetaData(data, page.host, page.xmitDataScalar);

				if (meta.subjectData == null)
					continue;

				// ignore non-collectable experiments
				if (!meta.is_collectable)
				{
					page.OnKeepData(data);
					continue;					
				}

				bool recorded = false;
				bool partial_record = false;

				if (!meta.is_sample)
				{
					var remaining = RecordData(data, meta);
					if (remaining > 0) partial_record = true;
					recorded = remaining < data.dataAmount;
				}
				else
				{
					Drive drive = Drive.SampleDrive(meta.vessel.KerbalismData(), data.dataAmount, meta.subjectData);
					if (drive != null)
						recorded = drive.Record_sample(meta.subjectData, data.dataAmount, meta.subjectData.ExpInfo.MassPerMB * data.dataAmount, true);
				}

				if (recorded)
				{
					if(!partial_record)
					{
						// render experiment inoperable if necessary
						if (!meta.is_rerunnable)
						{
							meta.experiment.SetInoperable();
						}

						// inform the user
						Message.Post(
							String.BuildString("<b>", meta.subjectData.FullTitle, "</b> recorded"),
							!meta.is_rerunnable ? Local.Science_inoperable : string.Empty
						);
					}

					// dump the data
					page.OnDiscardData(data);
				}
				else
				{
					Message.Post(
						String.Color(String.BuildString(meta.subjectData.FullTitle, " can not be stored"), String.Kolor.Red),
						"Not enough space on hard drive"
					);
				}
			}

			// dismiss the dialog
			dialog.Dismiss();
		}

		internal static double RecordData(ScienceData data, MetaData meta)
		{
			double remaining = data.dataAmount;

			foreach(var drive in Drive.GetDrives(meta.vessel.KerbalismData(), false))
			{
				var size = System.Math.Min(remaining, drive.FileCapacityAvailable());
				if(size > 0)
				{
					drive.Record_file(meta.subjectData, size, true, true);
					remaining -= size;
				}
			}

			if (remaining > 0)
			{
				Message.Post(
					String.Color(String.BuildString(meta.subjectData.FullTitle, " stored partially"), String.Kolor.Orange),
					"Not enough space on hard drive"
				);
			}
			return remaining;
		}

		ExperimentsResultDialog dialog;
	}

	// Manipulate science dialog callbacks to remove the data from the experiment
	// (rendering it inoperable) and store it in the vessel drive. The same data
	// capture method as in MiniHijacker is used, but the science dialog is not hidden.
	// Any event closing the dialog (like going on eva, or recovering) will act as
	// if the 'keep' button was pressed for each page.
	sealed class Hijacker : MonoBehaviour
	{
		void Start()
		{
			dialog = gameObject.GetComponentInParent<ExperimentsResultDialog>();
			if (dialog == null) { Destroy(gameObject); return; }
		}

		void Update()
		{
			var page = dialog.currentPage;
			page.OnKeepData = (ScienceData data) => Hijack(data, false);
			page.OnTransmitData = (ScienceData data) => Hijack(data, true);
			page.showTransmitWarning = false; //< mom's spaghetti
		}

		void Hijack(ScienceData data, bool send)
		{
			// shortcut
			ExperimentResultDialogPage page = dialog.currentPage;

			// collect and deduce all data necessary just once
			MetaData meta = new MetaData(data, page.host, page.xmitDataScalar);

			if (!meta.is_collectable)
			{
				dialog.Dismiss();
				return;
			}

			// hijack the dialog
			if (!meta.is_rerunnable)
			{
				popup = UI.Popup
				(
				  "Warning!",
				  "Recording the data will render this module inoperable.\n\nRestoring functionality will require a scientist.",
				  new DialogGUIButton("Record data", () => Record(meta, data, send)),
				  new DialogGUIButton("Discard data", () => Dismiss(data))
				);
			}
			else
			{
				Record(meta, data, send);
			}
		}


		void Record(MetaData meta, ScienceData data, bool send)
		{
			// if amount is zero, warn the user and do nothing else
			if (data.dataAmount <= double.Epsilon)
			{
				Message.Post("There is no more useful data here");
				return;
			}

			if (meta.subjectData == null)
				return;

			// if this is a sample and we are trying to send it, warn the user and do nothing else
			if (meta.is_sample && send)
			{
				Message.Post("We can't transmit a sample", "It needs to be recovered, or analyzed in a lab");
				return;
			}

			// record data in the drive
			bool recorded = false;
			bool partial_record = false;

			if (!meta.is_sample)
			{
				var remaining = MiniHijacker.RecordData(data, meta);
				if (remaining > 0) partial_record = true;
				recorded = remaining < data.dataAmount;
			}
			else
			{
				Drive drive = Drive.SampleDrive(meta.vessel.KerbalismData(), data.dataAmount, meta.subjectData);
				if (drive != null)
					recorded = drive.Record_sample(meta.subjectData, data.dataAmount, meta.subjectData.ExpInfo.MassPerMB * data.dataAmount, true);
			}

			if (recorded)
			{
				// flag for sending if specified
				if (!meta.is_sample && send)
				{
					foreach(var d in Drive.GetDrives(meta.vessel))
						d.Send(data.subjectID, true);
				}

				// render experiment inoperable if necessary
				if (!meta.is_rerunnable && !partial_record) meta.experiment.SetInoperable();

				// dismiss the dialog and popups
				Dismiss(data);

				if(!partial_record)
				{
					// inform the user
					Message.Post(
						String.BuildString("<b>", meta.subjectData.FullTitle, "</b> recorded"),
						!meta.is_rerunnable ? Local.Science_inoperable : string.Empty
					);
				}
			}
			else
			{
				Message.Post(
					String.Color(String.BuildString(meta.subjectData.FullTitle, " can not be stored"), String.Kolor.Red),
					"Not enough space on hard drive"
				);
			}
		}


		void Dismiss(ScienceData data)
		{
			// shortcut
			ExperimentResultDialogPage page = dialog.currentPage;

			// dump the data
			page.OnDiscardData(data);

			// close the confirm popup, if it is open
			if (popup != null)
			{
				popup.Dismiss();
				popup = null;
			}
		}

		ExperimentsResultDialog dialog;
		PopupDialog popup;
	}


	sealed class MetaData
	{
		internal MetaData(ScienceData data, Part host, float xmitScalar)
		{
			// find the part containing the data
			part = host;

			// get the vessel
			vessel = part.vessel;

			subjectData = ScienceDB.GetSubjectDataFromStockId(data.subjectID);
			if (subjectData == null)
				return;

			// get the container module storing the data
			container = Science.Container(part, subjectData.ExpInfo.ExperimentId);

			// get the stock experiment module storing the data (if that's the case)
			experiment = container != null ? container as ModuleScienceExperiment : null;

			// determine if data is supposed to be removable from the part
			is_collectable = experiment == null || experiment.dataIsCollectable;

			// determine if this is a sample (non-transmissible)
			// - if this is a third-party data container/experiment module, we assume it is transmissible
			// - stock experiment modules are considered sample if xmit scalar is below a threshold instead
			is_sample = xmitScalar < Science.maxXmitDataScalarForSample;

			// determine if the container/experiment can collect the data multiple times
			// - if this is a third-party data container/experiment, we assume it can collect multiple times
			is_rerunnable = experiment == null || experiment.rerunnable;
		}

		Part part;                               // part storing the data
		internal Vessel vessel;                           // vessel storing the data
		IScienceDataContainer container;         // module containing the data
		internal ModuleScienceExperiment experiment;      // module containing the data, as a stock experiment module
		internal bool is_sample;                          // true if the data can't be transmitted
		internal bool is_rerunnable;                      // true if the container/experiment can collect data multiple times
		internal bool is_collectable;                     // true if data can be collected from the module / part
		internal SubjectData subjectData;
	}


} // KERBALISM

