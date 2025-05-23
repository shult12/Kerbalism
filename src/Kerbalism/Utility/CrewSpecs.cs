using System;
using System.Collections.Generic;


namespace KERBALISM
{


	sealed class CrewSpecs
	{
		internal CrewSpecs(string value)
		{
			// if empty or false: not enabled
			if (value.Length == 0 || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
			{
				trait = string.Empty;
				level = 0;
				enabled = false;
				crewOnly = false;
			}
			// if true: enabled, any trait
			else if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
			{
				trait = string.Empty;
				level = 0;
				enabled = true;
				crewOnly = false;
			}
			// if crew only is specified: set the crewOnly bool
			else if (string.Equals(value, "crewonly", StringComparison.OrdinalIgnoreCase))
			{
				trait = string.Empty;
				level = 0;
				enabled = true;
				crewOnly = true;
			}
			// all other cases: enabled, specified trait and experience
			else
			{
				// ModuleManager doesn't like @ in values, so accept Pilot@3 and Pilot:3
				char separator = ':';
				if (value.IndexOf(separator) < 0)
					separator = '@';

				var tokens = String.Tokenize(value, separator);
				trait = tokens.Count > 0 ? tokens[0] : string.Empty;
				level = tokens.Count > 1 ? Parse.ToUInt(tokens[1]) : 0;
				enabled = true;
				crewOnly = false;
			}
		}

		/// <summary>
		/// return true if the crew of active vessel satisfy the specs
		/// </summary>
		bool Check()
		{
			Vessel v = FlightGlobals.ActiveVessel;
			return v != null && Check(v);
		}

		/// <summary>
		/// return true if the crew of specified vessel satisfy the specs
		/// </summary>
		internal bool Check(Vessel v)
		{
			return Check(Lib.CrewList(v));
		}

		/// <summary>
		/// return true if the specified crew satisfy the specs
		/// </summary>
		internal bool Check(List<ProtoCrewMember> crew)
		{
			for (int i = 0; i < crew.Count; ++i)
			{
				if (Check(crew[i])) return true;
			}
			return false;
		}

		/// <summary>
		/// return true if the specified crew member satisfy the specs
		/// </summary>
		bool Check(ProtoCrewMember c)
		{
			if (crewOnly && c.type == ProtoCrewMember.KerbalType.Tourist) return false;
			return trait.Length == 0 || (c.trait == trait && c.experienceLevel >= level);
		}

		/// <summary>
		/// Returns the total crew level bonus (= how many levels above required minimum is the crew).
		/// </summary>
		internal int Bonus(Vessel v, int requiredLevel = Int16.MinValue)
		{
			return Bonus(Lib.CrewList(v), requiredLevel);
		}

		/// <summary>
		/// Returns the total crew level bonus of the given list (= how many levels above required minimum is the crew).
		/// </summary>
		internal int Bonus(List<ProtoCrewMember> crew, int requiredLevel = Int16.MinValue)
		{
			int result = 0;
			for (int i = 0; i < crew.Count; ++i)
			{
				int bonus = Bonus(crew[i], requiredLevel);
				if (bonus > 0) result += bonus;
			}
			return result;
		}

		/// <summary>
		/// Returns the crew level bonus of the given crew member (= how many levels above required minimum is the crew).
		/// </summary>
		int Bonus(ProtoCrewMember c, int requiredLevel = Int16.MinValue)
		{
			if(requiredLevel == Int16.MinValue) {
				requiredLevel = (int)level;
			}
			if (trait.Length == 0 || c.trait != trait) return 0;
			return (int)(c.experienceLevel - requiredLevel);
		}

		/// <summary>
		/// generate a string for use in warning messages
		/// </summary>
		internal string Warning()
		{
			return String.BuildString
			(
			  "<b>",
			  (trait.Length == 0 ? Local.SCIENCEARCHIVE_info_Crew : trait),//"Crew"
			  "</b> ",
			  (level == 0 ? string.Empty : Local.SCIENCEARCHIVE_info_levelReq + " <b>" + level + "</b> "),//of level
			  Local.SCIENCEARCHIVE_info_Req//"is required"
			);
		}

		/// <summary>
		/// generate a string for use in part tooltip
		/// </summary>
		internal string Info()
		{
			if (!enabled) return Local.SCIENCEARCHIVE_info_no;//"no"
			else if (trait.Length == 0) return Local.SCIENCEARCHIVE_info_anyone;//"anyone"
			else return String.BuildString(trait, (level == 0 ? string.Empty : " (" + Local.SCIENCEARCHIVE_info_level + " " + level + ")"));//"level:"
		}

		// can check if enabled by bool comparison
		public static implicit operator bool(CrewSpecs ct)
		{
			return ct.enabled;
		}

		string trait;    // trait specified, or empty for any trait
		uint level;    // experience level specified
		bool enabled;  // can also specify 'disabled' state
		bool crewOnly; // whether it has to be type crew, or whether tourists can be used
	}


} // KERBALISM

