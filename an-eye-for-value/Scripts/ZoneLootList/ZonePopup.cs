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
					hotkey = "Plaidman_AnEyeForValue_Popup_Toggle"
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType),
					command = "option:-3",
					hotkey = "Plaidman_AnEyeForValue_Popup_Sort"
				},
			};

			while (true) {
				var toggleKey = ControlManager.getCommandInputFormatted("Plaidman_AnEyeForValue_Popup_Toggle");
				var selectPrefix = selectedItems.Count < options.Length ? "S" : "Des";
				menuCommands[0].text = "{{W|[" + toggleKey + "]}} {{y|" + selectPrefix + "elect All}}";

				int selectedIndex = Popup.PickOption(
					Title: "Lootable Items",
					Intro: "Mark items here, then autoexplore to pick them up./n/n",
					IntroIcon: null,
					Options: itemLabels,
					RespectOptionNewlines: false,
					Icons: itemIcons,
					DefaultSelected: defaultSelected,
					Buttons: menuCommands,
					AllowEscape: true
				);

				switch (selectedIndex) {
					case -1:  // cancel
						yield break;

					case -2:  // toggle all
						var tempList = new List<int>(selectedItems);

						if (selectedItems.Count < options.Length) {
							for (var i = 0; i < sortedOptions.Length; i++) {
								var item = sortedOptions[i];
								if (selectedItems.Contains(item.Index)) continue;
								selectedItems.Add(item.Index);
								itemLabels[i] = PopupUtils.GetItemLabel(true, item, CurrentSortType);
								yield return new ToggledItem(item.Index, true);
							}
						} else {
							for (var i = 0; i < sortedOptions.Length; i++) {
								var item = sortedOptions[i];
								if (!selectedItems.Contains(item.Index)) continue;
								selectedItems.Remove(item.Index);
								itemLabels[i] = PopupUtils.GetItemLabel(false, item, CurrentSortType);
								yield return new ToggledItem(item.Index, false);
							}
						}
						continue;
						
					case -3: // sort
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
