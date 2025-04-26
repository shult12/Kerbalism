using System;
using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{
	class SupplyData
	{
		internal SupplyData()
		{
			message = 0;
		}

		internal SupplyData(ConfigNode node)
		{
			message = Lib.ConfigValue(node, "message", 0u);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("message", message);
		}

		internal uint message;  // used to avoid sending messages multiple times
		internal List<ResourceBrokerRate> ResourceBrokers { get; private set; } = new List<ResourceBrokerRate>();
		static string LocAveragecache;

		internal class ResourceBrokerRate
		{
			internal ResourceBroker broker;
			internal double rate;
			internal ResourceBrokerRate(ResourceBroker broker, double amount)
			{
				this.broker = broker;
				this.rate = amount;
			}
		}

		internal void UpdateResourceBrokers(Dictionary<ResourceBroker, double> brokersResAmount, Dictionary<ResourceBroker, double> ruleBrokersRate, double unsupportedBrokersRate, double elapsedSeconds)
		{
			ResourceBrokers.Clear();

			foreach (KeyValuePair<ResourceBroker, double> p in ruleBrokersRate)
			{
				ResourceBroker broker = ResourceBroker.GetOrCreate(p.Key.Id + "Avg", p.Key.Category, Lib.BuildString(p.Key.Title, " (", Local.Generic_AVERAGE, ")"));
				ResourceBrokers.Add(new ResourceBrokerRate(broker, p.Value));
			}
			foreach (KeyValuePair<ResourceBroker, double> p in brokersResAmount)
			{
				ResourceBrokers.Add(new ResourceBrokerRate(p.Key, p.Value / elapsedSeconds));
			}
			if (unsupportedBrokersRate != 0.0)
			{
				ResourceBrokers.Add(new ResourceBrokerRate(ResourceBroker.Generic, unsupportedBrokersRate)); 
			}
		}
	}



} // KERBALISM
