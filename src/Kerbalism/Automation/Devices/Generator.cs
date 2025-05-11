namespace KERBALISM
{
	sealed class GeneratorDevice : LoadedDevice<ModuleGenerator>
	{
		internal GeneratorDevice(ModuleGenerator module) : base(module) { }

		internal override string Name => "generator";

		internal override string Status
			=> module.isAlwaysActive ? Local.Generic_ALWAYSON : String.Color(module.generatorIsActive, Local.Generic_ON, String.Kolor.Green, Local.Generic_OFF,  String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (module.isAlwaysActive) return;
			if (value) module.Activate();
			else module.Shutdown();
		}

		internal override void Toggle()
		{
			Ctrl(!module.generatorIsActive);
		}

		internal override bool IsVisible => !module.isAlwaysActive;
	}


	sealed class ProtoGeneratorDevice : ProtoDevice<ModuleGenerator>
	{
		internal ProtoGeneratorDevice(ModuleGenerator prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name => "generator";

		internal override string Status
		{
			get
			{
				if (prefab.isAlwaysActive) return Local.Generic_ALWAYSON;
				bool is_on = Lib.Proto.GetBool(protoModule, "generatorIsActive");
				return String.Color(is_on, Local.Generic_ON, String.Kolor.Green, Local.Generic_OFF, String.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value)
		{
			if (prefab.isAlwaysActive) return;
			Lib.Proto.Set(protoModule, "generatorIsActive", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "generatorIsActive"));
		}

		internal override bool IsVisible => !prefab.isAlwaysActive;
	}


} // KERBALISM
