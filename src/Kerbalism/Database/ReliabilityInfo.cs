using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class ReliabilityInfo
	{
		internal string title { get; private set; }
		internal string group { get; private set; }
		internal bool broken { get; private set; }
		internal bool critical { get; private set; }
		internal uint partId { get; private set; }
		double mtbf { get; set; }
		internal double rel_duration { get; private set; }
		internal double rel_ignitions { get; private set; }

		bool need_maintenance;
		double maintenance_after = 0;

		internal ReliabilityInfo(Reliability module)
		{
			title = String.BuildString(module.part.partInfo.title, String.Color(" " + module.title, String.Kolor.LightGrey));
			group = module.redundancy;
			broken = module.broken;
			critical = module.critical;
			partId = module.part.flightID;
			need_maintenance = module.needMaintenance;
			mtbf = Reliability.EffectiveMTBF(module.quality, module.mtbf);

			if(module.rated_operation_duration > 0)
			{
				rel_duration = module.operation_duration / Reliability.EffectiveDuration(module.quality, module.rated_operation_duration);
				rel_duration = Math.Clamp(rel_duration, 0, 1);
			}

			if(module.rated_ignitions > 0)
			{
				rel_ignitions = (double)module.ignitions / Reliability.EffectiveIgnitions(module.quality, module.rated_ignitions);
				rel_ignitions = Math.Clamp(rel_ignitions, 0, 1);
			}

			if (mtbf > 0)
			{
				maintenance_after = module.last_inspection + mtbf * 0.5;
			}
		}

		internal ReliabilityInfo(ProtoPartSnapshot p, ProtoPartModuleSnapshot m, Reliability module_prefab)
		{
			title = String.BuildString(p.partInfo.title, String.Color(" " + module_prefab.title, String.Kolor.LightGrey));
			group = module_prefab.redundancy;
			broken = Lib.Proto.GetBool(m, "broken", false);
			critical = Lib.Proto.GetBool(m, "critical", false);
			partId = 0;
			need_maintenance = Lib.Proto.GetBool(m, "need_maintenance", false);

			bool quality = Lib.Proto.GetBool(m, "quality", false);

			if (module_prefab.rated_operation_duration > 0)
			{
				var operation_duration = Lib.Proto.GetDouble(m, "operation_duration", 0);
				rel_duration = operation_duration / Reliability.EffectiveDuration(quality, module_prefab.rated_operation_duration);
				rel_duration = Math.Clamp(rel_duration, 0, 1);
			}

			if (module_prefab.rated_ignitions > 0)
			{
				var ignitions = Lib.Proto.GetInt(m, "ignitions", 0);
				rel_ignitions = (double)ignitions / Reliability.EffectiveDuration(quality, module_prefab.rated_ignitions);
				rel_ignitions = Math.Clamp(rel_ignitions, 0, 1);
			}

			mtbf = Reliability.EffectiveMTBF(quality, module_prefab.mtbf);

			if (mtbf > 0)
			{
				var last_inspection = Lib.Proto.GetDouble(m, "last_inspection", 0);
				maintenance_after = last_inspection + mtbf * 0.5;
			}
		}

		internal bool NeedsMaintenance()
		{
			if (maintenance_after > 0 && Planetarium.GetUniversalTime() > maintenance_after) return true;
			return need_maintenance;
		}

		internal static List<ReliabilityInfo> BuildList(Vessel vessel)
		{
			var result = new List<ReliabilityInfo>();

			if (vessel.loaded)
			{
				foreach (var r in Lib.FindModules<Reliability>(vessel))
				{
					result.Add(new ReliabilityInfo(r));
				}
			}
			else
			{
				var PD = new Dictionary<string, Lib.Module_prefab_data>();
				foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
				{
					// get part prefab (required for module properties)
					Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;

					// get all module prefabs
					var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();

					// clear module indexes
					PD.Clear();

					// for each module
					foreach (ProtoPartModuleSnapshot m in p.modules)
					{
						if (m.moduleName != "Reliability") continue;

						Reliability module_prefab = Lib.ModulePrefab(module_prefabs, m.moduleName, PD) as Reliability;
						if (!module_prefab) continue;

						// if the module is disabled, skip it
						// note: this must be done after ModulePrefab is called, so that indexes are right
						if (!Lib.Proto.GetBool(m, "isEnabled")) continue;

						result.Add(new ReliabilityInfo(p, m, module_prefab));
					}
				}
			}

			result.Sort((a, b) => {
				if (a.group != b.group) return string.Compare(a.group, b.group, StringComparison.Ordinal);
				return string.Compare(a.title, b.title);
			});

			return result;
		}
	}
}
