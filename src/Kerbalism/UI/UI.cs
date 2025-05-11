using System;
using UnityEngine;

namespace KERBALISM
{
	static class UI
	{
		static Message message;
		static Launcher launcher;
		internal static Window window;

		internal static void Init()
		{
			// create subsystems
			message = new Message();
			launcher = new Launcher();
			window = new Window((uint)Styles.ScaleWidthFloat(300), 0, 0);
		}

		internal static void Sync()
		{
			window.Position(DB.ui.win_left, DB.ui.win_top);
		}

		internal static void Update(bool show_window)
		{
			// if gui should be shown
			if (show_window)
			{
				// as a special case, the first time the user enter
				// map-view/tracking-station we open the body info window
				if (MapView.MapIsEnabled && !DB.ui.map_viewed)
				{
					Open(BodyInfo.Body_info);
					DB.ui.map_viewed = true;
				}

				// update subsystems
				launcher.Update();
				window.Update();

				// remember main window position
				DB.ui.win_left = window.Left();
				DB.ui.win_top = window.Top();
			}

			// re-enable camera mouse scrolling, as some of the on_gui functions can
			// disable it on mouse-hover, but can't re-enable it again consistently
			// (eg: you mouse-hover and then close the window with the cursor still inside it)
			// - we are ignoring user preference on mouse wheel
			GameSettings.AXIS_MOUSEWHEEL.primary.scale = 1.0f;
		}

		internal static void On_gui(bool show_window)
		{
			// render subsystems
			message.On_gui();
			if (show_window)
			{
				launcher.On_gui();
				window.On_gui();
			}
		}

		internal static void Open(Action<Panel> refresh)
		{
			window.Open(refresh);
		}


		#region UI Lib
		/// <summary>Trigger a planner update</summary>
		internal static void RefreshPlanner()
		{
			Planner.Planner.RefreshPlanner();
		}

		///<summary>return true if last GUILayout element was clicked</summary>
		internal static bool IsClicked(int button = 0)
		{
			return Event.current.type == EventType.MouseDown
				&& Event.current.button == button
				&& GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
		}

		///<summary>return true if the mouse is inside the last GUILayout element</summary>
		internal static bool IsHover()
		{
			return GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
		}

		///<summary>
		/// render a text field with placeholder
		/// - id: an unique name for the text field
		/// - text: the previous text field content
		/// - placeholder: the text to show if the content is empty
		/// - style: GUIStyle to use for the text field
		///</summary>
		internal static string TextFieldPlaceholder(string id, string text, string placeholder, GUIStyle style)
		{
			GUI.SetNextControlName(id);
			text = GUILayout.TextField(text, style);

			if (Event.current.type == EventType.Repaint)
			{
				if (GUI.GetNameOfFocusedControl() == id)
				{
					if (text == placeholder) text = "";
				}
				else
				{
					if (text.Length == 0) text = placeholder;
				}
			}
			return text;
		}

		///<summary>used to make rmb ui status toggles look all the same</summary>
		internal static string StatusToggle(string title, string status)
		{
			return String.BuildString("<b>", title, "</b>: ", status);
		}


		///<summary>show a modal popup window where the user can choose among two options</summary>
		internal static PopupDialog Popup(string title, string msg, params DialogGUIBase[] buttons)
		{
			return PopupDialog.SpawnPopupDialog
			(
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new MultiOptionDialog(title, msg, title, HighLogic.UISkin, buttons),
				false,
				HighLogic.UISkin,
				true,
				string.Empty
			);
		}

		internal static PopupDialog Popup(string title, string msg, float width, params DialogGUIBase[] buttons)
		{
			return PopupDialog.SpawnPopupDialog
			(
				new Vector2(0.5f, 0.5f),
				new Vector2(0.5f, 0.5f),
				new MultiOptionDialog(title, msg, title, HighLogic.UISkin, width, buttons),
				false,
				HighLogic.UISkin,
				true,
				string.Empty
			);
		}

		internal static string Greek()
		{
			string[] letters = {
				"Alpha",
				"Beta",
				"Gamma",
				"Delta",
				"Epsilon",
				"Zeta",
				"Eta",
				"Theta",
				"Iota",
				"Kappa",
				"Lambda",
				"Mu",
				"Nu",
				"Xi",
				"Omicron",
				"Pi",
				"Sigma",
				"Tau",
				"Upsilon",
				"Phi",
				"Chi",
				"Psi",
				"Omega"
			};
			System.Random rand = new System.Random();
			int index = rand.Next(letters.Length);
			return (string)letters[index];
		}
		#endregion
	}
} // KERBALISM

