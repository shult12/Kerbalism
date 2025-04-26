using System;


namespace KERBALISM
{


	class UIData
	{
		internal UIData()
		{
			win_left = 280u;
			win_top = 100u;
			map_viewed = false;
		}

		internal UIData(ConfigNode node)
		{
			win_left = Lib.ConfigValue(node, "win_left", 280u);
			win_top = Lib.ConfigValue(node, "win_top", 100u);
			map_viewed = Lib.ConfigValue(node, "map_viewed", false);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("win_left", win_left);
			node.AddValue("win_top", win_top);
			node.AddValue("map_viewed", map_viewed);
		}

		internal uint win_left;       // popout window position left
		internal uint win_top;        // popout window position top
		internal bool map_viewed;     // has the user entered map-view/tracking-station
	}


} // KERBALISM
