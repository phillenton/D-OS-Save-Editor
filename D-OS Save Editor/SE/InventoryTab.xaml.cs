using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace D_OS_Save_Editor
{
    /// <summary>
    /// Interaction logic for Inventory.xaml
    /// </summary>
    public partial class InventoryTab
    {
        private Player _player;
        private bool _suppressInventoryDetailEvents;

        /// <summary>Index into <see cref="Player.Items"/> for the selected tree row, or -1.</summary>
        private int _selectedItemIndex = -1;

        private Brush DefaultTextBoxBorderBrush { get; }
        private Brush[] _itemRarityColor =
            {Brushes.Black, Brushes.ForestGreen, Brushes.DodgerBlue, Brushes.BlueViolet, Brushes.DeepPink, Brushes.Gold, Brushes.DimGray};

        public Player Player
        {
            get => _player;

            set
            {
                _player = value;
                UpdateForm();
            }
        }

        public InventoryTab()
        {
            InitializeComponent();
            DefaultTextBoxBorderBrush = AmountTextBox.BorderBrush;

            RarityComboBox.ItemsSource = Enum.GetValues(typeof(Item.ItemRarityType)).Cast<Item.ItemRarityType>();
        }

        public bool HasPendingItemDetailEdits()
        {
            if (_selectedItemIndex < 0 || _selectedItemIndex >= Player.Items.Length) return false;
            var item = Player.Items[_selectedItemIndex];
            var allowed = item.GetAllowedChangeType();

            if (allowed.Contains(nameof(item.Amount)) && AmountTextBox.Text != item.Amount) return true;
            if (allowed.Contains(nameof(item.LockLevel)) && LockLevelTextBox.Text != item.LockLevel) return true;
            if (allowed.Contains(nameof(item.Vitality)) &&
                (VitalityTextBox.Text != item.Vitality || MaxVitalityPatchCheckTextBox.Text != item.MaxVitalityPatchCheck))
                return true;
            if (allowed.Contains(nameof(item.ItemRarity)) && RarityComboBox.SelectedIndex >= 0 &&
                (int)item.ItemRarity != RarityComboBox.SelectedIndex) return true;
            if (allowed.Contains(nameof(item.Stats)) && item.Stats != null)
            {
                if (DurabilityTextBox.Text != (item.Stats.Durability ?? "")) return true;
                if (DurabilityCounterTextBox.Text != (item.Stats.DurabilityCounter ?? "")) return true;
                if (RepairDurabilityPenaltyTextBox.Text != (item.Stats.RepairDurabilityPenalty ?? "")) return true;
                if (LevelTextBox.Text != (item.Stats.Level ?? "")) return true;
            }
            if (allowed.Contains(nameof(item.Generation)))
            {
                var uiBoosts = BoostsListBox.Items.Cast<string>().ToList();
                var gen = item.Generation;
                if (gen == null || gen.Boosts == null)
                {
                    if (uiBoosts.Count > 0) return true;
                }
                else if (!uiBoosts.SequenceEqual(gen.Boosts)) return true;
            }
            return false;
        }

        public void RefreshInventoryApplyUi()
        {
            var pending = _selectedItemIndex >= 0 && HasPendingItemDetailEdits();
            ApplyChangesButton.IsEnabled = pending;
            if (InventoryApplyPendingLabel != null)
                InventoryApplyPendingLabel.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InventoryDetailField_Changed(object sender, TextChangedEventArgs e)
        {
            if (_suppressInventoryDetailEvents) return;
            if (sender is TextBox tb && tb.Uid == "SearchText") return;
            if (Window.GetWindow(this) is SaveEditor se)
            {
                RefreshInventoryApplyUi();
            }
        }

        private void RarityComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressInventoryDetailEvents) return;
            RefreshInventoryApplyUi();
        }

        public void UpdateForm()
        {
            _selectedItemIndex = -1;
            ItemsTreeView.Items.Clear();
            EquipmentSlotText.Text = "";
            EquipmentSlotLabel.Visibility = Visibility.Collapsed;
            EquipmentSlotText.Visibility = Visibility.Collapsed;

            if (Player?.Items == null || Player.Items.Length == 0)
            {
                foreach (var i in ShowWrapPanel.Children)
                {
                    if (i is CheckBox)
                        CheckboxEventSetter_OnClick(i, new RoutedEventArgs());
                }
                foreach (var i in ValueWrapPanel.Children)
                {
                    if (i is TextBox t)
                        t.Text = "";
                }
                RefreshInventoryApplyUi();
                return;
            }

            RebuildItemTree();

            foreach (var i in ShowWrapPanel.Children)
            {
                if (i is CheckBox)
                    CheckboxEventSetter_OnClick(i, new RoutedEventArgs());
            }

            foreach (var i in ValueWrapPanel.Children)
            {
                if (i is TextBox t)
                    t.Text = "";
            }
            RefreshInventoryApplyUi();
        }

        private void RebuildItemTree()
        {
            ItemsTreeView.Items.Clear();
            var byParent = Player.Items
                .GroupBy(it => it.Parent)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => int.Parse(x.Slot)).ToList());

            if (!byParent.TryGetValue(Player.InventoryId, out var roots))
                return;

            foreach (var root in roots)
                ItemsTreeView.Items.Add(BuildTreeItem(root, byParent));
        }

        private TreeViewItem BuildTreeItem(Item item, Dictionary<string, List<Item>> byParent)
        {
            var header = FormatItemTreeLabel(item);
            var tvi = new TreeViewItem
            {
                Header = header,
                Tag = item,
                IsExpanded = false,
                Foreground = _itemRarityColor[(int)item.ItemRarity]
            };

            var nested = item.NestedInventoryId;
            if (!string.IsNullOrEmpty(nested) && nested != "0" && byParent.TryGetValue(nested, out var kids))
            {
                foreach (var ch in kids)
                    tvi.Items.Add(BuildTreeItem(ch, byParent));
            }

            return tvi;
        }

        private string FormatItemTreeLabel(Item item)
        {
            var equipped = item.IsEquippedPaperDoll(Player?.InventoryId) ? "\U0001F464 " : "";
            if (!string.IsNullOrWhiteSpace(item.DisplayName))
                return $"{equipped}{item.DisplayName}  ({item.StatsName})";
            return $"{equipped}{item.StatsName}";
        }

        private static string GetItemSearchText(Item item)
        {
            var s = (item.DisplayName ?? "") + " " + (item.StatsName ?? "");
            return s.Trim().ToLowerInvariant();
        }

        private void ItemsTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem tvi && tvi.Tag is Item item)
            {
                _selectedItemIndex = item.ItemXmlNodeIdx;
                // Tag can be stale after Apply (clone replaces Player.Items[i]); always use canonical row.
                if (Player?.Items != null && _selectedItemIndex >= 0 && _selectedItemIndex < Player.Items.Length)
                    PopulateItemDetail(Player.Items[_selectedItemIndex]);
                else
                    PopulateItemDetail(item);
            }
            else
            {
                _selectedItemIndex = -1;
            }
        }

        private void PopulateItemDetail(Item item)
        {
            _suppressInventoryDetailEvents = true;
            try
            {
                BoostsListBox.Items.Clear();
                PermBoostsListBox.Items.Clear();

                if (item.IsEquippedPaperDoll(Player?.InventoryId))
                {
                    EquipmentSlotLabel.Visibility = Visibility.Visible;
                    EquipmentSlotText.Visibility = Visibility.Visible;
                    EquipmentSlotText.Text = item.Slot;
                }
                else
                {
                    EquipmentSlotLabel.Visibility = Visibility.Collapsed;
                    EquipmentSlotText.Visibility = Visibility.Collapsed;
                    EquipmentSlotText.Text = "";
                }

                var allowedChanges = item.GetAllowedChangeType();

                if (allowedChanges.Contains(nameof(item.Vitality)))
                {
                    VitalityTextBox.IsEnabled = true;
                    MaxVitalityPatchCheckTextBox.IsEnabled = true;
                }
                else
                {
                    VitalityTextBox.IsEnabled = false;
                    MaxVitalityPatchCheckTextBox.IsEnabled = false;
                }

                RarityComboBox.IsEnabled = allowedChanges.Contains(nameof(item.ItemRarity));
                AmountTextBox.IsEnabled = allowedChanges.Contains(nameof(item.Amount));
                LockLevelTextBox.IsEnabled = allowedChanges.Contains(nameof(item.LockLevel));
                BoostsListBox.IsEnabled = allowedChanges.Contains(nameof(item.Generation));

                if (allowedChanges.Contains(nameof(item.Stats)))
                {
                    DurabilityTextBox.IsEnabled = true;
                    MaxDurabilityPatchCheckTextBox.IsEnabled = false;
                    DurabilityCounterTextBox.IsEnabled = true;
                    RepairDurabilityPenaltyTextBox.IsEnabled = true;
                    LevelTextBox.IsEnabled = true;
                    PermBoostsListBox.IsEnabled = true;
                }
                else
                {
                    DurabilityTextBox.IsEnabled = false;
                    MaxDurabilityPatchCheckTextBox.IsEnabled = false;
                    DurabilityCounterTextBox.IsEnabled = false;
                    RepairDurabilityPenaltyTextBox.IsEnabled = false;
                    LevelTextBox.IsEnabled = false;
                    PermBoostsListBox.IsEnabled = false;
                }

#if DEBUG && LOG_ITEMXML
                Console.WriteLine(item.Xml);
#endif
                AmountTextBox.Text = item.Amount;
                LockLevelTextBox.Text = item.LockLevel;
                VitalityTextBox.Text = item.Vitality;
                MaxVitalityPatchCheckTextBox.Text = item.MaxVitalityPatchCheck;
                DurabilityTextBox.Text = item.Stats?.Durability ?? "";
                DurabilityCounterTextBox.Text = item.Stats?.DurabilityCounter ?? "";
                MaxDurabilityPatchCheckTextBox.Text = item.MaxDurabilityPatchCheck;
                RepairDurabilityPenaltyTextBox.Text = item.Stats?.RepairDurabilityPenalty ?? "";
                LevelTextBox.Text = item.Stats?.Level ?? "";

                RarityComboBox.SelectedIndex = (int)item.ItemRarity;

                if (item.Generation != null)
                {
                    foreach (var m in item.Generation.Boosts)
                        BoostsListBox.Items.Add(m);
                }

                if (item.Stats != null)
                {
                    foreach (var m in item.Stats.PermanentBoost)
                        PermBoostsListBox.Items.Add($"{m.Key} - {m.Value}");
                }
            }
            finally
            {
                _suppressInventoryDetailEvents = false;
            }
            RefreshInventoryApplyUi();
        }

        private void CheckboxEventSetter_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyTreeFilter();
        }

        private void ApplyTreeFilter()
        {
            foreach (TreeViewItem root in ItemsTreeView.Items)
                ApplyTreeFilterRecursive(root);
        }

        private bool ApplyTreeFilterRecursive(TreeViewItem node)
        {
            var item = node.Tag as Item;
            if (item == null) return false;

            var selfVisible = IsCheckboxPassForItem(item) && !IsFilteredOutByText(GetItemSearchText(item));
            var anyChild = false;
            foreach (var childObj in node.Items)
            {
                if (childObj is TreeViewItem child)
                    anyChild |= ApplyTreeFilterRecursive(child);
            }

            var show = selfVisible || anyChild;
            node.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            return show;
        }

        private bool IsCheckboxPassForItem(Item item)
        {
            if (item.ItemSort == ItemSortType.Item || item.ItemSort == ItemSortType.Unique ||
                item.ItemSort == ItemSortType.Other)
                return ShowOtherCheckBox.IsChecked == true;

            foreach (var c in ShowWrapPanel.Children.OfType<CheckBox>())
            {
                if (c.Tag is ItemSortType t && t == item.ItemSort)
                    return c.IsChecked == true;
            }

            return false;
        }

        private bool IsFilteredOutByText(string searchBlob)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text)) return false;
            var searchTerms = SearchTextBox.Text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in searchTerms)
            {
                if (!searchBlob.Contains(s)) return true;
            }

            return false;
        }

        private void CheckAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var i in ShowWrapPanel.Children)
            {
                if (!(i is CheckBox box)) continue;
                box.IsChecked = true;
                CheckboxEventSetter_OnClick(i, new RoutedEventArgs());
            }
        }

        private void UncheckAllButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var i in ShowWrapPanel.Children)
            {
                if (!(i is CheckBox box)) continue;
                box.IsChecked = false;
                CheckboxEventSetter_OnClick(i, new RoutedEventArgs());
            }
        }

        private void ApplyChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!TryApplySelectedItemChanges(sender as Button))
                return;
        }

        /// <summary>
        /// Used by main Save to commit inventory UI before writing globals.lsx.
        /// </summary>
        public bool TryApplyPendingInventoryForMainSave()
        {
            if (!HasPendingItemDetailEdits()) return true;
            return TryApplySelectedItemChanges(null);
        }

        private static string ItemChangeKey(Item item) => item.ItemXmlNodeIdx.ToString();

        private bool TryApplySelectedItemChanges(Button toolTipButton)
        {
            if (_selectedItemIndex < 0 || _selectedItemIndex >= Player.Items.Length)
                return false;

            try
            {
                var previousTreeRef = Player.Items[_selectedItemIndex];
                var item = previousTreeRef.DeepClone();
                var allowedChanges = item.GetAllowedChangeType();
                if (allowedChanges.Contains(nameof(item.Amount)))
                    item.Amount = AmountTextBox.Text;

                if (allowedChanges.Contains(nameof(item.LockLevel)))
                    item.LockLevel = LockLevelTextBox.Text;

                if (allowedChanges.Contains(nameof(item.Vitality)))
                {
                    item.Vitality = VitalityTextBox.Text;
                    item.MaxVitalityPatchCheck = MaxVitalityPatchCheckTextBox.Text;
                }

                if (allowedChanges.Contains(nameof(item.ItemRarity)))
                    item.ItemRarity = (Item.ItemRarityType)RarityComboBox.SelectedIndex;

                if (allowedChanges.Contains(nameof(item.Stats)))
                {
                    item.Stats.Durability = DurabilityTextBox.Text;
                    item.Stats.DurabilityCounter = DurabilityCounterTextBox.Text;
                    item.Stats.RepairDurabilityPenalty = RepairDurabilityPenaltyTextBox.Text;
                    item.Stats.Level = LevelTextBox.Text;
                }

                if (allowedChanges.Contains(nameof(item.Generation)))
                {
                    if (item.Generation == null)
                    {
                        if (item.Stats == null)
                        {
                            var xmlSerializer = new XmlSerializer(item.GetType());

                            using (var sw = new StringWriter())
                            {
                                xmlSerializer.Serialize(sw, item);
                                var er = new ErrorReporting("It is not possible to add modifier to this item yet. No changes have been applied.", $"Item.Stats Null. Cannot add item modifiers.\n\nItem XML:\n{sw}", null);
                                er.ShowDialog();
                            }
                            return false;
                        }
                        item.Generation = new Item.GenerationNode(item.StatsName, "0");
                    }

                    item.Generation.Boosts = new List<string>();
                    foreach (string s in BoostsListBox.Items)
                        item.Generation.Boosts.Add(s);
                }

                var key = ItemChangeKey(item);
                if (Player.ItemChanges.ContainsKey(key))
                    Player.ItemChanges[key] = new ItemChange(item, Player.ItemChanges[key].ChangeType);
                else
                    Player.ItemChanges.Add(key, new ItemChange(item, ChangeType.Modify));

                Player.Items[_selectedItemIndex] = item;

                var tvi = FindTreeViewItemForItem(previousTreeRef);
                if (tvi != null)
                {
                    tvi.Tag = item;
                    tvi.Foreground = _itemRarityColor[(int)item.ItemRarity];
                }

                if (toolTipButton != null)
                {
                    var tooltip = new ToolTip { Content = "Changes have been applied!" };
                    toolTipButton.ToolTip = tooltip;
                    tooltip.Opened += async delegate (object o, RoutedEventArgs args)
                    {
                        var s = o as ToolTip;
                        await Task.Delay(1000);
                        s.IsOpen = false;
                        await Task.Delay(1000);
                        toolTipButton.ClearValue(ToolTipProperty);
                    };
                    tooltip.IsOpen = true;
                }

                RefreshInventoryApplyUi();

                if (Window.GetWindow(this) is SaveEditor se)
                    se.MarkUnsavedSessionChanges();
                return true;
            }
            catch (XmlValidationException ex)
            {
                MessageBox.Show($"Invalid value entered: {ex.Name}: {ex.Value}. No change has been applied.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Internal error. No change has been applied.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private TreeViewItem FindTreeViewItemForItem(Item target)
        {
            foreach (TreeViewItem root in ItemsTreeView.Items)
            {
                var f = FindTreeViewItemRecursive(root, target);
                if (f != null) return f;
            }
            return null;
        }

        private static TreeViewItem FindTreeViewItemRecursive(TreeViewItem node, Item target)
        {
            if (node.Tag is Item it && ReferenceEquals(it, target))
                return node;
            foreach (TreeViewItem child in node.Items)
            {
                var f = FindTreeViewItemRecursive(child, target);
                if (f != null) return f;
            }
            return null;
        }

        private void BoostsContextMenu_Click(object sender, RoutedEventArgs e)
        {
            switch (((MenuItem)sender).Header)
            {
                case "Add":
                    var predictedKeyword = "";
                    var boostsString = BoostsListBox.Items.Cast<string>().Aggregate("", (current, s) => current + s);
                    if (_selectedItemIndex >= 0 && _selectedItemIndex < Player.Items.Length)
                        boostsString += Player.Items[_selectedItemIndex].StatsName.ToLower();

                    if (boostsString != "")
                        foreach (var s in DataTable.GenerationBoostsFilterNames)
                        {
                            if (!boostsString.Contains(s)) continue;
                            switch (s)
                            {
                                case "arm":
                                    predictedKeyword += "armor ";
                                    break;
                                case "wpn":
                                    predictedKeyword += "weapon ";
                                    break;
                                default:
                                    predictedKeyword += s + " ";
                                    break;
                            }
                        }

                    var dlg = new AddBoostDialog(predictedKeyword);
                    dlg.ShowDialog();
                    if (dlg.DialogResult == true)
                    {
                        BoostsListBox.Items.Add(dlg.BoostText);
                        RefreshInventoryApplyUi();
                    }
                    break;
                case "Copy text":
                    Clipboard.SetText((string)BoostsListBox.SelectedValue);
                    break;
                case "Delete":
                    BoostsListBox.Items.RemoveAt(BoostsListBox.SelectedIndex);
                    RefreshInventoryApplyUi();
                    break;
            }
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyTreeFilter();
        }

        private void TextBoxEventSetter_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox s)) return;
            if (s.Uid == "SearchText") return;

            var text = s.Text;
            var valid = int.TryParse(text, out int _);
            s.BorderBrush = !valid ? Brushes.Red : DefaultTextBoxBorderBrush;
        }

        private void TextBoxEventSetter_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox s)) return;
            if (s.Uid == "SearchText") return;

            var text = s.Text.Insert(s.SelectionStart, e.Text);
            e.Handled = !int.TryParse(text, out int _);
        }
    }
}
