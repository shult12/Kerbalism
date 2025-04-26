using System;
using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	class RuleData
	{
		internal RuleData()
		{
			problem = 0.0;
			message = 0;
			time_since = 0.0;
			lifetime = false;
		}

		internal RuleData(ConfigNode node)
		{
			problem = Lib.ConfigValue(node, "problem", 0.0);
			message = Lib.ConfigValue(node, "message", 0u);
			time_since = Lib.ConfigValue(node, "time_since", 0.0);
			lifetime = Lib.ConfigValue(node, "lifetime", false);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("problem", problem);
			node.AddValue("message", message);
			node.AddValue("time_since", time_since);
			node.AddValue("lifetime", lifetime);
		}

		/// <summary>
		/// Reset process value, except lifetime values
		/// </summary>
		internal void Reset()
		{
			message = 0;
			time_since = 0;
			if (!lifetime) problem = 0.0;
		}

		internal double problem;      // accumulator for the rule
		internal uint message;        // used to avoid sending messages multiple times
		internal double time_since;   // time since last execution, if interval > 0
		internal bool lifetime;       // is this a life time value or not
	}


} // KERBALISM

