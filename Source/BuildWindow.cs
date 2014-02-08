using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExBuildWindow : MonoBehaviour
	{
		public class Styles {
			public static GUIStyle normal;
			public static GUIStyle red;
			public static GUIStyle yellow;
			public static GUIStyle green;
			public static GUIStyle white;
			public static GUIStyle label;
			public static GUIStyle slider;
			public static GUIStyle sliderText;

			public static GUIStyle listItem;
			public static GUIStyle listBox;

			public static ProgressBar bar;

			private static bool initialized;

			public static void Init ()
			{
				if (initialized)
					return;
				initialized = true;

				normal = new GUIStyle (GUI.skin.button);
				normal.normal.textColor = normal.focused.textColor = Color.white;
				normal.hover.textColor = normal.active.textColor = Color.yellow;
				normal.onNormal.textColor = normal.onFocused.textColor = normal.onHover.textColor = normal.onActive.textColor = Color.green;
				normal.padding = new RectOffset (8, 8, 8, 8);

				red = new GUIStyle (GUI.skin.box);
				red.padding = new RectOffset (8, 8, 8, 8);
				red.normal.textColor = red.focused.textColor = Color.red;

				yellow = new GUIStyle (GUI.skin.box);
				yellow.padding = new RectOffset (8, 8, 8, 8);
				yellow.normal.textColor = yellow.focused.textColor = Color.yellow;

				green = new GUIStyle (GUI.skin.box);
				green.padding = new RectOffset (8, 8, 8, 8);
				green.normal.textColor = green.focused.textColor = Color.green;

				white = new GUIStyle (GUI.skin.box);
				white.padding = new RectOffset (8, 8, 8, 8);
				white.normal.textColor = white.focused.textColor = Color.white;

				label = new GUIStyle (GUI.skin.label);
				label.normal.textColor = label.focused.textColor = Color.white;
				label.alignment = TextAnchor.MiddleCenter;

				slider = new GUIStyle (GUI.skin.horizontalSlider);
				slider.margin = new RectOffset (0, 0, 0, 0);

				sliderText = new GUIStyle (GUI.skin.label);
				sliderText.alignment = TextAnchor.MiddleCenter;
				sliderText.margin = new RectOffset (0, 0, 0, 0);

				listItem = new GUIStyle ();
				listItem.normal.textColor = Color.white;
				Texture2D texInit = new Texture2D(1, 1);
				texInit.SetPixel(0, 0, Color.white);
				texInit.Apply();
				listItem.hover.background = texInit;
				listItem.onHover.background = texInit;
				listItem.hover.textColor = Color.black;
				listItem.onHover.textColor = Color.black;
				listItem.padding = new RectOffset(4, 4, 4, 4);

				listBox = new GUIStyle(GUI.skin.box);

				bar = new ProgressBar (XKCDColors.Amber,
									   XKCDColors.Amethyst,
									   new Color(255, 255, 255, 0.8f));
			}
		}

		static ExBuildWindow instance;
		static bool gui_enabled = true;
		static Rect windowpos;
		static bool highlight_pad = true;
		static bool link_lfo_sliders = true;

		static CraftBrowser craftlist = null;
		static Vector2 resscroll;

		List<ExLaunchPad> launchpads;
		DropDownList pad_list;
		ExLaunchPad pad;

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
			if (instance != null) {
				instance.onShowUI ();
			}
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				windowpos.x = pos.x;
				windowpos.y = pos.y;
				windowpos.width = pos.z;
				windowpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out gui_enabled);
			}
			val = node.GetValue ("link_lfo_sliders");
			if (val != null) {
				bool.TryParse (val, out link_lfo_sliders);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			Quaternion pos;
			pos.x = windowpos.x;
			pos.y = windowpos.y;
			pos.z = windowpos.width;
			pos.w = windowpos.height;
			node.AddValue ("rect", KSPUtil.WriteQuaternion (pos));
			node.AddValue ("visible", gui_enabled);
			node.AddValue ("link_lfo_sliders", link_lfo_sliders);
		}

		void BuildPadList (Vessel v)
		{
			launchpads = null;
			pad_list = null;
			pad = null;	//FIXME would be nice to not lose the active pad
			var pads = new List<ExLaunchPad> ();

			foreach (var p in v.Parts) {
				pads.AddRange (p.Modules.OfType<ExLaunchPad> ());
			}
			if (pads.Count > 0) {
				launchpads = pads;
				pad = launchpads[0];
				var pad_names = new List<string> ();
				int ind = 0;
				foreach (var p in launchpads) {
					if (p.PadName != "") {
						pad_names.Add (p.PadName);
					} else {
						pad_names.Add ("pad-" + ind);
					}
					ind++;
				}
				pad_list = new DropDownList (pad_names);
			}
		}

		void onVesselChange (Vessel v)
		{
			BuildPadList (v);
			onShowUI ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (FlightGlobals.ActiveVessel == v) {
				BuildPadList (v);
			}
		}

		void onHideUI ()
		{
			enabled = false;
			if (pad != null) {
				pad.part.SetHighlightDefault ();
			}
		}

		void onShowUI ()
		{
			enabled = launchpads != null && gui_enabled;
			if (enabled && highlight_pad && pad != null) {
				pad.part.SetHighlightColor (XKCDColors.LightSeaGreen);
				pad.part.SetHighlight (true);
			}
		}

		void Awake ()
		{
			instance = this;
			GameEvents.onVesselChange.Add (onVesselChange);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);
			enabled = false;
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onVesselChange.Remove (onVesselChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
		}

		float ResourceLine (string label, string resourceName, float fraction,
							double minAmount, double maxAmount,
							double available)
		{
			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			// Fill amount
			// limit slider to 0.5% increments
			GUILayout.BeginVertical ();
			if (minAmount == maxAmount) {
				GUILayout.Box ("Must be 100%", GUILayout.Width (300),
							   GUILayout.Height (20));
				fraction = 1.0F;
			} else {
				fraction = GUILayout.HorizontalSlider (fraction, 0.0F, 1.0F,
													   Styles.slider,
													   GUI.skin.horizontalSliderThumb,
													   GUILayout.Width (300),
													   GUILayout.Height (20));
				fraction = (float)Math.Round (fraction, 3);
				fraction = (Mathf.Floor (fraction * 200)) / 200;
				GUILayout.Box ((fraction * 100).ToString () + "%",
							   Styles.sliderText, GUILayout.Width (300),
							   GUILayout.Height (20));
			}
			GUILayout.EndVertical ();

			double required = minAmount + (maxAmount - minAmount) * fraction;

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (available >= 0 && available < required) {
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			if (available >= 0) {
				GUILayout.Box ((Math.Round (available, 2)).ToString (),
							   Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			} else {
				GUILayout.Box ("N/A", Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			}

			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();

			return fraction;
		}

		void ResourceProgress (string label, BuildCost.BuildResource br,
							   BuildCost.BuildResource req)
		{
			double fraction = (req.amount - br.amount) / req.amount;
			double required = br.amount;
			double available = pad.padResources.ResourceAmount (br.name);

			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			GUILayout.BeginVertical ();
			var percent = (fraction * 100).ToString("G4") + "%";
			Styles.bar.Draw ((float) fraction, percent, 300);
			GUILayout.EndVertical ();

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (required > available) {
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.Box ((Math.Round (available, 2)).ToString (),
						   Styles.white, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();
		}

		void SelectPad_start ()
		{
			pad_list.styleListItem = Styles.listItem;
			pad_list.styleListBox = Styles.listBox;
			pad_list.DrawBlockingSelector ();
		}

		void SelectPad ()
		{
			GUILayout.BeginHorizontal ();
			pad_list.DrawButton ();
			pad = launchpads[pad_list.SelectedIndex];
			highlight_pad = GUILayout.Toggle (highlight_pad, "Highlight Pad");
			if (highlight_pad) {
				pad.part.SetHighlightColor (XKCDColors.LightSeaGreen);
				pad.part.SetHighlight (true);
			} else {
				pad.part.SetHighlightDefault ();
			}
			GUILayout.EndHorizontal ();
		}

		void SelectPad_end ()
		{
			pad_list.DrawDropDown();
			pad_list.CloseOnOutsideClick();
		}

		void SelectCraft ()
		{
			GUILayout.BeginHorizontal ("box");
			GUILayout.FlexibleSpace ();
			// VAB / SPH selection
			for (var t = ExLaunchPad.CraftType.VAB;
				 t <= ExLaunchPad.CraftType.SubAss;
				 t++) {
				if (GUILayout.Toggle (pad.craftType == t, t.ToString (),
									  GUILayout.Width (80))) {
					pad.craftType = t;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			string strpath = HighLogic.SaveFolder;

			if (GUILayout.Button ("Select Craft", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				string []dir = new string[] {"VAB", "SPH", "../Subassemblies"};
				var diff = HighLogic.CurrentGame.Parameters.Difficulty;
				bool stock = diff.AllowStockVessels;
				if (pad.craftType == ExLaunchPad.CraftType.SubAss) {
					diff.AllowStockVessels = false;
				}
				//GUILayout.Button is "true" when clicked
				var clrect = new Rect (Screen.width / 2, 100, 350, 500);
				craftlist = new CraftBrowser (clrect, dir[(int)pad.craftType],
											  strpath, "Select a ship to load",
											  craftSelectComplete,
											  craftSelectCancel,
											  HighLogic.Skin,
											  EditorLogic.ShipFileImage, true);
				diff.AllowStockVessels = stock;
			}
		}

		void SelectedCraft ()
		{
			var ship_name = pad.craftConfig.GetValue ("ship");
			GUILayout.Box ("Selected Craft:	" + ship_name, Styles.white);
		}

		void ResourceHeader ()
		{
			var width120 = GUILayout.Width (120);
			var width300 = GUILayout.Width (300);
			var width75 = GUILayout.Width (75);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resource", Styles.label, width120);
			GUILayout.Label ("Fill Percentage", Styles.label, width300);
			GUILayout.Label ("Required", Styles.label, width75);
			GUILayout.Label ("Available", Styles.label, width75);
			GUILayout.EndHorizontal ();
		}

		void RequiredResources ()
		{
			GUILayout.Label ("Resources required to build:", Styles.label,
							 GUILayout.Width (600));
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (600),
												   GUILayout.Height (300));
			foreach (var br in pad.buildCost.required) {
				double a = br.amount;
				double available = -1;

				available = pad.padResources.ResourceAmount (br.name);
				ResourceLine (br.name, br.name, 1.0f, a, a, available);
			}
			GUILayout.EndScrollView ();
		}

		void BuildButton ()
		{
			if (GUILayout.Button ("Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				pad.BuildCraft ();
			}
		}

		static BuildCost.BuildResource FindResource (List<BuildCost.BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void BuildProgress ()
		{
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (600),
												   GUILayout.Height (300));
			foreach (var br in pad.builtStuff.required) {
				var req = FindResource (pad.buildCost.required, br.name);
				ResourceProgress (br.name, br, req);
			}
			GUILayout.EndScrollView ();
		}

		void OptionalResources ()
		{
			link_lfo_sliders = GUILayout.Toggle (link_lfo_sliders,
												 "Link LiquidFuel and "
												 + "Oxidizer sliders");
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (600),
												   GUILayout.Height (300));
			foreach (var br in pad.buildCost.optional) {
				double available = pad.padResources.ResourceAmount (br.name);
				double maximum = pad.craftResources.ResourceCapacity(br.name);
				float frac = (float) (br.amount / maximum);
				frac = ResourceLine (br.name, br.name, frac, 0,
									 maximum, available);
				if (link_lfo_sliders
					&& (br.name == "LiquidFuel" || br.name == "Oxidizer")) {
					string other;
					if (br.name == "LiquidFuel") {
						other = "Oxidizer";
					} else {
						other = "LiquidFuel";
					}
					var or = FindResource (pad.buildCost.optional, other);
					double om = pad.craftResources.ResourceCapacity (other);
					or.amount = om * frac;
				}
				br.amount = maximum * frac;
			}
			GUILayout.EndScrollView ();
		}

		void ReleaseButton ()
		{
			if (GUILayout.Button ("Release", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				pad.TransferResources ();
				pad.ReleaseVessel ();
			}
		}

		void CloseButton ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				gui_enabled = false;
				onHideUI ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void WindowGUI (int windowID)
		{
			Styles.Init ();

			SelectPad_start ();

			GUILayout.BeginVertical ();
			SelectPad ();

			switch (pad.state) {
			case ExLaunchPad.State.Idle:
				SelectCraft ();
				break;
			case ExLaunchPad.State.Planning:
				SelectCraft ();
				SelectedCraft ();
				RequiredResources ();
				BuildButton ();
				break;
			case ExLaunchPad.State.Building:
				SelectedCraft ();
				BuildProgress ();
				break;
			case ExLaunchPad.State.Complete:
				SelectedCraft ();
				OptionalResources ();
				ReleaseButton ();
				break;
			}

			GUILayout.EndVertical ();

			CloseButton ();

			SelectPad_end ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		private void craftSelectComplete (string filename, string flagname)
		{
			craftlist = null;
			pad.LoadCraft (filename, flagname);
		}

		private void craftSelectCancel ()
		{
			craftlist = null;
		}

		void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;
			string sit = pad.vessel.situation.ToString ();
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  "Extraplanetary Launchpad: " + sit,
										  GUILayout.Width (600));
			if (craftlist != null) {
				craftlist.OnGUI ();
			}
		}
	}
}
