using System.Windows;
using MahApps.Metro.Controls;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow(SettingsViewModel model)
        {
            this.DataContext = this;
            this.Model = model;

            this.InitializeComponent();

            this.cboCache.SelectedItem = this.cboCache.Items[0];
        }

        public SettingsViewModel Model { get; private set; }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
