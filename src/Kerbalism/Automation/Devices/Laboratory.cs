namespace KERBALISM
{
	sealed class LaboratoryDevice : LoadedDevice<Laboratory>
	{
		internal LaboratoryDevice(Laboratory module) : base(module) { }

		internal override string Status => String.Color(module.running, Local.Generic_ACTIVE, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (module.running != value) module.Toggle();
		}

		internal override void Toggle()
		{
			Ctrl(!module.running);
		}
	}


	sealed class ProtoLaboratoryDevice : ProtoDevice<Laboratory>
	{
		internal ProtoLaboratoryDevice(Laboratory prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "running"), Local.Generic_ACTIVE, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "running", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
		}
	}


} // KERBALISM

