using System.Text;

namespace KERBALISM
{
	sealed class ExperimentDevice : LoadedDevice<Experiment>
	{
		readonly DeviceIcon icon;
		StringBuilder sb;
		string scienceValue;

		internal ExperimentDevice(Experiment module) : base(module)
		{
			icon = new DeviceIcon(module.ExpInfo.SampleMass > 0.0 ? Textures.sample_scicolor : Textures.file_scicolor, "open experiment window", () => new ExperimentPopup(module.vessel, module, PartId, PartName));
			sb = new StringBuilder();
			OnUpdate();
		}

		internal override void OnUpdate()
		{
			scienceValue = Experiment.ScienceValue(module.Subject);
		}

		internal override string Name => module.experiment_id;

		internal override string DisplayName
		{
			get
			{
				sb.Length = 0;
				sb.Append(Lib.EllipsisMiddle(module.ExpInfo.Title, 28));
				sb.Append(": ");
				sb.Append(scienceValue);

				if (module.Status == Experiment.ExpStatus.Running)
				{
					sb.Append(" ");
					sb.Append(Experiment.RunningCountdown(module.ExpInfo, module.Subject, module.data_rate, module.prodFactor));
				}
				else if (module.Subject != null && module.Status == Experiment.ExpStatus.Forced)
				{
					sb.Append(" ");
					sb.Append(module.Subject.PercentCollectedTotal.ToString("P0"));
				}
				return sb.ToString();
			}
		}

		internal override string Status => Experiment.StatusInfo(module.Status, module.issue);

		internal override string Tooltip
		{
			get
			{
				sb.Length = 0;
				if (module.Subject != null)
					sb.Append(module.Subject.FullTitle);
				else
					sb.Append(module.ExpInfo.Title);
				sb.Append("\n");
				sb.Append(Local.Experiment_on);//on
				sb.Append(" ");
				sb.Append(module.part.partInfo.title);
				sb.Append("\n");
				sb.Append(Local.Experiment_status);//status :
				sb.Append(" ");
				sb.Append(Experiment.StatusInfo(module.Status));

				if (module.Status == Experiment.ExpStatus.Issue)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_issue);//issue :
					sb.Append(" ");
					sb.Append(Lib.Color(module.issue, Lib.Kolor.Orange));
				}
				sb.Append("\n");
				sb.Append(Local.Experiment_sciencevalue);//science value :
				sb.Append(" ");
				sb.Append(scienceValue);

				if (module.Status == Experiment.ExpStatus.Running)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_completion);//completion :
					sb.Append(" ");
					sb.Append(Experiment.RunningCountdown(module.ExpInfo, module.Subject, module.data_rate, module.prodFactor, false));
				}
				else if (module.Subject != null && module.Status == Experiment.ExpStatus.Forced)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_completion);//completion :
					sb.Append(" ");
					sb.Append(module.Subject.PercentCollectedTotal.ToString("P0"));
				}

				return sb.ToString();
			}
		}

		internal override DeviceIcon Icon => icon;

		internal override void Ctrl(bool value)
		{
			if (value != module.Running) Toggle();
		}

		internal override void Toggle()
		{
			module.Toggle();
		}

		internal override string PartName => module.part.partInfo.title;
	}

	sealed class ProtoExperimentDevice : ProtoDevice<Experiment>
	{
		readonly Vessel vessel;

		readonly DeviceIcon icon;

		string issue;
		ExperimentInfo expInfo;
		Experiment.ExpStatus status;
		SubjectData subject;
		string scienceValue;
		double prodFactor;

		StringBuilder sb;

		internal ProtoExperimentDevice(Experiment prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, Vessel vessel)
			: base(prefab, protoPart, protoModule)
		{
			this.vessel = vessel;
			expInfo = ScienceDB.GetExperimentInfo(prefab.experiment_id);
			icon = new DeviceIcon(expInfo.SampleMass > 0f ? Textures.sample_scicolor : Textures.file_scicolor, "open experiment info", () => new ExperimentPopup(vessel, prefab, protoPart.flightID, prefab.part.partInfo.title, protoModule));
			sb = new StringBuilder();

			OnUpdate();
		}

		internal override void OnUpdate()
		{
			issue = Lib.Proto.GetString(protoModule, "issue");
			status = Lib.Proto.GetEnum(protoModule, "status", Experiment.ExpStatus.Stopped);
			subject = ScienceDB.GetSubjectData(expInfo, Lib.Proto.GetInt(protoModule, "situationId"));
			scienceValue = Experiment.ScienceValue(subject);
			prodFactor = Lib.Proto.GetDouble(protoModule, "prodFactor");
		}

		internal override string Name => prefab.experiment_id;

		internal override string DisplayName
		{
			get
			{
				sb.Length = 0;
				sb.Append(Lib.EllipsisMiddle(expInfo.Title, 28));
				sb.Append(": ");
				sb.Append(scienceValue);

				if (status == Experiment.ExpStatus.Running)
				{
					sb.Append(" ");
					sb.Append(Experiment.RunningCountdown(expInfo, subject, prefab.data_rate, prodFactor));
				}
				else if (subject != null && status == Experiment.ExpStatus.Forced)
				{
					sb.Append(" ");
					sb.Append(subject.PercentCollectedTotal.ToString("P0"));
				}
				return sb.ToString();
			}
		}

		internal override string Status => Experiment.StatusInfo(status, issue);

		internal override string Tooltip
		{
			get
			{
				sb.Length = 0;
				if (subject != null && Experiment.IsRunning(status))
					sb.Append(subject.FullTitle);
				else
					sb.Append(expInfo.Title);
				sb.Append("\n");
				sb.Append(Local.Experiment_on);//on
				sb.Append(" ");
				sb.Append(prefab.part.partInfo.title);
				sb.Append("\n");
				sb.Append(Local.Experiment_status);//status :
				sb.Append(" ");
				sb.Append(Experiment.StatusInfo(status));

				if (status == Experiment.ExpStatus.Issue)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_issue);//issue :
					sb.Append(" ");
					sb.Append(Lib.Color(issue, Lib.Kolor.Orange));
				}
				sb.Append("\n");
				sb.Append(Local.Experiment_sciencevalue);//science value :
				sb.Append(" ");
				sb.Append(scienceValue);

				if (status == Experiment.ExpStatus.Running)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_completion);//completion :
					sb.Append(" ");
					sb.Append(Experiment.RunningCountdown(expInfo, subject, prefab.data_rate, prodFactor, false));
				}
				else if (subject != null && status == Experiment.ExpStatus.Forced)
				{
					sb.Append("\n");
					sb.Append(Local.Experiment_completion);//completion :
					sb.Append(" ");
					sb.Append(subject.PercentCollectedTotal.ToString("P0"));
				}

				return sb.ToString();
			}
		}

		internal override DeviceIcon Icon => icon;

		internal override void Ctrl(bool value)
		{
			if (value != Experiment.IsRunning(status)) Experiment.ProtoToggle(vessel, prefab, protoModule);
		}

		internal override void Toggle()
		{
			Experiment.ProtoToggle(vessel, prefab, protoModule);
		}
	}
} // KERBALISM

