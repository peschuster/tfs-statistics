using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using TfsStatisticsWpf.Export;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : MetroWindow
    {
        public ExportWindow(string projectName, SettingsViewModel settings, IEnumerable<ChangesetViewModel> changesets)
        {
            this.DataContext = this;

            this.Settings = settings;
            this.Changesets = new ObservableCollection<SelectableChangesetViewModel>(changesets.Select(c => new SelectableChangesetViewModel(c)));

            this.GraphiteProjectKey = string.IsNullOrEmpty(projectName) 
                ? string.Empty 
                : projectName.ToLower().Replace(" ", "_").Replace(".", "_");

            this.InitializeComponent();
        }
        
        public string GraphiteProjectKey { get; set; }

        public SettingsViewModel Settings { get; private set; }

        public ObservableCollection<SelectableChangesetViewModel> Changesets { get; private set; }

        private void OnSelectAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.Changesets)
            {
                item.Selected = true;
            }
        }

        private void OnSelectNoneClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.Changesets)
            {
                item.Selected = false;
            }
        }

        private async void OnExportGraphite(object sender, RoutedEventArgs e)
        {
            try
            {
                this.btnExportGrphite.IsEnabled = false;

                using (var exporter = new GraphiteExport(this.Settings.GraphiteHost, this.Settings.GraphiteMetricFormat, this.GraphiteProjectKey, this.Settings.GraphiteBaseKey))
                {
                    ICollection<ChangesetViewModel> items = this.Changesets.Where(x => x.Selected).Select(x => x.Changeset).ToList();

                    this.prgBarGraphite.Maximum = items.Count;
                    exporter.ProgressChanged += (s, e2) => this.Dispatcher.Invoke(() => this.prgBarGraphite.Value = this.prgBarGraphite.Maximum * (double)e2.ProgressPercentage / 100);

                    await exporter.ExportAsync(items);
                }
            }
            finally
            {
                this.btnExportGrphite.IsEnabled = true;
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
