using System.Collections.Generic;

namespace KERBALISM
{
	sealed class ResourceUnitInfo : IConfigNode
	{
		// We have to disable the "never assigned" warning
		// because ConfigNode deserialization is what actually
		// assigns some of these fields
#pragma warning disable CS0649
		[Persistent]
		public string name;
		public string Name => name;
		[Persistent]
		public string rateUnit;
		public string RateUnit => rateUnit;
		[Persistent]
		public bool useRatePostfix = true;
		public bool UseRatePostfix => useRatePostfix;
		[Persistent]
		public string amountUnit;
		public string AmountUnit => amountUnit;
		[Persistent]
		public double multiplierToUnit = 1d;
		public double MultiplierToUnit => multiplierToUnit;
		[Persistent]
		public bool useHuman = false;
		public bool UseHuman => useHuman;
#pragma warning restore CS0649

		bool isValid;
		internal bool IsValid => isValid;

		internal static readonly int ECResID = "ElectricCharge".GetHashCode();

		static readonly Dictionary<int, ResourceUnitInfo> resourceUnitInfos = new Dictionary<int, ResourceUnitInfo>();

		public void Load(ConfigNode node)
		{
			ConfigNode.LoadObjectFromConfig(this, node);
			if (string.IsNullOrEmpty(rateUnit))
				rateUnit = amountUnit;
			if (!useHuman && useRatePostfix && rateUnit != null)
				rateUnit += Local.Generic_perSecond;

			isValid = !string.IsNullOrEmpty(name) && rateUnit != null && amountUnit != null;
		}

		public void Save(ConfigNode node)
		{
			ConfigNode.CreateConfigFromObject(this, node);
		}

		internal static void LoadResourceUnitInfo()
		{
			resourceUnitInfos.Clear();

			ConfigNode[] defs = GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION");
			foreach (var node in defs)
			{
				var info = new ResourceUnitInfo();
				info.Load(node);
				if (info.IsValid)
					resourceUnitInfos[info.Name.GetHashCode()] = info;
			}
			//Logging.Log("ResourceUnitInfo: Loaded " + resourceUnitInfos.Count + " infos from " + defs.Length + " nodes.");
		}

		internal static ResourceUnitInfo GetResourceUnitInfo(PartResourceDefinition res)
		{
			return GetResourceUnitInfo(res.id);
		}

		internal static ResourceUnitInfo GetResourceUnitInfo(string resName)
		{
			return GetResourceUnitInfo(resName.GetHashCode());
		}

		internal static ResourceUnitInfo GetResourceUnitInfo(int id)
		{
			resourceUnitInfos.TryGetValue(id, out var info);
			return info;
		}
	}
}
