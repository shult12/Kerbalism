using System.Collections.Generic;

namespace KERBALISM
{
	class ResourceBroker
	{
		internal enum BrokerCategory
		{
			Unknown,
			Generator,
			Converter,
			SolarPanel,
			Harvester,
			RTG,
			FuelCell,
			ECLSS,
			VesselSystem,
			Kerbal,
			Comms,
			Science
		}

		static Dictionary<string, ResourceBroker> brokersDict = new Dictionary<string, ResourceBroker>();
		static List<ResourceBroker> brokersList = new List<ResourceBroker>();

		internal static ResourceBroker Generic = GetOrCreate("Others", BrokerCategory.Unknown, Local.Brokers_Others);
		internal static ResourceBroker SolarPanel = GetOrCreate("SolarPanel", BrokerCategory.SolarPanel, Local.Brokers_SolarPanel);
		internal static ResourceBroker KSPIEGenerator = GetOrCreate("KSPIEGenerator", BrokerCategory.Generator, Local.Brokers_KSPIEGenerator);
		internal static ResourceBroker FissionReactor = GetOrCreate("FissionReactor", BrokerCategory.Converter, Local.Brokers_FissionReactor);
		internal static ResourceBroker RTG = GetOrCreate("RTG", BrokerCategory.RTG, Local.Brokers_RTG);
		internal static ResourceBroker ScienceLab = GetOrCreate("ScienceLab", BrokerCategory.Science, Local.Brokers_ScienceLab);
		internal static ResourceBroker Light = GetOrCreate("Light", BrokerCategory.VesselSystem, Local.Brokers_Light);
		internal static ResourceBroker Boiloff = GetOrCreate("Boiloff", BrokerCategory.VesselSystem, Local.Brokers_Boiloff);
		internal static ResourceBroker Cryotank = GetOrCreate("Cryotank", BrokerCategory.VesselSystem, Local.Brokers_Cryotank);
		internal static ResourceBroker Greenhouse = GetOrCreate("Greenhouse", BrokerCategory.VesselSystem, Local.Brokers_Greenhouse);
		internal static ResourceBroker Deploy = GetOrCreate("Deploy", BrokerCategory.VesselSystem, Local.Brokers_Deploy);
		internal static ResourceBroker Experiment = GetOrCreate("Experiment", BrokerCategory.Science, Local.Brokers_Experiment);
		internal static ResourceBroker Command = GetOrCreate("Command", BrokerCategory.VesselSystem, Local.Brokers_Command);
		internal static ResourceBroker GravityRing = GetOrCreate("GravityRing", BrokerCategory.RTG, Local.Brokers_GravityRing);
		internal static ResourceBroker Scanner = GetOrCreate("Scanner", BrokerCategory.VesselSystem, Local.Brokers_Scanner);
		internal static ResourceBroker Laboratory = GetOrCreate("Laboratory", BrokerCategory.Science, Local.Brokers_Laboratory);
		internal static ResourceBroker CommsIdle = GetOrCreate("CommsIdle", BrokerCategory.Comms, Local.Brokers_CommsIdle);
		internal static ResourceBroker CommsXmit = GetOrCreate("CommsXmit", BrokerCategory.Comms, Local.Brokers_CommsXmit);
		internal static ResourceBroker StockConverter = GetOrCreate("StockConverter", BrokerCategory.Converter, Local.Brokers_StockConverter);
		internal static ResourceBroker StockDrill = GetOrCreate("Converter", BrokerCategory.Harvester, Local.Brokers_StockDrill);
		internal static ResourceBroker Harvester = GetOrCreate("Harvester", BrokerCategory.Harvester, Local.Brokers_Harvester);

		internal string Id { get; private set; }
		internal BrokerCategory Category { get; private set; }
		internal string Title { get; private set; }
		internal string[] BrokerInfo { get; private set; }

		public override int GetHashCode() => hashcode;
		int hashcode;

		ResourceBroker(string id, BrokerCategory category = BrokerCategory.Unknown, string title = null)
		{
			Id = id;
			Category = category;

			if (string.IsNullOrEmpty(title))
				Title = id;
			else
				Title = title;

			BrokerInfo = new string[] { Category.ToString(), Id, Title };

			hashcode = id.GetHashCode();

			brokersDict.Add(id, this);
			brokersList.Add(this);
		}

		static IEnumerator<ResourceBroker> List()
		{
			return brokersList.GetEnumerator();
		}

		internal static ResourceBroker GetOrCreate(string id)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb;

			return new ResourceBroker(id, BrokerCategory.Unknown, id);
		}

		internal static ResourceBroker GetOrCreate(string id, BrokerCategory type, string title)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb;

			return new ResourceBroker(id, type, title);
		}

		static string GetTitle(string id)
		{
			ResourceBroker rb;
			if (brokersDict.TryGetValue(id, out rb))
				return rb.Title;
			return null;
		}
	}
}
