namespace KERBALISM
{
	sealed class ProcessDevice : LoadedDevice<ProcessController>
	{
		internal ProcessDevice(ProcessController module) : base(module) { }

		internal override bool IsVisible => module.toggle;

		internal override string DisplayName => module.title;

		internal override string Tooltip => String.BuildString(base.Tooltip, "\n", String.Bold("Process capacity :"),"\n", module.ModuleInfo);

		internal override string Status => String.Color(module.IsRunning(), Local.Generic_RUNNING, String.Kolor.Green, Local.Generic_STOPPED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			module.SetRunning(value);
		}

		internal override void Toggle()
		{
			Ctrl(!module.IsRunning());
		}
	}

	sealed class ProtoProcessDevice : ProtoDevice<ProcessController>
	{
		internal ProtoProcessDevice(ProcessController prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override bool IsVisible => prefab.toggle;

		internal override string DisplayName => prefab.title;

		internal override string Tooltip => String.BuildString(base.Tooltip, "\n", String.Bold("Process capacity :"), "\n", prefab.ModuleInfo);

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "running"), Local.Generic_RUNNING, String.Kolor.Green, Local.Generic_STOPPED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "running", value);

			double capacity = prefab.capacity;
			var res = protoPart.resources.Find(k => k.resourceName == prefab.resource);
			res.amount = value ? capacity : 0.0;
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
		}
	}


} // KERBALISM



