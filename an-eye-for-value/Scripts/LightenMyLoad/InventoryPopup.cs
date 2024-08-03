using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryPopup : BasePopup {
		public int[] ShowPopup(InventoryItem[] options) {
			var defaultSelected = 0;
			var weightSelected = 0;
			var selectedItems = new HashSet<int>();
			
			ResetCache();
			var sortedOptions = SortItems(options);
			IRenderable[] itemIcons = sortedOptions.Select(
				(item) => { return item.Icon; }
			).ToArray();
			string[] itemLabels = sortedOptions.Select((item) => {
				var selected = selectedItems.Contains(item.Index);
				return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
			}).ToArray();

			var dropKey = ControlManager.getCommandInputFormatted("Plaidman_AnEyeForValue_Popup_Drop");
			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					text = "{{W|[" + dropKey + "]}} {{y|Drop Items}}",
					command = "option:-2",
					hotkey = "Plaidman_AnEyeForValue_Popup_Drop"
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType),
					command = "option:-3",
					hotkey = "Plaidman_AnEyeForValue_Popup_Sort"
				},
			};

			while (true) {
				var intro = "Mark items here, then press {{W|[" + dropKey + "]}} to drop them.\n"
				    + "Selected/Carried: {{w|" + weightSelected + "#}}\n\n";
				
				int selectedIndex = Popup.PickOption(
					Title: "Inventory Items",
					Intro: intro,
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
						return null;

					case -2: // drop items
						return selectedItems.ToArray();

					case -3: // sort items
						CurrentSortType = PopupUtils.NextSortType.GetValue(CurrentSortType);

						menuCommands[1].text = PopupUtils.GetSortLabel(CurrentSortType);
						sortedOptions = SortItems(options);
						itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
						itemLabels = sortedOptions.Select((item) => {
							var selected = selectedItems.Contains(item.Index);
							return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
						}).ToArray();

						continue;

					default:
						break;
				}
	
				var mappedItem = sortedOptions[selectedIndex];
				if (selectedItems.Contains(mappedItem.Index)) {
					selectedItems.Remove(mappedItem.Index);
					weightSelected -= mappedItem.Weight;
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(false, mappedItem, CurrentSortType);
				} else {
					selectedItems.Add(mappedItem.Index);
					weightSelected += mappedItem.Weight;
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(true, mappedItem, CurrentSortType);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
