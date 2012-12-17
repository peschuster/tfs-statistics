using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using MahApps.Metro.Controls;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für ProjectSelector.xaml
    /// </summary>
    public partial class ProjectSelector : MetroWindow
    {
        public ProjectSelector(IEnumerable<Project> projects)
        {
            this.Projects = new ObservableCollection<Project>(projects.OrderBy(p => p.Name));

            this.InitializeComponent();
        }

        public ObservableCollection<Project> Projects { get; private set; }

        public Project SelectedProject
        {
            get { return this.lvProjects.SelectedValue as Project; }
            set { this.lvProjects.SelectedIndex = lvProjects.Items.IndexOf(value); }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void lvProjects_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
