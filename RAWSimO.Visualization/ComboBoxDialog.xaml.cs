using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RAWSimO.Visualization
{
    /// <summary>
    /// Implements a small dialog that offers a set of options and returns the user's choice.
    /// </summary>
    public partial class ComboBoxDialog : Window
    {
        /// <summary>
        /// Creates a new choice window.
        /// </summary>
        /// <param name="choices">The choices.</param>
        /// <param name="choiceCallback">The method that is send the selected choice's index after the OK button was pressed</param>
        public ComboBoxDialog(string[] choices, Action<int> choiceCallback)
        {
            InitializeComponent();
            foreach (var choice in choices)
                ComboBoxChoices.Items.Add(choice);
            ComboBoxChoices.SelectedIndex = 0;
            _choiceCallback = choiceCallback;
        }

        /// <summary>
        /// The method that is send the selected choice after the OK button was pressed.
        /// </summary>
        private Action<int> _choiceCallback;

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            // Notify about the choice
            _choiceCallback(ComboBoxChoices.SelectedIndex);
            DialogResult = true;
            // Close the dialog
            Close();
        }
    }
}
