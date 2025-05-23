using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
	// store and render a simple structured ui
	sealed class Panel
	{
		internal enum PanelType
		{
			unknown,
			telemetry,
			data,
			scripts,
			failures,
			config,
			log,
			connection
		}

		internal Panel()
		{
			headers = new List<Header>();
			sections = new List<Section>();
			callbacks = new List<Action>();
			win_title = string.Empty;
			min_width = Styles.ScaleWidthFloat(280.0f);
			paneltype = PanelType.unknown;
		}

		internal void Clear()
		{
			headers.Clear();
			sections.Clear();
			win_title = string.Empty;
			min_width = Styles.ScaleWidthFloat(280.0f);
			paneltype = PanelType.unknown;
		}

		internal void AddHeader(string label, string tooltip = "", Action click = null)
		{
			Header h = new Header
			{
				label = label,
				tooltip = tooltip,
				click = click,
				icons = new List<Icon>(),
				leftIcon = null
			};
			headers.Add(h);
		}

		///<summary> Sets the last added header or content leading icon (doesn't support sections)</summary>
		internal void SetLeftIcon(Texture2D texture, string tooltip = "", Action click = null)
		{
			Icon i = new Icon
			{
				texture = texture,
				tooltip = tooltip,
				click = click
			};

			if (sections.Count > 0)
			{
				Section p = sections[sections.Count - 1];
				p.entries[p.entries.Count - 1].leftIcon = i;
			}
			else if (headers.Count > 0)
			{
				Header h = headers[headers.Count - 1];
				h.leftIcon = i;
			}
		}

		internal void AddSection(string title, string desc = "", Action left = null, Action right = null, Boolean sort = false)
		{
			Section p = new Section
			{
				title = title,
				desc = desc,
				left = left,
				right = right,
				sort = sort,
				needsSort = false,
				entries = new List<Entry>()
			};
			sections.Add(p);
		}

		internal void AddContent(string label, string value = "", string tooltip = "", Action click = null, Action hover = null)
		{
			Entry e = new Entry
			{
				label = label,
				value = value,
				tooltip = tooltip,
				click = click,
				hover = hover,
				icons = new List<Icon>()
			};
			if (sections.Count > 0) {
				Section section = sections[sections.Count - 1];
				section.entries.Add(e);
				section.needsSort = section.sort;
			}
		}

		///<summary> Adds an icon to the last added header or content (doesn't support sections) </summary>
		internal void AddRightIcon(Texture2D texture, string tooltip = "", Action click = null)
		{
			Icon i = new Icon
			{
				texture = texture,
				tooltip = tooltip,
				click = click
			};
			if (sections.Count > 0)
			{
				Section p = sections[sections.Count - 1];
				p.entries[p.entries.Count - 1].icons.Add(i);
			}
			else if (headers.Count > 0)
			{
				Header h = headers[headers.Count - 1];
				h.icons.Add(i);
			}
		}

		internal void Render()
		{
			// headers
			foreach (Header h in headers)
			{
				GUILayout.BeginHorizontal(Styles.entry_container);
				if (h.leftIcon != null)
				{
					GUILayout.Label(new GUIContent(h.leftIcon.texture, h.leftIcon.tooltip), Styles.left_icon);
					if (h.leftIcon.click != null && UI.IsClicked())
						callbacks.Add(h.leftIcon.click);
				}
				GUILayout.Label(new GUIContent(h.label, h.tooltip), Styles.entry_label_nowrap);
				if (h.click != null && UI.IsClicked()) callbacks.Add(h.click);
				foreach (Icon i in h.icons)
				{
					GUILayout.Label(new GUIContent(i.texture, i.tooltip), Styles.right_icon);
					if (i.click != null && UI.IsClicked()) callbacks.Add(i.click);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(Styles.ScaleFloat(10.0f));
			}

			// sections
			foreach (Section p in sections)
			{
				// section title
				GUILayout.BeginHorizontal(Styles.section_container);
				if (p.left != null)
				{
					GUILayout.Label(Textures.left_arrow, Styles.left_icon);
					if (UI.IsClicked()) callbacks.Add(p.left);
				}
				GUILayout.Label(p.title, Styles.section_text);
				if (p.right != null)
				{
					GUILayout.Label(Textures.right_arrow, Styles.right_icon);
					if (UI.IsClicked()) callbacks.Add(p.right);
				}
				GUILayout.EndHorizontal();

				// description
				if (p.desc.Length > 0)
				{
					GUILayout.BeginHorizontal(Styles.desc_container);
					GUILayout.Label(p.desc, Styles.desc);
					GUILayout.EndHorizontal();
				}

				// entries
				if(p.needsSort) {
					p.needsSort = false;
					p.entries.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.Ordinal));
				}
				foreach (Entry e in p.entries)
				{
					GUILayout.BeginHorizontal(Styles.entry_container);
					if (e.leftIcon != null)
					{
						GUILayout.Label(new GUIContent(e.leftIcon.texture, e.leftIcon.tooltip), Styles.left_icon);
						if (e.leftIcon.click != null && UI.IsClicked())
							callbacks.Add(e.leftIcon.click);
					}
					GUILayout.Label(new GUIContent(e.label, e.tooltip), Styles.entry_label, GUILayout.Height(Styles.entry_label.fontSize));
					if (e.hover != null && UI.IsHover()) callbacks.Add(e.hover);
					GUILayout.Label(new GUIContent(e.value, e.tooltip), Styles.entry_value, GUILayout.Height(Styles.entry_value.fontSize));
					if (e.click != null && UI.IsClicked()) callbacks.Add(e.click);
					if (e.hover != null && UI.IsHover()) callbacks.Add(e.hover);
					foreach (Icon i in e.icons)
					{
						GUILayout.Label(new GUIContent(i.texture, i.tooltip), Styles.right_icon);
						if (i.click != null && UI.IsClicked()) callbacks.Add(i.click);
					}
					GUILayout.EndHorizontal();
				}

				// spacing
				GUILayout.Space(Styles.ScaleFloat(10.0f));
			}

			// call callbacks
			if (Event.current.type == EventType.Repaint)
			{
				foreach (Action func in callbacks) func();
				callbacks.Clear();
			}
		}

		internal float Height()
		{
			float h = 0.0f;

			h += Styles.ScaleFloat((float)headers.Count * 27.0f);

			foreach (Section p in sections)
			{
				h += Styles.ScaleFloat(18.0f + (float)p.entries.Count * 16.0f + 16.0f);
				if (p.desc.Length > 0)
				{
					h += Styles.desc.CalcHeight(new GUIContent(p.desc), min_width - Styles.ScaleWidthFloat(20.0f));
				}
			}

			return h;
		}

		// utility: decrement an index, warping around 0
		internal void Prev(ref int index, int count)
		{
			index = (index == 0 ? count : index) - 1;
		}

		// utility: increment an index, warping around a max
		internal void Next(ref int index, int count)
		{
			index = (index + 1) % count;
		}

		// utility: toggle a flag
		internal void Toggle(ref bool b)
		{
			b = !b;
		}

		// merge another panel with this one
		internal void Add(Panel p)
		{
			headers.AddRange(p.headers);
			sections.AddRange(p.sections);
		}

		// collapse all sections into one
		internal void Collapse(string title)
		{
			if (sections.Count > 0)
			{
				sections[0].title = title;
				for (int i = 1; i < sections.Count; ++i) sections[0].entries.AddRange(sections[i].entries);
			}
			while (sections.Count > 1) sections.RemoveAt(sections.Count - 1);
		}

		// return true if panel has no sections or titles
		internal bool Empty()
		{
			return sections.Count == 0 && headers.Count == 0;
		}

		// set title metadata
		internal void Title(string s)
		{
			win_title = s;
		}

		// set width metadata
		// - width never shrink
		internal void Width(float w)
		{
			min_width = System.Math.Max(w, min_width);
		}

		// get medata
		internal string Title() { return win_title; }
		internal float Width() { return min_width; }

		sealed class Header
		{
			internal string label;
			internal string tooltip;
			internal Action click;
			internal List<Icon> icons;
			internal Icon leftIcon;
		}

		sealed class Section
		{
			internal string title;
			internal string desc;
			internal Action left;
			internal Action right;
			internal Boolean sort;
			internal Boolean needsSort;
			internal List<Entry> entries;
		}

		sealed class Entry
		{
			internal string label;
			internal string value;
			internal string tooltip;
			internal Action click;
			internal Action hover;
			internal List<Icon> icons;
			internal Icon leftIcon;
		}

		sealed class Icon
		{
			internal Texture2D texture;
			internal string tooltip;
			internal Action click;
		}

		List<Header> headers;    // fat entries to show before the first section
		List<Section> sections;  // set of sections
		List<Action> callbacks;  // functions to call on input events
		string win_title;        // metadata stored in panel
		float min_width;         // metadata stored in panel
		internal PanelType paneltype;
	}
} // KERBALISM
