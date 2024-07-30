using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using Qud.UI;
using ConsoleLib.Console;

namespace Plaidman.LightenMyLoad.Menus {
	public class ItemListPopup {
		public enum SortType { Weight, Value };

		public SortType CurrentSortType;
		private Dictionary<SortType, InventoryItem[]> ItemListCache;
		private readonly Dictionary<SortType, IComparer<InventoryItem>> Comparers;
		private readonly Dictionary<SortType, string> SortStrings;
		private readonly Dictionary<SortType, SortType> NextSortType;
		
		public ItemListPopup() {
			Comparers = new() {
				{ SortType.Value, new ValueComparer() },
				{ SortType.Weight, new WeightComparer() },
			};
			SortStrings = new() {
				{ SortType.Value, "value" },
				{ SortType.Weight, "weight" },
			};
			NextSortType = new() {
				{ SortType.Value, SortType.Weight },
				{ SortType.Weight, SortType.Value },
			};
		}

		private void ResetCache() {
			ItemListCache = new() {
				{ SortType.Value, null },
				{ SortType.Weight, null },
			};
		}
		
		private InventoryItem[] ChangeSort(InventoryItem[] items, SortType sortType) {
			var cache = ItemListCache.GetValue(sortType);
			
			if (cache == null) {
				cache = items.OrderBy(item => item, Comparers.GetValue(sortType)).ToArray();
				ItemListCache.Set(sortType, cache);
			}
			
			return cache;
		}

		private string GetItemLabel(bool selected, InventoryItem item) {
			var label = PopupLabelUtils.GetSelectionLabel(selected) + " ";
			
			switch (CurrentSortType) {
				case SortType.Value:
					label += PopupLabelUtils.GetValueLabel(item) + " ";
					break;

				case SortType.Weight:
					label += PopupLabelUtils.GetWeightLabel(item) + " ";
					break;
			}

			return label + item.DisplayName;
		}
		
		private string GetSortLabel() {
			return "{{W|[Tab]}} {{y|Sort Mode: " + SortStrings.GetValue(CurrentSortType) + "}}";
		}
		
		public int[] ShowPopup(InventoryItem[] options) {
			var defaultSelected = 0;
			var weightSelected = 0;
			var selectedItems = new HashSet<int>();
			
			ResetCache();
			var sortedOptions = ChangeSort(options, CurrentSortType);
			IRenderable[] itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
			string[] itemLabels = sortedOptions.Select((item) => {
				var selected = selectedItems.Contains(item.Index);
				return GetItemLabel(selected, item);
			}).ToArray();

			QudMenuItem[] menuCommands = new QudMenuItem[2]
			{
				new() {
					text = "{{W|[D]}} {{y|Drop Items}}",
					command = "option:-2",
					hotkey = "D"
				},
				new() {
					text = GetSortLabel(),
					command = "option:-3",
					hotkey = "Tab"
				},
			};

			while (true) {
				var intro = "Mark items here, press 'd' to drop them.\n";
				intro += "Selected Item Weight: {{w|" + weightSelected + "#}}\n\n";
				
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
					case -1:  // Esc / Cancelled
						return null;

					case -2: // D drop items
						return selectedItems.ToArray();

					default:
						break;
				}
				
				if (selectedIndex == -3) {
					CurrentSortType = NextSortType.GetValue(CurrentSortType);

					menuCommands[1].text = GetSortLabel();
					sortedOptions = ChangeSort(options, CurrentSortType);
					itemIcons = sortedOptions.Select((item) => { return item.Icon; }).ToArray();
					itemLabels = sortedOptions.Select((item) => {
						var selected = selectedItems.Contains(item.Index);
						return GetItemLabel(selected, item);
					}).ToArray();
					
					continue;
				}

				var mappedItem = sortedOptions[selectedIndex];
				if (selectedItems.Contains(mappedItem.Index)) {
					selectedItems.Remove(mappedItem.Index);
					weightSelected -= mappedItem.Weight;
					itemLabels[selectedIndex] = GetItemLabel(false, mappedItem);
				} else {
					selectedItems.Add(mappedItem.Index);
					weightSelected += mappedItem.Weight;
					itemLabels[selectedIndex] = GetItemLabel(true, mappedItem);
				}

				defaultSelected = selectedIndex;
			}
		}
	}
};
