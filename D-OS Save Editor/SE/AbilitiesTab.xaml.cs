using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace D_OS_Save_Editor
{
    /// <summary>
    /// Interaction logic for AbilitiesTab.xaml
    /// </summary>
    public partial class AbilitiesTab
    {
        private Player _player;
        private bool _suppressFieldEvents;
        private Brush DefaultTextBoxBorderBrush { get; }

        public Player Player
        {
            get => _player;

            set
            {
                _player = value;
                UpdateForm();
            }
        }

        public AbilitiesTab()
        {
            InitializeComponent();
            DefaultTextBoxBorderBrush = ManAtArmsTextBox.BorderBrush;
        }

        public bool HasPendingEdits()
        {
            if (Player == null) return false;
            if (ManAtArmsTextBox.Text != Player.Abilities[(int)DataTable.Abilities.ManAtArms].ToString()) return true;
            if (ExpertMarksmanTextBox.Text != Player.Abilities[(int)DataTable.Abilities.ExpertMarksman].ToString()) return true;
            if (ScoundrelTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Scoundrel].ToString()) return true;
            if (SingleHandedTextBox.Text != Player.Abilities[(int)DataTable.Abilities.SingleHanded].ToString()) return true;
            if (TwoHandedTextBox.Text != Player.Abilities[(int)DataTable.Abilities.TwoHanded].ToString()) return true;
            if (BowTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Bow].ToString()) return true;
            if (CrossbowTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Crossbow].ToString()) return true;
            if (ShieldSpecialistTextBox.Text != Player.Abilities[(int)DataTable.Abilities.ShieldSpecialist].ToString()) return true;
            if (ArmourSpecialistTextBox.Text != Player.Abilities[(int)DataTable.Abilities.ArmourSpecialist].ToString()) return true;
            if (WitchcraftTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Witchcraft].ToString()) return true;
            if (TelekinesisTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Telekinesis].ToString()) return true;
            if (WillpowerTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Willpower].ToString()) return true;
            if (PyrokineticTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Pyrokinetic].ToString()) return true;
            if (HydrosophistTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Hydrosophist].ToString()) return true;
            if (AerotheurgeTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Aerotheurge].ToString()) return true;
            if (GeomancerTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Geomancer].ToString()) return true;
            if (BlacksmithingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Blacksmithing].ToString()) return true;
            if (SneakingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Sneaking].ToString()) return true;
            if (PickpocketingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Pickpocketing].ToString()) return true;
            if (LockpickingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Lockpicking].ToString()) return true;
            if (LoremasterTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Loremaster].ToString()) return true;
            if (CraftingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Crafting].ToString()) return true;
            if (BarteringTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Bartering].ToString()) return true;
            if (CharismaTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Charisma].ToString()) return true;
            if (LeadershipTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Leadership].ToString()) return true;
            if (LuckyCharmTextBox.Text != Player.Abilities[(int)DataTable.Abilities.LuckyCharm].ToString()) return true;
            if (BodyBuildingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.BodyBuilding].ToString()) return true;
            if (DualWieldingTextBox.Text != Player.Abilities[(int)DataTable.Abilities.DualWielding].ToString()) return true;
            if (WandTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Wand].ToString()) return true;
            if (TenebriumTextBox.Text != Player.Abilities[(int)DataTable.Abilities.Tenebrium].ToString()) return true;
            return false;
        }

        private void CharacterField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressFieldEvents) return;
            if (Window.GetWindow(this) is SaveEditor editor)
                editor.RefreshCharacterApplyPendingState();
        }
        public void UpdateForm()
        {
            _suppressFieldEvents = true;
            try
            {
            ManAtArmsTextBox.Text = Player.Abilities[(int)DataTable.Abilities.ManAtArms].ToString();
            ExpertMarksmanTextBox.Text = Player.Abilities[(int)DataTable.Abilities.ExpertMarksman].ToString();
            ScoundrelTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Scoundrel].ToString();
            SingleHandedTextBox.Text = Player.Abilities[(int)DataTable.Abilities.SingleHanded].ToString();
            TwoHandedTextBox.Text = Player.Abilities[(int)DataTable.Abilities.TwoHanded].ToString();
            BowTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Bow].ToString();
            CrossbowTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Crossbow].ToString();
            ShieldSpecialistTextBox.Text = Player.Abilities[(int)DataTable.Abilities.ShieldSpecialist].ToString();
            ArmourSpecialistTextBox.Text = Player.Abilities[(int)DataTable.Abilities.ArmourSpecialist].ToString();
            WitchcraftTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Witchcraft].ToString();
            TelekinesisTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Telekinesis].ToString();
            WillpowerTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Willpower].ToString();
            PyrokineticTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Pyrokinetic].ToString();
            HydrosophistTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Hydrosophist].ToString();
            AerotheurgeTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Aerotheurge].ToString();
            GeomancerTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Geomancer].ToString();
            BlacksmithingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Blacksmithing].ToString();
            SneakingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Sneaking].ToString();
            PickpocketingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Pickpocketing].ToString();
            LockpickingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Lockpicking].ToString();
            LoremasterTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Loremaster].ToString();
            CraftingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Crafting].ToString();
            BarteringTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Bartering].ToString();
            CharismaTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Charisma].ToString();
            LeadershipTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Leadership].ToString();
            LuckyCharmTextBox.Text = Player.Abilities[(int)DataTable.Abilities.LuckyCharm].ToString();
            BodyBuildingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.BodyBuilding].ToString();
            DualWieldingTextBox.Text = Player.Abilities[(int)DataTable.Abilities.DualWielding].ToString();
            WandTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Wand].ToString();
            TenebriumTextBox.Text = Player.Abilities[(int)DataTable.Abilities.Tenebrium].ToString();
            }
            finally
            {
                _suppressFieldEvents = false;
            }
        }

        public void SaveEdits()
        {
            Player.Abilities[(int)DataTable.Abilities.ManAtArms] = int.Parse(ManAtArmsTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.ExpertMarksman] = int.Parse(ExpertMarksmanTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Scoundrel] = int.Parse(ScoundrelTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.SingleHanded] = int.Parse(SingleHandedTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.TwoHanded] = int.Parse(TwoHandedTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Bow] = int.Parse(BowTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Crossbow] = int.Parse(CrossbowTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.ShieldSpecialist] = int.Parse(ShieldSpecialistTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.ArmourSpecialist] = int.Parse(ArmourSpecialistTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Witchcraft] = int.Parse(WitchcraftTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Telekinesis] = int.Parse(TelekinesisTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Willpower] = int.Parse(WillpowerTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Pyrokinetic] = int.Parse(PyrokineticTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Hydrosophist] = int.Parse(HydrosophistTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Aerotheurge] = int.Parse(AerotheurgeTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Geomancer] = int.Parse(GeomancerTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Blacksmithing] = int.Parse(BlacksmithingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Sneaking] = int.Parse(SneakingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Pickpocketing] = int.Parse(PickpocketingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Lockpicking] = int.Parse(LockpickingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Loremaster] = int.Parse(LoremasterTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Crafting] = int.Parse(CraftingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Bartering] = int.Parse(BarteringTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Charisma] = int.Parse(CharismaTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Leadership] = int.Parse(LeadershipTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.LuckyCharm] = int.Parse(LuckyCharmTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.BodyBuilding] = int.Parse(BodyBuildingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.DualWielding] = int.Parse(DualWieldingTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Wand] = int.Parse(WandTextBox.Text);
            Player.Abilities[(int)DataTable.Abilities.Tenebrium] = int.Parse(TenebriumTextBox.Text);
        }

        private void TextBoxEventSetter_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox s)) return;

            var text = s.Text;
            var valid = int.TryParse(text, out int _);
            s.BorderBrush = !valid ? Brushes.Red : DefaultTextBoxBorderBrush;
        }

        private void TextBoxEventSetter_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox s)) return;

            var text = s.Text.Insert(s.SelectionStart, e.Text);
            e.Handled = !int.TryParse(text, out int _);
        }
    }
}
