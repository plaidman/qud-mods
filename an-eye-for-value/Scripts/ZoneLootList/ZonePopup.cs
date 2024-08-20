using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public class ZonePopup : BasePopup {
		public PickupType CurrentPickupType;

		public ZonePopup() {
			Comparers = new() {
				{ SortType.Value, new ZoneValueComparer() },
				{ SortType.Weight, new WeightComparer() },
			};
		}

		public IEnumerable<ZonePopupAction> ShowPopup(
			List<InventoryItem> options,
			int[] initialSelections
		) {
			var defaultSelected = 0;
			var weightSelected = 0.0;

			var selectedItems = new HashSet<int>();
			foreach (var item in initialSelections) {
				selectedItems.Add(item);
				weightSelected += options[item].Weight;
			}

			ResetCache();
			int numTakeableItems = options.Count((item) => { return item.Type == ItemType.Takeable; });
			var sortedOptions = SortItemsDescending(options);
			IRenderable[] itemIcons = sortedOptions.Select(
				(item) => { return item.Icon; }
			).ToArray();
			string[] itemLabels = sortedOptions.Select((item) => {
				var selected = selectedItems.Contains(item.Index);
				return PopupUtils.GetItemLabel(selected, item, CurrentSortType);
			}).ToArray();

			var toggleKey = ControlManager.getCommandInputFormatted(XMLStrings.ToggleCommand);
			var sortKey = ControlManager.getCommandInputFormatted(XMLStrings.ZoneSortCommand);
			var pickupKey = ControlManager.getCommandInputFormatted(XMLStrings.PickupCommand);
			QudMenuItem[] menuCommands = new QudMenuItem[]
			{
				new() {
					command = "option:-2",
					hotkey = XMLStrings.ToggleCommand,
				},
				new() {
					text = PopupUtils.GetSortLabel(CurrentSortType, sortKey),
					command = "option:-3",
					hotkey = XMLStrings.ZoneSortCommand,
				},
				new() {
					text = PopupUtils.GetPickupLabel(CurrentPickupType, pickupKey),
					command = "option:-4",
					hotkey = XMLStrings.PickupCommand,
				},
			};

			while (true) {
				var buttonLabel = selectedItems.Count < numTakeableItems
					? "\xffSelect All\xff"
					: "Deselect All";
				menuCommands[0].text = "{{W|[" + toggleKey + "]}} {{y|" + buttonLabel + "}}";

				var intro = "Mark items here, then autoexplore to pick them up.\n"
					+ "Selecting a liquid item ({{c|[\xf7]}}) will auto-travel to that liquid.\n"
					+ "Selected weight: {{w|" + (int)weightSelected + "#}}\n\n";

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

						if (selectedItems.Count < numTakeableItems) {
							for (var i = 0; i < sortedOptions.Count; i++) {
								var item = sortedOptions[i];
								if (selectedItems.Contains(item.Index)) continue;
								if (item.Type == ItemType.Liquid) continue;
								if (item.Type == ItemType.ChestReset) continue;

								if (item.Type == ItemType.Chest) {
									item.Type = ItemType.ChestReset;
									itemLabels[i] = PopupUtils.GetItemLabel(false, item, CurrentSortType);
									yield return new ZonePopupAction(item.Index, ActionType.ResetChest);
									continue;
								}

								selectedItems.Add(item.Index);
								itemLabels[i] = PopupUtils.GetItemLabel(true, item, CurrentSortType);
								weightSelected += item.Weight;
								yield return new ZonePopupAction(item.Index, ActionType.TurnOn);
							}
						} else {
							for (var i = 0; i < sortedOptions.Count; i++) {
								var item = sortedOptions[i];
								if (!selectedItems.Contains(item.Index)) continue;
								if (item.Type == ItemType.Liquid) continue;
								if (item.Type == ItemType.Chest) continue;
								if (item.Type == ItemType.ChestReset) continue;

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

					case -4: // pickup mode
						CurrentPickupType = PopupUtils.NextPickupType.GetValue(CurrentPickupType);
						menuCommands[2].text = PopupUtils.GetPickupLabel(CurrentPickupType, pickupKey);
						yield return new ZonePopupAction(0, ActionType.Sort);
						continue;

					default:
						break;
				}

				var mappedItem = sortedOptions[selectedIndex];
				if (mappedItem.Type == ItemType.Liquid || CurrentPickupType == PickupType.Single) {
					yield return new ZonePopupAction(mappedItem.Index, ActionType.Travel);
					yield break;

				} else if (mappedItem.Type == ItemType.Chest) {
					for (var i = 0; i < sortedOptions.Count; i++) {
						var item = sortedOptions[i];
						if (item.Index == mappedItem.Index) {
							item.Type = ItemType.ChestReset;
							itemLabels[i] = PopupUtils.GetItemLabel(false, item, CurrentSortType);
						}
					}
					yield return new ZonePopupAction(mappedItem.Index, ActionType.ResetChest);

				} else if (mappedItem.Type == ItemType.ChestReset) {
					// do nothing

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
