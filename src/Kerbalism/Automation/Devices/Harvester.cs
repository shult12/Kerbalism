namespace KERBALISM
{
	sealed class HarvesterDevice : LoadedDevice<Harvester>
	{
		readonly ModuleAnimationGroup animator;

		internal HarvesterDevice(Harvester module) : base(module)
		{
			animator = module.part.FindModuleImplementing<ModuleAnimationGroup>();
		}

		internal override string Name => String.BuildString(module.resource, " harvester").ToLower();

		internal override string Status
		{
			get
			{
			return animator != null && !module.deployed
			  ? Local.Generic_notdeployed//"not deployed"
			  : !module.running
			  ? String.Color(Local.Generic_STOPPED, String.Kolor.Yellow)
			  : module.issue.Length == 0
			  ? String.Color(Local.Generic_RUNNING, String.Kolor.Green)
			  : String.Color(module.issue, String.Kolor.Red);
			}
		}

		internal override void Ctrl(bool value)
		{
			if (module.deployed)
			{
				module.running = value;
			}
		}

		internal override void Toggle()
		{
			Ctrl(!module.running);
		}
	}

	sealed class ProtoHarvesterDevice : ProtoDevice<Harvester>
	{
		readonly ProtoPartModuleSnapshot animator;

		internal ProtoHarvesterDevice(Harvester prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule)
		{
			this.animator = protoPart.FindModule("ModuleAnimationGroup");
		}

		internal override string Name => String.BuildString(prefab.resource, " harvester").ToLower();

		internal override string Status
		{
			get
			{
				bool deployed = Lib.Proto.GetBool(protoModule, "deployed");
				bool running = Lib.Proto.GetBool(protoModule, "running");
				string issue = Lib.Proto.GetString(protoModule, "issue");

				return animator != null && !deployed
				  ? Local.Generic_notdeployed//"not deployed"
				  : !running
				  ? String.Color(Local.Generic_STOPPED, String.Kolor.Yellow)
				  : issue.Length == 0
				  ? String.Color(Local.Generic_RUNNING, String.Kolor.Green)
				  : String.Color(issue, String.Kolor.Red);
			}
		}

		internal override void Ctrl(bool value)
		{
			if (Lib.Proto.GetBool(protoModule, "deployed"))
			{
				Lib.Proto.Set(protoModule, "running", value);
			}
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
		}
	}


} // KERBALISM
