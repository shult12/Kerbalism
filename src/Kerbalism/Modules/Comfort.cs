using System;
using System.Collections.Generic;


namespace KERBALISM
{


	class Comfort : PartModule, ISpecifics
	{
		// config+persistence
		[KSPField(isPersistant = true)] public string bonus = string.Empty; // the comfort bonus provided

		// config
		[KSPField] public string desc = string.Empty;                       // short description shown in part tooltip


		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;
		}


		public override string GetInfo()
		{
			return Specs().Info(desc);
		}

		// specifics support
		public Specifics Specs()
		{
			Specifics specs = new Specifics();
			specs.Add("bonus", bonus);
			return specs;
		}

		public override string GetModuleDisplayName() { return Local.Module_Comfort; }//"Comfort"
	}


	class Comforts
	{
		internal Comforts(Vessel v, bool env_firm_ground, bool env_not_alone, bool env_call_home)
		{
			// environment factors
			firm_ground = env_firm_ground;
			not_alone = env_not_alone;
			call_home = env_call_home;

			// if loaded
			if (v.loaded)
			{
				// scan parts for comfort
				foreach (Comfort c in Lib.FindModules<Comfort>(v))
				{
					switch (c.bonus)
					{
						case "firm-ground": firm_ground = true; break;
						case "not-alone": not_alone = true; break;
						case "call-home": call_home = true; break;
						case "exercise": exercise = true; break;
						case "panorama": panorama = true; break;
						case "plants": plants = true; break;
					}
				}

				// scan parts for gravity ring
				if (Lib.IsPowered(v))
				{
					firm_ground |= Lib.HasModule<GravityRing>(v, k => k.deployed);
				}
			}
			// if not loaded
			else
			{
				// scan parts for comfort
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Comfort"))
				{
					switch (Lib.Proto.GetString(m, "bonus"))
					{
						case "firm-ground": firm_ground = true; break;
						case "not-alone": not_alone = true; break;
						case "call-home": call_home = true; break;
						case "exercise": exercise = true; break;
						case "panorama": panorama = true; break;
						case "plants": plants = true; break;
					}
				}

				// scan parts for gravity ring
				if (Lib.IsPowered(v))
				{
					firm_ground |= Lib.HasModule(v.protoVessel, "GravityRing", k => Lib.Proto.GetBool(k, "deployed"));
				}
			}

			// calculate factor
			factor = 0.1;
			if (firm_ground) factor += PreferencesComfort.Instance.firmGround;
			if (not_alone) factor += PreferencesComfort.Instance.notAlone;
			if (call_home) factor += PreferencesComfort.Instance.callHome;
			if (exercise) factor += PreferencesComfort.Instance.exercise;
			if (panorama) factor += PreferencesComfort.Instance.panorama;
			if (plants) factor += PreferencesComfort.Instance.plants;
			factor = Math.Clamp(factor, 0.1, 1.0);
		}


		internal Comforts(List<Part> parts, bool env_firm_ground, bool env_not_alone, bool env_call_home)
		{
			// environment factors
			firm_ground = env_firm_ground;
			not_alone = env_not_alone;
			call_home = env_call_home;

			// for each parts
			foreach (Part p in parts)
			{
				// for each modules in part
				foreach (PartModule m in p.Modules)
				{
					// skip disabled modules
					if (!m.isEnabled) continue;

					// comfort
					if (m.moduleName == "Comfort") 
					{
						Comfort c = m as Comfort;
						switch (c.bonus)
						{
							case "firm-ground": firm_ground = true; break;
							case "not-alone": not_alone = true; break;
							case "call-home": call_home = true; break;
							case "exercise": exercise = true; break;
							case "panorama": panorama = true; break;
							case "plants": plants = true; break;
						}
					}
					// gravity ring
					// - ignoring if ec is present or not here
					else if (m.moduleName == "GravityRing")
					{
						GravityRing ring = m as GravityRing;
						firm_ground |= ring.deployed;
					}
				}
			}

			// calculate factor
			factor = 0.1;
			if (firm_ground) factor += PreferencesComfort.Instance.firmGround;
			if (not_alone) factor += PreferencesComfort.Instance.notAlone;
			if (call_home) factor += PreferencesComfort.Instance.callHome;
			if (exercise) factor += PreferencesComfort.Instance.exercise;
			if (panorama) factor += PreferencesComfort.Instance.panorama;
			factor = Math.Clamp(factor, 0.1, 1.0);
		}



		internal string Tooltip()
		{
			string yes = String.BuildString("<b><color=#00ff00>", Local.Generic_YES, " </color></b>");
			string no = String.BuildString("<b><color=#ffaa00>", Local.Generic_NO, " </color></b>");
			return String.BuildString
			(
				"<align=left />",
				string.Format("{0,-14}\t{1}\n", Local.Comfort_firmground, firm_ground ? yes : no),
				string.Format("{0,-14}\t{1}\n", Local.Comfort_exercise, exercise ? yes : no),
				string.Format("{0,-14}\t{1}\n", Local.Comfort_notalone, not_alone ? yes : no),
				string.Format("{0,-14}\t{1}\n", Local.Comfort_callhome, call_home ? yes : no),
				string.Format("{0,-14}\t{1}\n", Local.Comfort_panorama, panorama ? yes : no),
				string.Format("{0,-14}\t{1}\n", Local.Comfort_plants, plants ? yes : no),
				string.Format("<i>{0,-14}</i>\t{1}", Local.Comfort_factor, HumanReadable.Percentage(factor))
			);
		}

		internal string Summary()
		{
			if (factor >= 0.99) return Local.Module_Comfort_Summary1;//"ideal"
			else if (factor >= 0.66) return Local.Module_Comfort_Summary2;//"good"
			else if (factor >= 0.33) return Local.Module_Comfort_Summary3;//"modest"
			else if (factor > 0.1) return Local.Module_Comfort_Summary4;//"poor"
			else return Local.Module_Comfort_Summary5;//"none"
		}

		bool firm_ground;
		bool exercise;
		bool not_alone;
		bool call_home;
		bool panorama;
		bool plants;
		internal double factor;
	}


} // KERBALISM
