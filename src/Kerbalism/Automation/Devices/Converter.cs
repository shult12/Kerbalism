using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;


namespace KERBALISM
{
	sealed class ConverterDevice : LoadedDevice<ModuleResourceConverter>
	{
		internal ConverterDevice(ModuleResourceConverter module) : base(module) { }

		internal override string Status => module.AlwaysActive ? Local.Generic_ALWAYSON : Lib.Color(module.IsActivated, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (module.AlwaysActive) return;
			if (value) module.StartResourceConverter();
			else module.StopResourceConverter();
		}

		internal override void Toggle()
		{
			Ctrl(!module.IsActivated);
		}
	}


	sealed class ProtoConverterDevice : ProtoDevice<ModuleResourceConverter>
	{
		internal ProtoConverterDevice(ModuleResourceConverter prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Status
		{
			get
			{
				if (prefab.AlwaysActive) return Local.Generic_ALWAYSON;
				bool is_on = Lib.Proto.GetBool(protoModule, "IsActivated");
				return Lib.Color(is_on, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value)
		{
			if (prefab.AlwaysActive) return;
			Lib.Proto.Set(protoModule, "IsActivated", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "IsActivated"));
		}
	}


} // KERBALISM
