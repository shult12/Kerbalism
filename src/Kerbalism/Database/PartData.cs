using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KERBALISM
{
	class PartData
	{
		internal uint FlightId { get; private set; }

		internal Drive Drive { get; set; }

		internal PartData(Part part)
		{
			FlightId = part.flightID;
		}

		internal PartData(ProtoPartSnapshot protopart)
		{
			FlightId = protopart.flightID;
		}

		internal void Save(ConfigNode node)
		{
			if (Drive != null)
			{
				ConfigNode driveNode = node.AddNode("drive");
				Drive.Save(driveNode);
			}
		}

		internal void Load(ConfigNode node)
		{
			if (node.HasNode("drive"))
			{
				Drive = new Drive(node.GetNode("drive"));
			}
		}

		/// <summary> Must be called if the part is destroyed </summary>
		internal void OnPartWillDie()
		{
			if (Drive != null)
				Drive.DeleteDriveData();
		}
	}
}
