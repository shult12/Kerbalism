namespace KERBALISM
{
	sealed class EmitterDevice : LoadedDevice<Emitter>
	{
		internal EmitterDevice(Emitter module) : base(module) { }

		internal override string Name => "emitter";

		internal override string Status => String.Color(module.running, Local.Generic_ON, String.Kolor.Green, Local.Generic_OFF, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (!module.toggle) return;
			if (module.running != value) module.Toggle();
		}

		internal override void Toggle()
		{
			Ctrl(!module.running);
		}

		internal override bool IsVisible => module.toggle;
	}

	sealed class ProtoEmitterDevice : ProtoDevice<Emitter>
	{
		internal ProtoEmitterDevice(Emitter prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name => "emitter";

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "running"), Local.Generic_ACTIVE, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "running", value);
		}

		internal override void Toggle()
		{
			if (!Lib.Proto.GetBool(protoModule, "toggle")) return;
			Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
		}

		internal override bool IsVisible => Lib.Proto.GetBool(protoModule, "toggle");
	}


} // KERBALISM
