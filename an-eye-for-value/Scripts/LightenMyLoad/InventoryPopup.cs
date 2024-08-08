using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryPopup : BasePopup {
		private static readonly string SortCommand = "Plaidman_AnEyeForValue_Popup_InvSort";
		private static readonly string DropCommand = "Plaidman_AnEyeForValue_Popup_Drop";

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

			var dropKey = ControlManager.getCommandInputFormatted(DropCommand);
			var sortKey = ControlManager.getCommandInputFormatted(SortCommand);
			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					text = "{{W|[" + dropKey + "]}} {{y|Drop Items}}",
					command = "option:-2",
					hotkey = DropCommand,
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType, sortKey),
					command = "option:-3",
					hotkey = SortCommand,
				},
			};

			while (true) {
				var intro = "Mark items here, then press {{W|[" + dropKey + "]}} to drop them.\n"
					+ "Selected weight: {{w|" + weightSelected + "#}}\n\n";
				
				int selectedIndex = Popup.PickOption(
					Title: "Inventory Items",
					Intro: intro,
					// IntroIcon: Renderable.UITile("an_eye_for_value.png", 'y', 'm'),
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

						menuCommands[1].text = PopupUtils.GetSortLabel(CurrentSortType, sortKey);
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
