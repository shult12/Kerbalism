using System.Collections.Generic;


namespace KERBALISM
{
	/// <summary>
	/// Contains a list of resources that can be dumped overboard
	/// </summary>
	sealed class DumpSpecs
	{
		// constructor
		/// <summary> Configures the always dump resources and the dump valves, dump valves are only used if always_dump is empty or contains "false" </summary>
		internal DumpSpecs(string always_dump, string dump_valves)
		{
			// if always_dump is empty or false: configure dump valves if any requested
			if (always_dump.Length == 0 || string.Equals(always_dump, "false", System.StringComparison.OrdinalIgnoreCase))
			{
				// if dump_valves is empty or false then don't do anything
				if (dump_valves.Length > 0 && !string.Equals(dump_valves, "false", System.StringComparison.OrdinalIgnoreCase))
				{
					dumpType = DumpType.DumpValve;
					dumpValves.Add(new List<string>());
					dumpValvesTitles.Add("Nothing");

					foreach (string dumpValve in Lib.Tokenize(dump_valves, ','))
					{
						List<string> resources = Lib.Tokenize(dumpValve, '&');
						dumpValves.Add(resources);
						dumpValvesTitles.Add(string.Join(", ", resources));
					}
				}
			}
			// if true: dump everything
			else if (string.Equals(always_dump, "true", System.StringComparison.OrdinalIgnoreCase))
			{
				dumpType = DumpType.AlwaysDump;
			}
			// all other cases: dump only specified resources in always_dump
			else
			{
				dumpType = DumpType.DumpValve;
				dumpValves.Add(Lib.Tokenize(always_dump, ','));
				dumpValvesTitles.Add(always_dump);
			}
		}

		enum DumpType
		{
			NeverDump,
			AlwaysDump,
			DumpValve
		}

		// dump type
		DumpType dumpType = DumpType.NeverDump;
		// list of resource names to be dumped for each dump valve entry
		List<List<string>> dumpValves = new List<List<string>>();
		// UI title of each dump valve (note : can't be localized due to https://github.com/JadeOfMaar/RationalResources/issues/25)
		List<string> dumpValvesTitles = new List<string>();

		internal sealed class ActiveValve
		{
			DumpSpecs _dumpSpecs;
			internal DumpSpecs DumpSpecs => _dumpSpecs;
			int current;

			internal ActiveValve(DumpSpecs dumpSpecs)
			{
				_dumpSpecs = dumpSpecs;
			}

			internal bool CanSwitchValves => _dumpSpecs.dumpValves.Count > 1;

			internal string ValveTitle => current < _dumpSpecs.dumpValvesTitles.Count ? _dumpSpecs.dumpValvesTitles[current] : string.Empty;

			/// <summary> activates or returns the current dump valve index </summary>
			internal int ValveIndex
			{
				get => current;
				set
				{
					if (value < 0 || value >= _dumpSpecs.dumpValves.Count)
						value = 0;

					current = value;
				}
			}

			/// <summary> activates the next dump valve and returns its index </summary>
			internal int NextValve()
			{
				if (_dumpSpecs.dumpValves.Count == 0)
					current = 0;
				else
					current = ++current % _dumpSpecs.dumpValves.Count;

				return current;
			}

			/// <summary> returns true if the specified resource should be dumped </summary>
			internal bool Check(string res_name)
			{
				switch (_dumpSpecs.dumpType)
				{
					case DumpType.NeverDump:
						return false;
					case DumpType.AlwaysDump:
						return true;
					case DumpType.DumpValve:
						List<string> valve = _dumpSpecs.dumpValves[current];
						int i = valve.Count;
						while (i-- > 0)
							if (valve[i] == res_name)
								return true;
						break;
				}

				return false;
			}
		}
	}
} // KERBALISM



