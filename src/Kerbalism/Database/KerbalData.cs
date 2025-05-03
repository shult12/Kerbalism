using System;
using System.Collections.Generic;


namespace KERBALISM
{


	class KerbalData
	{
		internal KerbalData()
		{
			rescue = true;
			disabled = false;
			eva_dead = false;
			sickbay = String.Empty;
			rules = new Dictionary<string, RuleData>();
		}

		internal KerbalData(ConfigNode node)
		{
			rescue = Lib.ConfigValue(node, "rescue", Lib.ConfigValue(node, "resque", true)); //< support pre 1.1.9 typo
			disabled = Lib.ConfigValue(node, "disabled", false);
			eva_dead = Lib.ConfigValue(node, "eva_dead", false);
			sickbay = Lib.ConfigValue(node, "sickbay", String.Empty);
			rules = new Dictionary<string, RuleData>();

			foreach (var rule_node in node.GetNode("rules").GetNodes())
			{
				rules.Add(DB.From_safe_key(rule_node.name), new RuleData(rule_node));
			}
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("rescue", rescue);
			node.AddValue("disabled", disabled);
			node.AddValue("eva_dead", eva_dead);
			if (sickbay.Length > 0)
				node.AddValue("sickbay", sickbay);
			
			var rules_node = node.AddNode("rules");
			foreach (var p in rules)
			{
				p.Value.Save(rules_node.AddNode(DB.To_safe_key(p.Key)));
			}
		}

		/// <summary>
		/// Reset all process values
		/// </summary>
		internal void Recover()
		{
			// just retain the lifetime values to keep the save file small
			Dictionary<string, RuleData> cleaned = new Dictionary<string, RuleData>();
			foreach (var p in rules)
			{
				if(p.Value.lifetime) {
					p.Value.Reset();
					cleaned.Add(p.Key, p.Value);
				}
			}
			rules = cleaned;
		}

		internal RuleData Rule(string name)
		{
			if (!rules.ContainsKey(name))
			{
				rules.Add(name, new RuleData());
			}
			return rules[name];
		}

		internal bool rescue;         // used to deal with rescue mission kerbals
		internal bool disabled;       // a generic flag to disable resource consumption, for use by other mods
		internal bool eva_dead;       // the eva kerbal died, and is now a floating body
		internal string sickbay;        // comma-separated list of sickbay resources active for this kerbal
		internal Dictionary<string, RuleData> rules; // rules data
	}


} // KERBALISM

