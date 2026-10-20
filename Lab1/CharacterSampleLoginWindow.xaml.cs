using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PasswordProtectionApp
{
    public partial class CharacterSampleLoginWindow : Window
    {
        private readonly List<int> _positions;
        private readonly List<PasswordBox> _boxes = new List<PasswordBox>();

        public List<char> EnteredCharacters { get; private set; } = new List<char>();

        public CharacterSampleLoginWindow(List<int> positions)
        {
            InitializeComponent();
            _positions = positions;
            BuildFields();
        }

        private void BuildFields()
        {
            foreach (int position in _positions)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = new TextBlock
                {
                    Text = $"Character #{position + 1}:",
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);

                var box = new PasswordBox
                {
                    PasswordChar = '*',
                    MaxLength = 1,
                    Height = 26,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(box, 1);

                row.Children.Add(label);
                row.Children.Add(box);
                FieldsPanel.Children.Add(row);
                _boxes.Add(box);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_boxes.Any(b => string.IsNullOrEmpty(b.Password)))
            {
                StatusText.Text = "Please fill in every requested character.";
                return;
            }

            EnteredCharacters = _boxes.Select(b => b.Password[0]).ToList();
            DialogResult = true;
            Close();
        }
    }
}