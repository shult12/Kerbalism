#if DEBUG_PROFILER
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
#endif

namespace KERBALISM
{
#if !DEBUG_PROFILER
    /// <summary> Simple profiler for measuring the execution time of code placed between the Start and Stop methods. </summary>
    sealed class Profiler
    {
#endif
#if DEBUG_PROFILER
	/// <summary> Simple profiler for measuring the execution time of code placed between the Start and Stop methods. </summary>
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public sealed class Profiler: MonoBehaviour
	{
		// constants
		const float width = 500.0f;
		const float height = 500.0f;

		const float value_width = 65.0f;

		// visible flag
		static bool visible = false;
		static bool show_zero = true;

		// popup window
		static MultiOptionDialog multi_dialog;
		static PopupDialog popup_dialog;
		static DialogGUIVerticalLayout dialog_items;

		// an entry in the profiler
		class Entry
		{
			internal double start;        // used to measure call time
			internal long calls;          // number of calls in current simulation step
			internal double time;         // time in current simulation step
			internal long prev_calls;     // number of calls in previous simulation step
			internal double prev_time;    // time in previous simulation step
			internal long tot_calls;      // number of calls in total used for avg calculation
			internal double tot_time;     // total time used for avg calculation

			internal string last_txt = "";        // last call time display string
			internal string avg_txt = "";         // average call time display string
			internal string calls_txt = "";       // number of calls display string
			internal string avg_calls_txt = "";   // number of average calls display string
		}

		// store all entries
		Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

		// display update timer
		static double update_timer = Time.Clocks();
		readonly static double timeout = Stopwatch.Frequency / update_fps;
		const double update_fps = 5.0;      // Frames per second the entry value display will update.
		static long tot_frames = 0;         // total physics frames used for avg calculation
		static string tot_frames_txt = "";  // total physics frames display string


		// permit global access
		internal static Profiler Fetch { get; private set; } = null;

		//  constructor
		internal Profiler()
		{
			// enable global access
			Fetch = this;

			// create window
			dialog_items = new DialogGUIVerticalLayout();
			multi_dialog = new MultiOptionDialog(
			   "KerbalismProfilerWindow",
			   "",
			   GetTitle(),
			   HighLogic.UISkin,
			   new Rect(0.5f, 0.5f, width, height),
			   new DialogGUIBase[]
			   {
				   new DialogGUIVerticalLayout(false, false, 0, new RectOffset(), TextAnchor.UpperCenter,
                       // create average reset and show zero calls buttons
                       new DialogGUIHorizontalLayout(false, false,
						   new DialogGUIButton(Localizer.Format("#autoLOC_900305"),
							   OnButtonClick_Reset, () => true, 75, 25, false),
						   new DialogGUIToggle(() => { return show_zero; },"Show zero calls", OnButtonClick_ShowZero),
						   new DialogGUILabel(() => { return tot_frames_txt; }, value_width + 50f)),
                       // create header line
                       new DialogGUIHorizontalLayout(
						   new DialogGUILabel("<b>   NAME</b>", true),
						   new DialogGUILabel("<b>LAST</b>", value_width),
						   new DialogGUILabel("<b>AVG</b>", value_width),
						   new DialogGUILabel("<b>CALLS</b>", value_width - 15f),
						   new DialogGUILabel("<b>AVG</b>", value_width - 10f))),
                   // create scrollbox for entry data
                   new DialogGUIScrollList(new Vector2(), false, true, dialog_items)
			   });
		}

		void Start()
		{
			// create popup dialog
			popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, false, HighLogic.UISkin, false, "");
			if (popup_dialog != null)
				popup_dialog.gameObject.SetActive(false);
		}

		void Update()
		{
			if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
					 Input.GetKeyUp(KeyCode.P) && popup_dialog != null)
			{
				visible = !visible;
				popup_dialog.gameObject.SetActive(visible);
			}

			// skip updates for a smoother display
			if (((Time.Clocks() - update_timer) > timeout) && visible)
			{
				update_timer = Time.Clocks();
				Calculate();
			}
		}

		static void Calculate()
		{
			foreach (KeyValuePair<string, Entry> p in Fetch.entries)
			{
				Entry e = p.Value;

				if (e.prev_calls > 0L)
				{
					e.last_txt = Time.Microseconds((ulong)(e.prev_time / e.prev_calls)).ToString("F2") + "ms";
					e.calls_txt = e.prev_calls.ToString();
				}
				else if (show_zero)
				{
					e.last_txt = "ms";
					e.calls_txt = "0";
				}

				e.avg_txt = (e.tot_calls > 0L ? Time.Microseconds((ulong)(e.tot_time / e.tot_calls)).ToString("F2") : "") + "ms";
				e.avg_calls_txt = tot_frames > 0L ? ((float)e.tot_calls / (float)tot_frames).ToString("F3") : "0";
			}

			tot_frames_txt = tot_frames.ToString() + " Frames";
		}

		void FixedUpdate()
		{
			foreach (KeyValuePair<string, Entry> p in Fetch.entries)
			{
				Entry e = p.Value;

				e.prev_calls = e.calls;
				e.prev_time = e.time;
				e.tot_calls += e.calls;
				e.tot_time += e.time;
				e.calls = 0L;
				e.time = 0.0;
			}

			++tot_frames;
		}

		void OnDestroy()
		{
			Fetch = null;
			if (popup_dialog != null)
			{
				popup_dialog.Dismiss();
				popup_dialog = null;
			}
		}

		static string GetTitle()
		{
			switch (Localizer.CurrentLanguage)
			{
				case "es-es":
					return "Kerbalism Profiler";
				case "ru":
					return "Провайдер Kerbalism";
				case "zh-cn":
					return "Kerbalism 分析器";
				case "ja":
					return "Kerbalism プロファイラ";
				case "de-de":
					return "Kerbalism Profiler";
				case "fr-fr":
					return "Kerbalism Profiler";
				case "it-it":
					return "Kerbalism Profiler";
				case "pt-br":
					return "Kerbalism perfil";
				default:
					return "Kerbalism Profiler";
			}
		}

		static void OnButtonClick_Reset()
		{
			foreach (KeyValuePair<string, Entry> e in Fetch.entries)
			{
				e.Value.tot_calls = 0L;
				e.Value.tot_time = 0.0;
			}

			tot_frames = 0L;
		}

		static void OnButtonClick_ShowZero(bool inState)
		{
			show_zero = inState;
		}

		void AddDialogItem(string e_name)
		{
			// add item
			dialog_items.AddChild(
				new DialogGUIHorizontalLayout(
					new DialogGUILabel("  " + e_name, true),
					new DialogGUILabel(() => { return entries[e_name].last_txt; }, value_width),
					new DialogGUILabel(() => { return entries[e_name].avg_txt; }, value_width),
					new DialogGUILabel(() => { return entries[e_name].calls_txt; }, value_width - 15f),
					new DialogGUILabel(() => { return entries[e_name].avg_calls_txt; }, value_width - 10f)));

			// required to force the Gui creation
			Stack<Transform> stack = new Stack<Transform>();
			stack.Push(dialog_items.uiItem.gameObject.transform);
			dialog_items.children[dialog_items.children.Count - 1].Create(ref stack, HighLogic.UISkin);
		}
#endif

		[System.Diagnostics.Conditional("DEBUG_PROFILER")]
		/// <summary> Start a profiler entry. </summary>
		internal static void Start(string e_name)
		{
#if DEBUG_PROFILER
			if (Fetch == null)
				return;

			if (!Fetch.entries.ContainsKey(e_name))
			{
				Fetch.entries.Add(e_name, new Entry());
				Fetch.AddDialogItem(e_name);
			}

			Fetch.entries[e_name].start = Time.Clocks();
#endif
		}

		[System.Diagnostics.Conditional("DEBUG_PROFILER")]
		/// <summary> Stop a profiler entry. </summary>
		internal static void Stop(string e_name)
		{
#if DEBUG_PROFILER
			if (Fetch == null)
				return;

			Entry e = Fetch.entries[e_name];

			++e.calls;
			e.time += Time.Clocks() - e.start;
#endif
		}

#if DEBUG_PROFILER

		/// <summary> Profile a function scope. </summary>
		sealed class ProfileScope: IDisposable
		{
			internal ProfileScope(string name)
			{
				this.name = name;
				Profiler.Start(name);
			}

			public void Dispose()
			{
				Profiler.Stop(name);
			}

			readonly string name;
		}

#endif
	}

} // KERBALISM
