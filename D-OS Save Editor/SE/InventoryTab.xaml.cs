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
            if (ItemsListBox.SelectedIndex < 0) return false;
            var item = Player.Items[ItemsListBox.SelectedIndex];
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
            var pending = ItemsListBox.SelectedIndex >= 0 && HasPendingItemDetailEdits();
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
            ItemsListBox.Items.Clear();
            foreach (var i in Player.Items)
                ItemsListBox.Items.Add(new ListBoxItem
                {
                    Content = i.StatsName,
                    Tag = i.ItemSort,
                    Foreground = _itemRarityColor[(int)i.ItemRarity]
                });

            // check filter
            foreach (var i in ShowWrapPanel.Children)
            {
                if (i is CheckBox)
                    CheckboxEventSetter_OnClick(i, new RoutedEventArgs());
            }

            // clear all text boxes
            foreach(var i in ValueWrapPanel.Children)
            {
                if (i is TextBox t)
                    t.Text = "";
            }
            RefreshInventoryApplyUi();
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

        private void ItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _suppressInventoryDetailEvents = true;
            try
            {
            // clear list boxes
            BoostsListBox.Items.Clear();
            PermBoostsListBox.Items.Clear();

            var lb = sender as ListBox;
            if (lb.SelectedIndex < 0)
                return;
            var item = Player.Items[lb.SelectedIndex];


            var allowedChanges = item.GetAllowedChangeType();
            #region enable disable controls

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
            #endregion

#if DEBUG && LOG_ITEMXML
            Console.WriteLine(item.Xml);
#endif
            // textbox contents
            AmountTextBox.Text = item.Amount;
            LockLevelTextBox.Text = item.LockLevel;
            VitalityTextBox.Text = item.Vitality;
            MaxVitalityPatchCheckTextBox.Text = item.MaxVitalityPatchCheck;
            DurabilityTextBox.Text = item.Stats?.Durability ?? "";
            DurabilityCounterTextBox.Text = item.Stats?.DurabilityCounter ?? "";
            MaxDurabilityPatchCheckTextBox.Text = item.MaxDurabilityPatchCheck;
            RepairDurabilityPenaltyTextBox.Text = item.Stats?.RepairDurabilityPenalty ?? "";
            LevelTextBox.Text = item.Stats?.Level ?? "";

            // combobox
            RarityComboBox.SelectedIndex = (int) item.ItemRarity;

            // generation
            if (item.Generation != null)
            {
                foreach (var m in item.Generation.Boosts)
                {
                    BoostsListBox.Items.Add(m);
                }
            }

            // stats
            if (item.Stats != null)
            {
                foreach (var m in item.Stats.PermanentBoost)
                {
                    PermBoostsListBox.Items.Add($"{m.Key} - {m.Value}");
                }
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
            var ckb = sender as CheckBox;
            if (!(ckb?.Tag is ItemSortType))
                return;

            if ((ItemSortType) ckb.Tag == ItemSortType.Other)
            {
                foreach (ListBoxItem i in ItemsListBox.Items)
                {
                    if ((ItemSortType) i.Tag == ItemSortType.Item || (ItemSortType) i.Tag == ItemSortType.Unique ||
                        (ItemSortType) i.Tag == ItemSortType.Other)
                        i.Visibility = ckb.IsChecked == true && !IsFilteredOutByText(i.Content as string) ? Visibility.Visible:Visibility.Collapsed;
                }
            }
            else
            {
                foreach (ListBoxItem i in ItemsListBox.Items)
                {
                    if ((ItemSortType)i.Tag == (ItemSortType)ckb.Tag)
                        i.Visibility = ckb.IsChecked == true && !IsFilteredOutByText(i.Content as string) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private bool IsFilteredOutByText(string itemName)
        {
            var isFilteredOut = false;
            itemName = itemName.ToLower();
            var searchTerms = SearchTextBox.Text.ToLower().Split(' ');
            foreach (var s in searchTerms)
            {
                if (itemName.Contains(s)) continue;

                isFilteredOut = true;
                break;
            }

            return isFilteredOut;
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

        private bool TryApplySelectedItemChanges(Button toolTipButton)
        {
            if (ItemsListBox.SelectedIndex < 0)
                return false;

            try
            {
                // apply changes to a copy of the item
                var item = Player.Items[ItemsListBox.SelectedIndex].DeepClone();
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
                    item.ItemRarity = (Item.ItemRarityType) RarityComboBox.SelectedIndex;

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
                        //TODO if Item.Stats is null, an error will occur later on when writing xml because Geneartion.Level is taken from Stats.Level.
                        //Since Item.Stats == null is not likely to be possible, let's handle it later on if we have an report of this case.
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
                    {
                        item.Generation.Boosts.Add(s);
                    }
                }

                // add changes
                if (Player.ItemChanges.ContainsKey(item.Slot))
                {
                    Player.ItemChanges[item.Slot] = new ItemChange(item, Player.ItemChanges[item.Slot].ChangeType);
                }
                else
                {
                    Player.ItemChanges.Add(item.Slot,
                        new ItemChange(item, ChangeType.Modify));
                }

                // apply changes to the original item
                Player.Items[ItemsListBox.SelectedIndex] = item;

                // change colour
                ((ListBoxItem) ItemsListBox.Items[ItemsListBox.SelectedIndex]).Foreground =
                    _itemRarityColor[(int) item.ItemRarity];

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

        private void BoostsContextMenu_Click(object sender, RoutedEventArgs e)
        {
            switch (((MenuItem)sender).Header)
            {
                case "Add":
                    // try pre-determine equipment type
                    var predictedKeyword = "";
                    var boostsString= BoostsListBox.Items.Cast<string>().Aggregate("", (current, s) => current + s);
                    boostsString += Player.Items[ItemsListBox.SelectedIndex].StatsName.ToLower();

                    if (boostsString!="")
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

                    var dlg=new AddBoostDialog(predictedKeyword);
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
            foreach (ListBoxItem i in ItemsListBox.Items)
            {
                var listBoxText = ((string)i.Content).ToLower();
                var searchTerms = SearchTextBox.Text.ToLower().Split(' ');
                var visiblily = Visibility.Visible;
                foreach (var s in searchTerms)
                {
                    if (listBoxText.Contains(s)) continue;

                    visiblily = Visibility.Collapsed;
                    break;
                }
                i.Visibility = visiblily;
            }
        }
    }
}
