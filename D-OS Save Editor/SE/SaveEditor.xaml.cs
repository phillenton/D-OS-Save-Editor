using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace D_OS_Save_Editor
{
    /// <summary>
    /// Interaction logic for SaveEditor.xaml
    /// </summary>
    public partial class SaveEditor
    {
        private Savegame Savegame { get; set; }
        private Player[] EditingPlayers { get; set; }

        private string _baseTitle;
        private bool _hasUnsavedChanges;

        public SaveEditor(string jsonFile)
        {
            InitializeComponent();

            Savegame = Savegame.GetSavegameFromJson(jsonFile);
            // make a copy of players
            try
            {
                EditingPlayers = Savegame.Players.Select(a => a?.DeepClone()).ToArray();
            }
            catch (Exception ex)
            {
                var er = new ErrorReporting($"Fail to clone players.\n\n{ex}", null);
                er.ShowDialog();
                throw;
            }

            foreach (var p in Savegame.Players)
            {
                PlayerSelectionComboBox.Items.Add(p.Name);
            }

            PlayerSelectionComboBox.SelectedIndex = 0;
            _baseTitle = Title;
            SetUnsavedChanges(false);
        }

        public SaveEditor(Savegame savegame)
        {
            InitializeComponent();
            Savegame = savegame;

            _baseTitle = $"D-OS Save Editor: {savegame.SavegameName.Substring(0, savegame.SavegameName.Length - 4)}";
            Title = _baseTitle;

            // make a copy of players
            try
            {
                EditingPlayers = Savegame.Players.Select(a => a?.DeepClone()).ToArray();
            }
            catch (Exception ex)
            {
                var er = new ErrorReporting($"Fail to clone players.\n\n{ex}", null);
                er.ShowDialog();
                throw;
            }

            foreach (var p in Savegame.Players)
            {
                PlayerSelectionComboBox.Items.Add(p.Name);
            }

            PlayerSelectionComboBox.SelectedIndex = 0;
            SetUnsavedChanges(false);
        }

        /// <summary>
        /// Call when tab Apply or inventory apply commits edits to the in-memory save (still need Save to write .lsv).
        /// </summary>
        public void MarkUnsavedSessionChanges()
        {
            SetUnsavedChanges(true);
        }

        private void SetUnsavedChanges(bool dirty)
        {
            _hasUnsavedChanges = dirty;
            if (!string.IsNullOrEmpty(_baseTitle))
                Title = dirty ? $"{_baseTitle} *" : _baseTitle;
            if (UnsavedChangesLabel != null)
                UnsavedChangesLabel.Visibility = dirty ? Visibility.Visible : Visibility.Collapsed;
        }

        public void RefreshCharacterApplyPendingState()
        {
            var pending = StatsTab.HasPendingEdits() || AbilitiesTab.HasPendingEdits() || TraitsTab.HasPendingEdits() ||
                          TalentTab.HasPendingEdits();
            SavePlayer.IsEnabled = pending;
            if (CharacterApplyPendingLabel != null)
                CharacterApplyPendingLabel.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowContent(int id)
        {
            StatsTab.Player = EditingPlayers[id];
            AbilitiesTab.Player = EditingPlayers[id];
            InventoryTab.Player = EditingPlayers[id];
            TraitsTab.Player = EditingPlayers[id];
            TalentTab.Player = EditingPlayers[id];

            if (EditingPlayers[id].Name == "Henchman")
            {
                //TraitsTab.IsEnabled = false;
            }
            RefreshCharacterApplyPendingState();
            InventoryTab.RefreshInventoryApplyUi();
        }

        private void PlayerSelectionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowContent(PlayerSelectionComboBox.SelectedIndex);
        }

        private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            try
            {
                Cursor = Cursors.Wait;
                if (!InventoryTab.TryApplyPendingInventoryForMainSave())
                {
                    Cursor = Cursors.Arrow;
                    SaveButton.IsEnabled = true;
                    return;
                }
                StatsTab.SaveEdits();
                AbilitiesTab.SaveEdits();
                TraitsTab.SaveEdits();
                TalentTab.SaveEdits();

                // progress indicator
                var progressIndicator = new ProgressIndicator("Saving", false) { Owner = Application.Current.MainWindow };
                var progress = new Progress<string>();
                progress.ProgressChanged += (o, s) =>
                {
                    progressIndicator.ProgressText = s;
                };
                progressIndicator.Show();

                // apply changes
                Savegame.Players = EditingPlayers;
                await Savegame.WriteEditsToLsxAsync(progress);
                // pack up files
                await Savegame.PackSavegameAsync(progress);

                SetUnsavedChanges(false);

                progressIndicator.ProgressText = "Successful.";
                progressIndicator.CanCancel = true;
                progressIndicator.CancelButtonText = "Close";

                DialogResult = true;
            }
            catch (Exception ex)
            {
                SaveButton.IsEnabled = true;
                var er = new ErrorReporting($"Failed to save changes.\n\n{ex}", null);
                er.ShowDialog();
            }
            finally
            {
                Cursor = Cursors.Arrow;
                SaveButton.IsEnabled = true;
            }
        }

        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditingPlayers = Savegame.Players.Select(a => a.DeepClone()).ToArray();
            StatsTab.UpdateForm();
            AbilitiesTab.UpdateForm();
            InventoryTab.UpdateForm();
            SetUnsavedChanges(false);
            RefreshCharacterApplyPendingState();
            InventoryTab.RefreshInventoryApplyUi();
        }

        private void SaveEditor_OnClosed(object sender, EventArgs e)
        {
            Savegame = null;
            EditingPlayers = null;
        }

        private void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Tag)
            {
                case "AllPlayer":
                    Savegame.DumpSavegame();
                    break;
                case "AllInv":
                    Savegame.DumpAllInventory();
                    break;
                case "AllMod":
                    Savegame.DumpAllModifiers();
                    break;
                case "AllPerBoost":
                    Savegame.DumpAllPermanentBoosts();
                    break;
                case "AllSkills":
                    Savegame.DumpAllSkills();
                    break;
                case "AllTalents":
                    Savegame.DumpAllTalents();
                    break;
            }

            MessageBox.Show("A dump file has been created. Thank you!");
        }

        
        private void SavePlayer_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatsTab.SaveEdits();
                AbilitiesTab.SaveEdits();
                TraitsTab.SaveEdits();
                TalentTab.SaveEdits();

                MessageBox.Show(this, "Changes have been applied to the selected character.", "Successful");
                MarkUnsavedSessionChanges();
                RefreshCharacterApplyPendingState();
            }
            catch (Exception ex)
            {
                var er = new ErrorReporting($"Failed to save changes.\n\n{ex}", null);
                er.ShowDialog();
            }
        }

        private void DismissButton_OnClick(object sender, RoutedEventArgs e)
        {
            SubmitPanel.Visibility = Visibility.Collapsed;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RoutedEventArgs e)
        {
        }

        private void SaveEditor_OnClosing(object sender, CancelEventArgs e)
        {
            var unapplied = StatsTab.HasPendingEdits() || AbilitiesTab.HasPendingEdits() || TraitsTab.HasPendingEdits() ||
                            TalentTab.HasPendingEdits() || InventoryTab.HasPendingItemDetailEdits();
            if (unapplied)
            {
                var r1 = MessageBox.Show(this,
                    "You have edited fields that are not applied yet (use Apply on the character tabs or Apply changes on inventory). Close anyway and lose those edits?",
                    "Unapplied edits",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (r1 == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (!_hasUnsavedChanges) return;

            var result = MessageBox.Show(this,
                "You have changes that are not saved to the save file yet (use Save). Close anyway and discard those changes?",
                "Unsaved changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void BugReportButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(
                "https://docs.google.com/forms/d/e/1FAIpQLSeUeKYdV8InQslbvCvA1rmffJ5t1ieond4W6hpUHkHTH7I7dg/viewform");
        }
    }
}
