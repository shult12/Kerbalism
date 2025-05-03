using System;
using UnityEngine;


namespace KERBALISM
{
	// a window containing a panel
	sealed class Window
	{
		// click through locks
		bool clickThroughLocked = false;
		const ControlTypes WindowLockTypes = ControlTypes.MANNODE_ADDEDIT | ControlTypes.MANNODE_DELETE | ControlTypes.MAP_UI |
			ControlTypes.TARGETING | ControlTypes.VESSEL_SWITCHING | ControlTypes.TWEAKABLES | ControlTypes.EDITOR_UI | ControlTypes.EDITOR_SOFT_LOCK | ControlTypes.UI;

		// - width: window width in pixel
		// - left: initial window horizontal position
		// - top: initial window vertical position
		internal Window(uint width, uint left, uint top)
		{
			// generate unique id
			win_id = Lib.RandomInt(int.MaxValue);

			// setup window geometry
			win_rect = new Rect((float)left, (float)top, (float)width, 0.0f);

			// setup dragbox geometry
			drag_rect = new Rect(0.0f, 0.0f, (float)width, Styles.ScaleFloat(20.0f));

			// initialize tooltip utility
			tooltip = new Tooltip();
		}

		internal void Open(Action<Panel> refresh)
		{
			this.refresh = refresh;
		}

		internal void Close()
		{
			// clear input locks
			InputLockManager.RemoveControlLock("KerbalismWindowLock");
			InputLockManager.RemoveControlLock("KerbalismMainGUILock");

			refresh = null;
			panel = null;
		}

		internal void Update()
		{
			if (refresh != null)
			{
				// initialize or clear panel
				if (panel == null) panel = new Panel();
				else panel.Clear();

				// refresh panel content
				refresh(panel);

				// if panel is empty, close the window
				if (panel.Empty())
				{
					Close();
				}
			}
		}

		internal void On_gui()
		{
			// window is considered closed if panel is null
			if (panel == null) return;

			// adapt window size to panel
			// - clamp to screen height
			win_rect.width = Math.Min(panel.Width(), Screen.width * 0.8f);
			win_rect.height = Math.Min(Styles.ScaleFloat(20.0f) + panel.Height(), Screen.height * 0.8f);

			// clamp the window to the screen, so it can't be dragged outside
			float offset_x = Math.Max(0.0f, -win_rect.xMin) + Math.Min(0.0f, Screen.width - win_rect.xMax);
			float offset_y = Math.Max(0.0f, -win_rect.yMin) + Math.Min(0.0f, Screen.height - win_rect.yMax);
			win_rect.xMin += offset_x;
			win_rect.xMax += offset_x;
			win_rect.yMin += offset_y;
			win_rect.yMax += offset_y;

			// draw the window
			win_rect = GUILayout.Window(win_id, win_rect, Draw_window, "", Styles.win);

			// get mouse over state
			//bool mouse_over = win_rect.Contains(Event.current.mousePosition);
			bool mouse_over = win_rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

			// disable camera mouse scrolling on mouse over
			if (mouse_over)
			{
				GameSettings.AXIS_MOUSEWHEEL.primary.scale = 0.0f;
			}

			// Disable Click through
			if (mouse_over && !clickThroughLocked)
			{
				InputLockManager.SetControlLock(WindowLockTypes, "KerbalismWindowLock");
				clickThroughLocked = true;
			}
			if (!mouse_over && clickThroughLocked)
			{
				InputLockManager.RemoveControlLock("KerbalismWindowLock");
				clickThroughLocked = false;
			}
		}

		void Draw_window(int _)
		{
			// render window title
			GUILayout.BeginHorizontal(Styles.title_container);
			GUILayout.Label(Textures.empty, Styles.left_icon);
			GUILayout.Label(panel.Title().ToUpper(), Styles.title_text);
			GUILayout.Label(Textures.close, Styles.right_icon);
			bool b = Lib.IsClicked();
			GUILayout.EndHorizontal();
			if (b) { Close(); return; }

			// start scrolling view
			scroll_pos = GUILayout.BeginScrollView(scroll_pos, HighLogic.Skin.horizontalScrollbar, HighLogic.Skin.verticalScrollbar);

			// render panel content
			panel.Render();

			// end scroll view
			GUILayout.EndScrollView();

			// draw tooltip
			tooltip.Draw(win_rect);

			// right click close the window
			if (Event.current.type == EventType.MouseDown
			 && Event.current.button == 1)
			{
				Close();
			}

			// enable dragging
			GUI.DragWindow(drag_rect);
		}

		bool Contains(Vector2 pos)
		{
			return win_rect.Contains(pos);
		}

		internal void Position(uint x, uint y)
		{
			win_rect.Set((float)x, (float)y, win_rect.width, win_rect.height);
		}

		internal uint Left()
		{
			return (uint)win_rect.xMin;
		}

		internal uint Top()
		{
			return (uint)win_rect.yMin;
		}

		internal Panel.PanelType PanelType
		{
			get
			{
				if (panel == null)
					return Panel.PanelType.unknown;
				else
					return panel.paneltype;
			}
		}

		// store window id
		readonly int win_id;

		// store window geometry
		Rect win_rect;

		// store dragbox geometry
		Rect drag_rect;

		// used by scroll window mechanics
		Vector2 scroll_pos;

		// tooltip utility
		Tooltip tooltip;

		// panel
		Panel panel;

		// refresh function
		Action<Panel> refresh;
	}


} // KERBALISM
