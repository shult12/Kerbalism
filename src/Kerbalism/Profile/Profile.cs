using System;
using System.Collections.Generic;


namespace KERBALISM
{


	static class Profile
	{

		// node parsing
		static void Nodeparse(ConfigNode profile_node)
		{
			// parse all rules
			foreach (ConfigNode rule_node in profile_node.GetNodes("Rule"))
			{
				try
				{
					// parse rule
					Rule rule = new Rule(rule_node);

					// ignore duplicates
					if (rules.Find(k => k.name == rule.name) == null)
					{
						// add the rule
						rules.Add(rule);
					}
				}
				catch (Exception e)
				{
					Logging.Log("failed to load rule\n" + e.ToString(), Logging.LogLevel.Warning);
				}
			}

			// parse all supplies
			foreach (ConfigNode supply_node in profile_node.GetNodes("Supply"))
			{
				try
				{
					// parse supply
					Supply supply = new Supply(supply_node);

					// ignore duplicates
					if (supplies.Find(k => k.resource == supply.resource) == null)
					{
						// add the supply
						supplies.Add(supply);
					}
				}
				catch (Exception e)
				{
					Logging.Log("failed to load supply\n" + e.ToString(), Logging.LogLevel.Warning);
				}
			}

			// parse all processes
			foreach (ConfigNode process_node in profile_node.GetNodes("Process"))
			{
				try
				{
					// parse process
					Process process = new Process(process_node);

					// ignore duplicates
					if (processes.Find(k => k.name == process.name) == null)
					{
						// add the process
						processes.Add(process);
					}
				}
				catch (Exception e)
				{
					Logging.Log("failed to load process\n" + e.ToString(), Logging.LogLevel.Warning);
				}
			}
		}

		// Support config file parsing
		static void ParseSupport()
		{
			// for each profile
			foreach (ConfigNode profile_node in Lib.ParseConfigs("Profile"))
			{
				// get the name
				string name = Lib.ConfigValue(profile_node, "name", string.Empty);

				// if this is a Kerbalism Support profile
				if (name == "KerbalismSupport")
				{
					// get the mod name and directory
					string modname = Lib.ConfigValue(profile_node, "modname", string.Empty);
					string moddir = Lib.ConfigValue(profile_node, "moddir", string.Empty);

					// if the mods directory exists
					if (Lib.GameDirectoryExist(moddir))
					{
						// log profile and mod name
						Logging.Log(Lib.BuildString("importing Kerbalism Support profile for mod: ", modname));

						// parse nodes
						Nodeparse(profile_node);

						// done a Support profile now on to the next
					}
				}
			}
		}

		internal static void Parse()
		{
			// initialize data
			rules = new List<Rule>();
			supplies = new List<Supply>();
			processes = new List<Process>();

			// if a profile is specified
			if (Settings.Profile.Length > 0)
			{
				// for each profile config
				foreach (ConfigNode profile_node in Lib.ParseConfigs("Profile"))
				{
					// get the name
					string name = Lib.ConfigValue(profile_node, "name", string.Empty);
					// if this is the one chosen in settings
					if (name == Settings.Profile)
					{
						// log profile name
						Logging.Log(Lib.BuildString("using profile: ", Settings.Profile));

						// parse nodes
						Nodeparse(profile_node);

						// Add support configs
						ParseSupport();

						// log info
						Logging.Log("supplies:");
						foreach (Supply supply in supplies) Logging.Log(Lib.BuildString("- ", supply.resource));
						if (supplies.Count == 0) Logging.Log("- none");
						Logging.Log("rules:");
						foreach (Rule rule in rules) Logging.Log(Lib.BuildString("- ", rule.name));
						if (rules.Count == 0) Logging.Log("- none");
						Logging.Log("processes:");
						foreach (Process process in processes) Logging.Log(Lib.BuildString("- ", process.name));
						if (processes.Count == 0) Logging.Log("- none");

						// we are done here
						return;
					}
				}
				// if we reach this point, the profile was not found
				Logging.Log(Lib.BuildString("profile '", Settings.Profile, "' was not found"), Logging.LogLevel.Warning);
			}
		}


		internal static void Execute(Vessel v, VesselData vd, VesselResources resources, double elapsed_s)
		{
			// execute all supplies
			foreach (Supply supply in supplies)
			{
				// this will just show warning messages if resources get low
				supply.Execute(v, vd, resources);
			}

			// execute all rules
			foreach (Rule rule in rules)
			{
				rule.Execute(v, vd, resources, elapsed_s);
			}

			// execute all processes
			foreach (Process process in processes)
			{
				process.Execute(v, vd, resources, elapsed_s);
			}
		}


		internal static void SetupPods()
		{
			// add supply resources to all pods
			foreach (AvailablePart p in PartLoader.LoadedPartsList)
			{
				foreach (Supply supply in supplies)
				{
					supply.SetupPod(p);
				}
			}
		}


		internal static void SetupEva(Part p)
		{
			foreach (Supply supply in supplies)
			{
				supply.SetupEva(p);
			}
		}


		internal static void SetupRescue(Vessel v)
		{
			foreach (Supply supply in supplies)
			{
				supply.SetupRescue(v);
			}
		}


		internal static List<Rule> rules;               // rules in the profile
		internal static List<Supply> supplies;          // supplies in the profile
		internal static List<Process> processes;        // processes in the profile
	}


} // KERBALISM
