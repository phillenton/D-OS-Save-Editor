namespace D_OS_Save_Editor
{
    /// <summary>
    /// Interaction logic for TraitsTab.xaml
    /// </summary>
    public partial class TraitsTab
    {
        private Player _player;
        public Player Player
        {
            get => _player;

            set
            {
                _player = value;
                UpdateForm();
            }
        }

        private TraitCouple[] _traitCouples;
        public bool SuppressTraitNotifications { get; private set; }

        public TraitsTab()
        {
            InitializeComponent();
        }


        public bool HasPendingEdits()
        {
            if (Player == null || _traitCouples == null) return false;
            for (var i = 0; i < DataTable.TraitNames.Length / 2; i++)
            {
                if (_traitCouples[i] == null) continue;
                var tc = _traitCouples[i].DataContext as TraitCoupleViewModel;
                if (tc == null) continue;
                if (tc.LeftTrait.Value != Player.Traits[2 * i].ToString()) return true;
                if (tc.RightTrait.Value != Player.Traits[2 * i + 1].ToString()) return true;
            }
            return false;
        }

        public void UpdateForm()
        {
            SuppressTraitNotifications = true;
            try
            {
            _traitCouples = new TraitCouple[DataTable.TraitNames.Length / 2];
            StackPanel.Children.Clear();

            for (var i = 0; i < DataTable.TraitNames.Length / 2; i++)
            {
                _traitCouples[i] = new TraitCouple(
                    new Trait(DataTable.TraitNames[2 * i], Player.Traits[2 * i].ToString(),
                        DataTable.TraitEffects[2 * i]),
                    new Trait(DataTable.TraitNames[2 * i + 1], Player.Traits[2 * i + 1].ToString(),
                        DataTable.TraitEffects[2 * i + 1]));

                StackPanel.Children.Add(_traitCouples[i]);
            }
            }
            finally
            {
                SuppressTraitNotifications = false;
            }
        }

        public void SaveEdits()
        {
            for (var i = 0; i < DataTable.TraitNames.Length / 2; i++)
            {
                if (_traitCouples[i] == null) continue;
                var tc = _traitCouples[i].DataContext as TraitCoupleViewModel;
                Player.Traits[2 * i] = int.Parse(tc.LeftTrait.Value);
                Player.Traits[2 * i + 1] = int.Parse(tc.RightTrait.Value);
            }
        }
    }
}
