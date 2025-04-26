using System;
using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	class LandmarkData
	{
		internal LandmarkData()
		{
		}

		internal LandmarkData(ConfigNode node)
		{
			belt_crossing = Lib.ConfigValue(node, "belt_crossing", false);
			manned_orbit = Lib.ConfigValue(node, "manned_orbit", false);
			space_harvest = Lib.ConfigValue(node, "space_harvest", false);
			space_analysis = Lib.ConfigValue(node, "space_analysis", false);
			heliopause_crossing = Lib.ConfigValue(node, "heliopause_crossing", false);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("belt_crossing", belt_crossing);
			node.AddValue("manned_orbit", manned_orbit);
			node.AddValue("space_harvest", space_harvest);
			node.AddValue("space_analysis", space_analysis);
			node.AddValue("heliopause_crossing", heliopause_crossing);
		}

		internal bool belt_crossing;        // record first belt crossing
		internal bool manned_orbit;         // record first 30 days manned orbit
		internal bool space_harvest;        // record first greenhouse harvest in space
		internal bool space_analysis;       // record first lab sample analysis in space
		internal bool heliopause_crossing;  // record first heliopause crossing
	}


} // KERBALISM





