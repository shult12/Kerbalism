using System;
using UnityEngine;

namespace KERBALISM
{


	abstract class Device
	{
		internal class DeviceIcon
		{
			internal Texture2D texture;
			internal string tooltip;
			internal Action onClick;

			internal DeviceIcon(Texture2D texture, string tooltip = "", Action onClick = null)
			{
				this.texture = texture;
				this.tooltip = tooltip;
				this.onClick = onClick;
			}
		}

		protected Device()
		{
			DeviceType = GetType().Name;
		}

		// note 1 : the Id must be unique and always the same (persistence), so the Name property must always be the
		// same, and be unique in case multiple modules of the same type exists on the part.
		// note 2 : dynamically generate the id when first requested.
		// can't do it in the base ctor because the PartId and Name may be overloaded.
		internal uint Id
		{
			get
			{
				if (id == uint.MaxValue)
					id = PartId + (uint)System.Math.Abs(Name.GetHashCode());

				return id;
			}
		}
		uint id = uint.MaxValue; // lets just hope nothing will ever have that id

		internal string DeviceType { get; private set; }

		// return device name, must be static and unique in case several modules of the same type are on the part
		internal abstract string Name { get; }

		// the name that will be displayed. can be overloaded in case some dynamic text is added (see experiments)
		internal virtual string DisplayName => Name;

		// return part id
		internal abstract uint PartId { get; }

		// return part name
		internal abstract string PartName { get; }

		// return short device status string
		internal abstract string Status { get; }

		// return tooltip string
		internal virtual string Tooltip => Lib.BuildString(Lib.Bold(DisplayName), "\non ", PartName);

		// return icon/button
		internal virtual DeviceIcon Icon => null;

		// control the device using a value
		internal abstract void Ctrl(bool value);

		// toggle the device state
		internal abstract void Toggle();

		internal virtual bool IsVisible => true;

		internal virtual void OnUpdate() { }
	}

	abstract class LoadedDevice<T> : Device where T : PartModule
	{
		protected readonly T module;

		protected LoadedDevice(T module) : base()
		{
			this.module = module;
		}

		internal override string PartName => module.part.partInfo.title;
		internal override string Name => module is IModuleInfo ? ((IModuleInfo)module).GetModuleTitle() : module.GUIName;
		internal override uint PartId => module.part.flightID;
	}

	abstract class ProtoDevice<T> : Device where T : PartModule
	{
		protected readonly T prefab;
		protected readonly ProtoPartSnapshot protoPart;
		protected readonly ProtoPartModuleSnapshot protoModule;

		protected ProtoDevice(T prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule) : base()
		{
			this.prefab = prefab;
			this.protoPart = protoPart;
			this.protoModule = protoModule;
		}

		internal override string PartName => prefab.part.partInfo.title;
		internal override string Name => prefab is IModuleInfo ? ((IModuleInfo)prefab).GetModuleTitle() : prefab.GUIName;
		internal override uint PartId => protoPart.flightID;
	}

	abstract class VesselDevice : Device
	{
		readonly Vessel vessel;
		protected readonly VesselData vesselData;

		protected VesselDevice(Vessel v, VesselData vd) : base()
		{
			vessel = v;
			vesselData = vd;
		}

		internal override uint PartId => 0u;
		internal override string PartName => string.Empty;
		internal override string Tooltip => Lib.Bold(DisplayName);
	}


} // KERBALISM
