using System;

namespace KERBALISM
{
	static class ModuleManager
	{
		internal static int MM_major;
		internal static int MM_minor;
		internal static int MM_rev;
		static ModuleManager()
		{
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				if (a.name == "ModuleManager")
				{
					Version v = a.assembly.GetName().Version;
					MM_major = v.Major;
					MM_minor = v.Minor;
					MM_rev = v.Revision;
					break;
				}
			}
		}
	}

}
