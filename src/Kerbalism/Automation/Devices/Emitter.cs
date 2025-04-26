using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;


namespace KERBALISM
{
	sealed class EmitterDevice : LoadedDevice<Emitter>
	{
		internal EmitterDevice(Emitter module) : base(module) { }

		internal override string Name => "emitter";

		internal override string Status => Lib.Color(module.running, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

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

		internal override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, "running"), Local.Generic_ACTIVE, Lib.Kolor.Green, Local.Generic_DISABLED, Lib.Kolor.Yellow);

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
