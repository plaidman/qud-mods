using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class ZonePopup : BasePopup {
		public IEnumerable<ToggledItem> ShowPopup(
			InventoryItem[] options = null,
			int[] initialSelections = null
		) {
			var defaultSelected = 0;

			var selectedItems = new HashSet<int>();
			foreach (var item in initialSelections) {
				selectedItems.Add(item);
			}

			ResetCache();
			var sortedOptions = SortItemsDescending(options);
			IRenderable[] itemIcons = sortedOptions.Select(
				(item) => { return item.Icon; }
			).ToArray();
			string[] itemLabels = sortedOptions.Select((item) => {
				var selected = selectedItems.Contains(item.Index);
				return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
			}).ToArray();

			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					command = "option:-2",
					hotkey = "G"
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType),
					command = "option:-3",
					hotkey = "Tab"
				},
			};

			while (true) {
				menuCommands[0].text = "{{W|[G]}} {{y|" + (selectedItems.Count < options.Length ? "S" : "Des") + "elect All}}";

				int selectedIndex = Popup.PickOption(
					Title: "Lootable Items",
					Intro: "Mark items here, then autoexplore to pick them up.",
					IntroIcon: null,
					Options: itemLabels,
					RespectOptionNewlines: false,
					Icons: itemIcons,
					DefaultSelected: defaultSelected,
					Buttons: menuCommands,
					AllowEscape: true
				);

				switch (selectedIndex) {
					case -1:  // Cancelled
						yield break;

					case -2:  // G
						// var tempList = new List<int>(selectedItems);
						// if (selectedItems.Count < options.Length) {
						// 	selectedItems.Clear();
						// 	selectedItems.AddRange(Enumerable.Range(0, itemLabels.Length));

						// 	// Yield options that changed
						// 	foreach (var n in selectedItems.Except(tempList)) {
						// 		yield return new ToggledItem(n, true);
						// 	}
						// } else {
						// 	selectedItems.Clear();
						// 	// Yield options that changed
						// 	foreach (var n in tempList) {
						// 		yield return new ToggledItem(n, false);
						// 	}
						// }
						continue;
						
					case -3: // Tab
						CurrentSortType = PopupUtils.NextSortType.GetValue(CurrentSortType);

						menuCommands[1].text = PopupUtils.GetSortLabel(CurrentSortType);
						sortedOptions = SortItemsDescending(options);
						itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
						itemLabels = sortedOptions.Select((item) => {
							var selected = selectedItems.Contains(item.Index);
							return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
						}).ToArray();

						yield return new ToggledItem(-3, false);
						continue;

					default:
						break;
				}
				
				var mappedItem = sortedOptions[selectedIndex];
				if (selectedItems.Contains(mappedItem.Index)) {
					selectedItems.Remove(mappedItem.Index);
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(false, mappedItem, CurrentSortType);
					yield return new ToggledItem(mappedItem.Index, false);
				} else {
					selectedItems.Add(mappedItem.Index);
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(true, mappedItem, CurrentSortType);
					yield return new ToggledItem(mappedItem.Index, true);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
