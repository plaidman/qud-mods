using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class ZonePopup : BasePopup {
		private static readonly string ToggleCommand = "Plaidman_AnEyeForValue_Popup_Toggle";
		private static readonly string SortCommand = "Plaidman_AnEyeForValue_Popup_ZoneSort";

		public IEnumerable<ZonePopupAction> ShowPopup(
			InventoryItem[] options,
			int[] initialSelections
		) {
			var defaultSelected = 0;
			var weightSelected = 0;

			var selectedItems = new HashSet<int>();
			foreach (var item in initialSelections) {
				selectedItems.Add(item);
				weightSelected += options[item].Weight;
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

			var toggleKey = ControlManager.getCommandInputFormatted(ToggleCommand);
			var sortKey = ControlManager.getCommandInputFormatted(SortCommand);
			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					command = "option:-2",
					hotkey = ToggleCommand
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType, sortKey),
					command = "option:-3",
					hotkey = SortCommand
				},
			};

			while (true) {
				var selectPrefix = selectedItems.Count < options.Length ? "S" : "Des";
				menuCommands[0].text = "{{W|[" + toggleKey + "]}} {{y|" + selectPrefix + "elect All}}";

				var intro = "Mark items here, then autoexplore to pick them up.\n"
				    + "Selected weight: {{w|" + weightSelected + "#}}\n\n";

				int selectedIndex = Popup.PickOption(
					Title: "Lootable Items",
					Intro: intro,
					// IntroIcon: Renderable.UITile("an_eye_for_value.png", 'y', 'r'),
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
								weightSelected += item.Weight;
								yield return new ZonePopupAction(item.Index, ActionType.TurnOn);
							}
						} else {
							for (var i = 0; i < sortedOptions.Length; i++) {
								var item = sortedOptions[i];
								if (!selectedItems.Contains(item.Index)) continue;
								selectedItems.Remove(item.Index);
								itemLabels[i] = PopupUtils.GetItemLabel(false, item, CurrentSortType);
								weightSelected -= item.Weight;
								yield return new ZonePopupAction(item.Index, ActionType.TurnOff);
							}
						}
						continue;
						
					case -3: // sort
						CurrentSortType = PopupUtils.NextSortType.GetValue(CurrentSortType);

						menuCommands[1].text = PopupUtils.GetSortLabel(CurrentSortType, sortKey);
						sortedOptions = SortItemsDescending(options);
						itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
						itemLabels = sortedOptions.Select((item) => {
							var selected = selectedItems.Contains(item.Index);
							return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
						}).ToArray();

						yield return new ZonePopupAction(0, ActionType.Sort);
						continue;

					default:
						break;
				}
				
				var mappedItem = sortedOptions[selectedIndex];
				if (mappedItem.Liquids) {
					yield return new ZonePopupAction(mappedItem.Index, ActionType.Travel);
					yield break;
				} else if (selectedItems.Contains(mappedItem.Index)) {
					selectedItems.Remove(mappedItem.Index);
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(false, mappedItem, CurrentSortType);
					weightSelected -= mappedItem.Weight;
					yield return new ZonePopupAction(mappedItem.Index, ActionType.TurnOff);
				} else {
					selectedItems.Add(mappedItem.Index);
					itemLabels[selectedIndex] = PopupUtils.GetItemLabel(true, mappedItem, CurrentSortType);
					weightSelected += mappedItem.Weight;
					yield return new ZonePopupAction(mappedItem.Index, ActionType.TurnOn);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
