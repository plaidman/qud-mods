using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryPopup : BasePopup {
		public InventoryPopup() {
			Comparers = new() {
				{ SortType.Value, new InventoryValueComparer() },
				{ SortType.Weight, new WeightComparer() },
			};
		}

		public int[] ShowPopup(List<InventoryItem> options) {
			var defaultSelected = 0;
			var weightSelected = 0.0;
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

			var dropKey = ControlManager.getCommandInputFormatted(XMLStrings.DropCommand);
			var sortKey = ControlManager.getCommandInputFormatted(XMLStrings.InvSortCommand);
			var menuCommands = new QudMenuItem[] {
				new() {
					text = "{{W|[" + dropKey + "]}} {{y|Drop Items}}",
					command = "option:-2",
					hotkey = XMLStrings.DropCommand,
				},
				new() {
					text = "{{W|[" + sortKey + "]}} {{y|Sort Mode}}",
					command = "option:-3",
					hotkey = XMLStrings.InvSortCommand,
				},
			};

			while (true) {
				var sortModeString = PopupUtils.SortStrings.GetValue(CurrentSortType);
				var intro = "Mark items here, then press {{W|[" + dropKey + "]}} to drop them.\n"
					+ "[Selected Weight: {{w|" + (int)weightSelected + "#}}]\xff\xff\xff"
					+ sortModeString + "\n\n";

				int selectedIndex;
				if (!CheckUIViewsLoaded()) {
					selectedIndex = -1;
				}
                else {
					s_OverridePopup = true; // Use our custom UIViews for Popup::PickOption()
					selectedIndex = Popup.PickOption(
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
					s_OverridePopup = false; // Should already be false after PickOption(), just adding this here as a backup
				}

				switch (selectedIndex) {
					case -1:  // cancel
						return null;

					case -2: // drop items
						return selectedItems.ToArray();

					case -3: // sort items
						CurrentSortType = PopupUtils.NextSortType.GetValue(CurrentSortType);
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
