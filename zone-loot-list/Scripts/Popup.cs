using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;

namespace Plaidman.ZoneLootList.Menus {
	public class ItemList {
		public class ToggledItem {
			public int Index { get; }
			public bool Value { get; }

			public ToggledItem(int index, bool value) {
				Index = index;
				Value = value;
			}
		}

		public static IEnumerable<ToggledItem> ShowPopup(
			string[] options = null,
			IRenderable[] icons = null,
			int[] initialSelections = null
		) {
			var defaultSelected = 0;
			var selectedItems = (initialSelections is null) ? new List<int>() : new List<int>(initialSelections);
			int numOptions = options.Length;
			string[] itemLabels = new string[options.Length];

			QudMenuItem[] menuCommands = new QudMenuItem[1]
			{
				new() {
					command = "option:-3",
					hotkey = "Tab"
				}
			};

			while (true) {
				menuCommands[0].text = "{{W|[Tab]}} {{y|" + (selectedItems.Count < numOptions ? "S" : "Des") + "elect All}}";
				for (int i = 0; i < itemLabels.Length; i++) {
					itemLabels[i] = (selectedItems.Contains(i) ? "{{W|[þ]}} " : "{{y|[ ]}} ") + options[i];
				}

				int selectedIndex = Popup.PickOption(
					Title: "Lootable Items",
					Intro: "Mark items here, then autoexplore to pick them up.",
					IntroIcon: null,
					Options: itemLabels,
					RespectOptionNewlines: false,
					Icons: icons,
					DefaultSelected: defaultSelected,
					Buttons: menuCommands,
					AllowEscape: true
				);

				switch (selectedIndex) {
					case -1:  // Esc / Cancelled
						yield break;

					case -3:  // Tab
						var tempList = new List<int>(selectedItems);
						if (selectedItems.Count < numOptions) {
							selectedItems.Clear();
							selectedItems.AddRange(Enumerable.Range(0, itemLabels.Length));

							// Yield options that changed
							foreach (var n in selectedItems.Except(tempList)) {
								yield return new ToggledItem(n, true);
							}
						} else {
							selectedItems.Clear();
							// Yield options that changed
							foreach (var n in tempList) {
								yield return new ToggledItem(n, false);
							}
						}
						continue;

					default:
						break;
				}

				int selectedItemIndex = selectedItems.IndexOf(selectedIndex);
				if (selectedItemIndex == -1) {
					selectedItems.Add(selectedIndex);
					yield return new ToggledItem(selectedIndex, true);
				} else {
					selectedItems.RemoveAt(selectedItemIndex);
					yield return new ToggledItem(selectedIndex, false);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
