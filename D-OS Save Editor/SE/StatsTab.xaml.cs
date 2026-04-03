using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace D_OS_Save_Editor
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class StatsTab
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

        public StatsTab()
        {
            InitializeComponent();
            DefaultTextBoxBorderBrush = ExpTextBox.BorderBrush;
        }


        public bool HasPendingEdits()
        {
            if (Player == null) return false;
            if (ExpTextBox.Text != Player.Experience) return true;
            if (ReputationTextBox.Text != Player.Reputation) return true;
            if (HpCurrentTextBox.Text != Player.Vitality) return true;
            if (HpMaxTextBox.Text != Player.MaxVitalityPatchCheck) return true;
            if (AttributePointsTextBox.Text != Player.AttributePoints) return true;
            if (AbilityPointsTextBox.Text != Player.AbilityPoints) return true;
            if (TalentPointsTextBox.Text != Player.TalentPoints) return true;
            if (StrengthTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Strength].ToString()) return true;
            if (DexterityTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Dexerity].ToString()) return true;
            if (IntelligenceTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Intelligence].ToString()) return true;
            if (ConstitutionTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Consitution].ToString()) return true;
            if (SpeedTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Speed].ToString()) return true;
            if (PerceptionTextBox.Text != Player.Attributes[(int)DataTable.Attributes.Perception].ToString()) return true;
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
            ExpTextBox.Text = Player.Experience;
            ReputationTextBox.Text = Player.Reputation;
            HpCurrentTextBox.Text = Player.Vitality;
            HpMaxTextBox.Text = Player.MaxVitalityPatchCheck;
            AttributePointsTextBox.Text = Player.AttributePoints;
            AbilityPointsTextBox.Text = Player.AbilityPoints;
            TalentPointsTextBox.Text = Player.TalentPoints;
            StrengthTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Strength].ToString();
            DexterityTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Dexerity].ToString();
            IntelligenceTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Intelligence].ToString();
            ConstitutionTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Consitution].ToString();
            SpeedTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Speed].ToString();
            PerceptionTextBox.Text = Player.Attributes[(int)DataTable.Attributes.Perception].ToString();
            }
            finally
            {
                _suppressFieldEvents = false;
            }
        }

        public void SaveEdits()
        {
            Player.Experience = ExpTextBox.Text;
            Player.Reputation = ReputationTextBox.Text;
            Player.Vitality = HpCurrentTextBox.Text;
            Player.MaxVitalityPatchCheck = HpMaxTextBox.Text;
            Player.AttributePoints = AttributePointsTextBox.Text;
            Player.AbilityPoints = AbilityPointsTextBox.Text;
            Player.TalentPoints = TalentPointsTextBox.Text;
            Player.Attributes[(int) DataTable.Attributes.Strength] = int.Parse(StrengthTextBox.Text);
            Player.Attributes[(int)DataTable.Attributes.Dexerity] = int.Parse(DexterityTextBox.Text);
            Player.Attributes[(int)DataTable.Attributes.Intelligence] = int.Parse(IntelligenceTextBox.Text);
            Player.Attributes[(int)DataTable.Attributes.Consitution] = int.Parse(ConstitutionTextBox.Text);
            Player.Attributes[(int)DataTable.Attributes.Speed] = int.Parse(SpeedTextBox.Text);
            Player.Attributes[(int)DataTable.Attributes.Perception] = int.Parse(PerceptionTextBox.Text);
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
