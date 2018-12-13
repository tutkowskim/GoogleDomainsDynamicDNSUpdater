using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GoogleDomainsDynamicDNSUpdater
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="domains">The collection of domains to configure.</param>
        public ConfigWindow(IEnumerable<Domain> domains)
        {
            InitializeComponent();
            DomainsGrid.ItemsSource = domains;
        }

        /// <summary>
        /// Set the password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            Domain domain = passwordBox.DataContext as Domain;
            if (domain != null)
            {
                domain.Password = passwordBox.Password;
            }
        }

        /// <summary>
        /// Set the password on the password box once it's created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            Domain domain = passwordBox.DataContext as Domain;
            if (domain != null)
            {
                passwordBox.Password = domain.Password;
            }
        }

        /// <summary>
        /// Commit the edit when unloading the datagrid.
        ///<remark>
        /// This resolves the issue of not being able to reopen the config window after
        /// closing the window with an unsaved edit.
        /// </remark>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = sender as DataGrid;
            if (grid != null)
            {
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }
    }
}
