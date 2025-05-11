using System;
using System.Reflection;
using UnityEngine;

namespace KERBALISM
{
	static class BackgroundResources
	{
		static Type deepFreezeParamsType;
		static bool deepFreezeIsInstalled = false;
		static Type taclsParamsType;
		static bool taclsIsInstalled = false;

		static bool? isInstalled;
		static bool IsInstalled
		{
			get
			{
				if (isInstalled == null)
				{
					foreach (var a in AssemblyLoader.loadedAssemblies)
					{
						if (a.name == "BackgroundResources")
						{
							isInstalled = true;
						}
						else if (a.name == "DeepFreeze")
						{
							try
							{
								deepFreezeParamsType = a.assembly.GetType("DF.DeepFreeze_SettingsParms");
								deepFreezeIsInstalled = true;
							}
							catch (Exception)
							{
								deepFreezeIsInstalled = false;
								Logging.Log("DeepFreeze is installed but the CustomParameterNode type wasn't found, make sure the \"Unloaded Vessel Processing\" DeepFreeze difficulty setting is disabled", Logging.LogLevel.Warning);
							}
						}
						else if (a.name == "TacLifeSupport")
						{
							try
							{
								taclsParamsType = a.assembly.GetType("Tac.TAC_SettingsParms");
								taclsIsInstalled = true;
							}
							catch (Exception)
							{
								taclsIsInstalled = false;
								Logging.Log("TAC-LS is installed but the CustomParameterNode type wasn't found, make sure the \"Unloaded Vessel Processing\" TAC-LS difficulty setting is disabled", Logging.LogLevel.Warning);
							}
						}
					}

					if (isInstalled == null)
						isInstalled = false;
				}

				return (bool)isInstalled;
			}
		}

		internal static void DisableBackgroundResources()
		{
			if (IsInstalled)
			{
				if (deepFreezeIsInstalled)
				{
					try
					{
						GameParameters.CustomParameterNode paramNode = HighLogic.CurrentGame.Parameters.CustomParams(deepFreezeParamsType);
						FieldInfo bgEnabled = paramNode.GetType().GetField("backgroundresources");
						bool isBGEnabled = (bool)bgEnabled.GetValue(paramNode);
						if (isBGEnabled)
						{
							ShowPopup("DeepFreeze");
							bgEnabled.SetValue(paramNode, false);
						}
					}
					catch (Exception)
					{
						Logging.Log("DeepFreeze is installed but couldn't disable BackgroundResources, make sure the \"Unloaded Vessel Processing\" DeepFreeze difficulty setting is disabled", Logging.LogLevel.Warning);
					}
				}

				if (taclsIsInstalled)
				{
					try
					{
						GameParameters.CustomParameterNode paramNode = HighLogic.CurrentGame.Parameters.CustomParams(taclsParamsType);
						FieldInfo bgEnabled = paramNode.GetType().GetField("backgroundresources");
						bool isBGEnabled = (bool)bgEnabled.GetValue(paramNode);
						if (isBGEnabled)
						{
							ShowPopup("TAC-LS");
							bgEnabled.SetValue(paramNode, false);
						}
					}
					catch (Exception)
					{
						Logging.Log("TAC-LS is installed but we couldn't disable BackgroundResources, make sure the \"Unloaded Vessel Processing\" TAC-LS difficulty setting is disabled", Logging.LogLevel.Warning);
					}
				}


			}
		}

		static void ShowPopup(string modName)
		{
			string title = "Kerbalism compatibility notice";
			string msg = String.Color($"{modName} has been detected in your game.\n\nFor it to be compatible with Kerbalism, the \"Unloaded Vessel Processing\" {modName} difficulty setting option has been automatically disabled.", String.Kolor.Yellow, true);
			PopupDialog.SpawnPopupDialog
			(
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new MultiOptionDialog(modName, msg, title, HighLogic.UISkin, 350f, new DialogGUIButton("OK", null, true)),
				false,
				HighLogic.UISkin,
				true,
				string.Empty
			);
		}


	}
}
