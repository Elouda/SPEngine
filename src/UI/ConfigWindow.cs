﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPEngine.UI
{
	public class ConfigWindow : AbstractWindow
	{
		Vector2 designScroll;
		ModuleSPEngine module;
		string inputThrust, inputIgnitions, inputName;
		int inputTL = 1;
		Design currentDesign;
		string confirmTool = null;
		public ConfigWindow(ModuleSPEngine m) :
			base(new Guid("41f4fc6f-06b4-4d6c-9774-908f46beffc0"),
			     "SPEngine Config", new Rect(100, 100, 615, 320))
		{
			designScroll = new Vector2();
			module = m;
			inputThrust = "";
			inputIgnitions = "1";
			fetchDesign();
			Family f = Core.Instance.families[module.familyLetter[0]];
			currentDesign = new Design(f, 0);
		}

		private void fetchDesign()
		{
			inputName = module.DesignName;
			if (module.design != null) {
				inputThrust = module.design.thrust.ToString();
				inputIgnitions = module.design.ignitions.ToString();
				inputTL = module.design.tl + 1;
			}
		}

		private void designList()
		{
			foreach (Design d in Core.Instance.library.designs.Values)
				if (d.family.letter == module.familyLetter[0]) {
					GUILayout.BeginHorizontal();
					try {
						if (GUILayout.Button(d.name)) {
							module.DesignName = d.name;
							module.applyConfig();
							fetchDesign();
						}
						GUILayout.Label(String.Format(": {0:0.##}kN, {1} ignitions; TL {4}.  Mass {2:0.###}, cost {3:0.#}.", d.thrust, d.ignitions, d.mass, d.cost, d.tl + 1));
						if (!d.tooled) {
							if (confirmTool == d.name) {
								GUILayout.Label("Tool:");
								if (GUILayout.Button("OK"))
									d.Tool();
								else if (GUILayout.Button("CANCEL"))
									confirmTool = null;
							} else {
								if (GUILayout.Button("TOOL"))
									confirmTool = d.name;
							}
							GUILayout.Label(String.Format("{0:0.#}f", d.toolCost));
						}
						if (d.tl + 1 < d.family.techLevels.Count) {
							if (d.upgradeTo != null) {
								GUILayout.Label(String.Format("Upgraded: {0}", d.upgradeTo));
							} else if (!d.family.haveTechRequired(d.tl + 1)) {
								GUILayout.Label(String.Format("Upgrade: requires {0}", d.family.getTechRequired(d.tl + 1)));
							} else if (d.tl + 1 >= d.family.unlocked) {
								GUILayout.Label(String.Format("Upgrade: unlock TL {0}", d.tl + 2));
							} else if (Core.Instance.library.designs.ContainsKey(currentDesign.name)) {
								GUILayout.Label("Upgrade: Name in use");
							} else if (currentDesign.name.Equals("")) {
								GUILayout.Label("Upgrade: Choose name");
							} else {
								if (GUILayout.Button("Upgrade")) {
									Design up = new Design(d, d.tl + 1);
									Design updup = up.checkDuplicate();
									if (updup != null) {
										/* Link the existing upgrade to this design.
										 * It shouldn't be possible for updup to already have an upgradeFrom,
										 * because otherwise we'd be a duplicate of that so how did we get
										 * created in the first place?  But check anyway, and don't overwrite
										 * an existing upgradeFrom.
										 */
										d.upgradeTo = updup.name;
										if (updup.upgradeFrom == null)
											updup.upgradeFrom = d.name;
									} else {
										up.name = inputName;
										if (!Core.Instance.library.designs.ContainsKey(currentDesign.name)) {
											Core.Instance.library.AddDesign(up);
											d.upgradeTo = up.name;
											module.DesignName = up.name;
											module.applyConfig();
											currentDesign = new Design(up);
											fetchDesign();
										}
									}
								}
							}
						}
						GUILayout.FlexibleSpace();
					} finally {
						GUILayout.EndHorizontal();
					}
				}
			GUILayout.BeginHorizontal();
			try {
				inputName = GUILayout.TextField(inputName, GUILayout.Width(90));
				inputThrust = GUILayout.TextField(inputThrust, GUILayout.Width(50));
				GUILayout.Label("kN, ");
				inputIgnitions = GUILayout.TextField(inputIgnitions, GUILayout.Width(30));
				currentDesign.name = inputName;
				float.TryParse(inputThrust, out currentDesign.thrust);
				int.TryParse(inputIgnitions, out currentDesign.ignitions);
				GUILayout.Label("ignitions.  TL ");
				if (GUILayout.Button("-") && inputTL > 1)
					inputTL -= 1;
				GUILayout.Label(inputTL.ToString());
				if (GUILayout.Button("+") && inputTL < currentDesign.family.techLevels.Count)
					inputTL += 1;
				currentDesign.tl = inputTL - 1;
				GUILayout.Label(String.Format("  Mass {0:0.###}, cost {1:0.#}", currentDesign.mass, currentDesign.tooledCost));
				Design.Constraint check = currentDesign.check;
				switch (check) {
				case Design.Constraint.OK:
					if (Core.Instance.library.designs.ContainsKey(currentDesign.name)) {
						GUILayout.Label("Name in use");
					} else if (currentDesign.name.Equals("")) {
						GUILayout.Label("Choose name");
					} else if (currentDesign.checkDuplicate() != null) {
						GUILayout.Label(String.Format("Duplicates {0}", currentDesign.checkDuplicate().name));
					} else if (GUILayout.Button("Apply")) {
						Core.Instance.library.AddDesign(currentDesign);
						module.DesignName = currentDesign.name;
						module.applyConfig();
						currentDesign = new Design(currentDesign);
					}
					break;
				case Design.Constraint.UNLOCK:
					if (currentDesign.unlockCost == 0f) {
						GUILayout.Label("Unlocking");
						currentDesign.Unlock();
						break;
					}
					if (GUILayout.Button("Unlock")) {
						currentDesign.Unlock();
					}
					GUILayout.Label(String.Format("{0:F0}f", currentDesign.unlockCost));
					break;
				case Design.Constraint.TECH:
					GUILayout.Label(String.Format("Requires {0}", currentDesign.techRequired));
					break;
				default:
					GUILayout.Label(check.ToString());
					break;
				}
				GUILayout.FlexibleSpace();
			} finally {
				GUILayout.EndHorizontal();
			}
		}

		public override void Window(int id)
		{
			GUILayout.BeginVertical(GUILayout.Width(595));
			try {
				GUILayout.BeginHorizontal();
				try {
					Family f = Core.Instance.families[module.familyLetter[0]];
					GUILayout.Label("Family ");
					GUILayout.Label(f.letter.ToString(), boldLblStyle);
					GUILayout.Label(": ");
					GUILayout.Label(f.description);
				} finally {
					GUILayout.EndHorizontal();
				}
				designScroll = GUILayout.BeginScrollView(designScroll, GUILayout.Width(595), GUILayout.Height(160));
				try {
					designList();
				} finally {
					GUILayout.EndScrollView();
				}
			} finally {
				GUILayout.EndVertical();
				base.Window(id);
			}
		}
	}
}
