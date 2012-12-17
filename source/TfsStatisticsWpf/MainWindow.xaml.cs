using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly TfsConnector tfsConnector;

        private readonly TfsAnalytics analystics;

        private readonly IUserImageService imageService;

        private CancellationTokenSource cancelSource = new CancellationTokenSource();

        private ChangesetViewModel currentChangeset;

        private ConcurrentDictionary<string, UserViewModel> users = new ConcurrentDictionary<string, UserViewModel>();

        private readonly SettingsViewModel settings;

        private event EventHandler RedrawCharts;

        public MainWindow(SettingsViewModel settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");
            
            this.settings = settings;

            this.tfsConnector = new TfsConnector(this.settings.TfsConnection);

            var cache = new MongoDbCache<ChangeInfo>(
                this.settings.MongoConnection,
                MongoDbCache.DatabaseName,
                "changeStats");

            this.analystics = new TfsAnalytics(this.tfsConnector, cache);

            this.imageService = new DirectoryUserImageService(this.settings.DomainController, this.settings.DirectoryImageProperty);

            this.DataContext = this;

            this.Projects = new ObservableCollection<TeamProject>(this.tfsConnector.GeTeamProjects());

            this.Changesets = new ObservableCollection<ChangesetViewModel>();
            this.Changes = new ObservableCollection<ChangeViewModel>();
            
            this.InitializeComponent();

            Observable.FromEvent<EventHandler, EventArgs>(
                handler => (sender, e) => handler(e),
                h => this.RedrawCharts += h,
                h => this.RedrawCharts -= h)
                .Throttle(TimeSpan.FromSeconds(3)).ObserveOn(Scheduler.CurrentThread).Subscribe(l =>
                {
                    this.Dispatcher.Invoke(this.DrawCharts);
                });
        }

        public void OnChartRadio(object sender, RoutedEventArgs e)
        {
            this.DrawCharts();
        }

        private void OnRedraw(object sender, RoutedEventArgs e)
        {
            this.DrawCharts();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            this.cancelSource.Cancel();

            this.btnCancel.IsEnabled = false;
        }

        private async void ProjectChanged()
        {
            var checkins = await this.tfsConnector.GetCheckinsAsync(this.CurrentProject.ServerItem);
            
            this.Changesets.Clear();
            foreach (var item in checkins.Select(c => new ChangesetViewModel(this.CurrentProject, c, null)))
            {
                item.PropertyChanged += this.OnInfoPropertyChanged;
                this.Changesets.Add(item);
            }

            await this.ReadInfosAsync(false);
        }

        private void OnInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.RedrawCharts == null)
                return;

            if (e.PropertyName == "AddedLines" || e.PropertyName == "RemovedLines")
            {
                this.RedrawCharts(sender, new EventArgs());
            }
        }
  
        private void DrawCharts()
        {
            if (!this.Changesets.Any())
                return;

            DateTime startDate = this.Changesets.Select(x => x.Datum).Min();

            startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0);

            if (rbnLines.IsChecked.Value)
            {
                Dictionary<string, IList<IChangeInfo>> data = new Dictionary<string, IList<IChangeInfo>>();
                DateTime currentDate = startDate;
                DateTime previouseDate = DateTime.MinValue;
                while (currentDate < DateTime.Today)
                {
                    data.Add(
                        currentDate.Year + "/" + currentDate.Month,
                        this.Changesets.Where(x => x.Datum < currentDate && x.Datum >= previouseDate && x.Info != null).Select(x => x.Info).ToList());

                    previouseDate = currentDate;
                    currentDate = currentDate.AddMonths(1);
                }

                var addedData = data.ToDictionary(
                    i => i.Key,
                    i => i.Value.Sum(s => s.AddedLines));

                var removedData = data.ToDictionary(
                    i => i.Key,
                    i => i.Value.Sum(s => s.RemovedLines) * -1);

                this.chartVerlauf.Series.Clear();

                var lsAdded = new LineSeries
                    {
                        IsSelectionEnabled = true,
                        DependentValuePath = "Value",
                        IndependentValuePath = "Key",
                        Name = "lsAdded",
                        Title = "Added",
                        ItemsSource = addedData,
                    };

                this.chartVerlauf.Series.Add(lsAdded);

                var lsRemoved = new LineSeries
                {
                    IsSelectionEnabled = true,
                    DependentValuePath = "Value",
                    IndependentValuePath = "Key",
                    Name = "lsRemoved",
                    Title = "Removed",
                    ItemsSource = removedData,
                };

                this.chartVerlauf.Series.Add(lsRemoved);
            }
            else if (rbnContributors.IsChecked.Value)
            {
                this.chartVerlauf.Series.Clear();

                foreach (var group in this.Changesets.GroupBy(x => x.Author))
                {
                    Dictionary<string, IList<IChangeInfo>> data = new Dictionary<string, IList<IChangeInfo>>();
                    DateTime currentDate = startDate;
                    DateTime previouseDate = DateTime.MinValue;
                    while (currentDate < DateTime.Today)
                    {
                        data.Add(
                            currentDate.Year + "/" + currentDate.Month,
                            group.Where(x => x.Datum < currentDate && x.Datum >= previouseDate && x.Info != null).Select(x => x.Info).ToList());

                        previouseDate = currentDate;
                        currentDate = currentDate.AddMonths(1);
                    }

                    var lsCon = new LineSeries
                    {
                        IsSelectionEnabled = true,
                        DependentValuePath = "Value",
                        IndependentValuePath = "Key",
                        Name = "lsCon" + Guid.NewGuid().ToString().Replace("-", string.Empty),
                        Title = group.Key,
                        ItemsSource = data.ToDictionary(i => i.Key, i => i.Value.Sum(s => s.AddedLines + s.RemovedLines)),
                    };

                    this.chartVerlauf.Series.Add(lsCon);
                }
            }

            var lsCommitter = this.chart1.Series.OfType<PieSeries>().First(x => x.Name == "lsCommitter");

            lsCommitter.ItemsSource = this.Changesets
                .GroupBy(x => x.Author)
                .ToDictionary(x => x.Key, x => x.Sum(y => (y.AddedLines ?? 0) + (y.RemovedLines ?? 0)))
                .OrderBy(x => x.Value);
        }

        public ObservableCollection<TeamProject> Projects { get; private set; }

        public ObservableCollection<ChangesetViewModel> Changesets { get; private set; }

        public ObservableCollection<ChangeViewModel> Changes { get; private set; }

        private Task ReadInfosAsync(bool force)
        {
            return Task.Run(() => this.ReadInfos(force));
        }

        private async void ReadInfos(bool force)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.cmbProject.IsEnabled = false;
                this.btnReAnalyze.IsEnabled = false;
                this.btnExport.IsEnabled = false;
                this.btnCancel.IsEnabled = true;

                this.prgBarStatus.Maximum = this.Changesets.Count;
                this.prgBarStatus.Value = 0;
            });

            try
            {
                foreach (var item in this.Changesets)
                {
                    if (this.cancelSource.IsCancellationRequested)
                        return;

                    item.InProgress = true;

                    item.Info = await this.RefreshStats(item.Changeset, force);

                    item.InProgress = false;

                    this.Dispatcher.Invoke(() =>
                    {
                        this.prgBarStatus.Value += 1;
                    });
                }
            }
            finally
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.cmbProject.IsEnabled = true;
                    this.btnReAnalyze.IsEnabled = true;
                    this.btnExport.IsEnabled = true;
                    this.btnCancel.IsEnabled = false;
                });
            }
        }

        public TeamProject CurrentProject { get; private set; }

        private async void OnRefreshStatisticsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null || !(button.Tag is string))
                return;

            string id = (string)button.Tag;

            var model = this.Changesets.Single(c => c.Id == id);

            model.InProgress = true;
            model.Info = await this.RefreshStats(model.Changeset, true);
            model.InProgress = false;
        }

        private Task<IChangeInfo> RefreshStats(Changeset changeset, bool force)
        {
            return Task<IChangeInfo>.Run(() => 
            {
                return this.analystics.GetDiff(changeset, force);
            });
        }

        private Task<IChangeInfo> RefreshStats(Changeset changeset, Change change, bool force)
        {
            return Task<IChangeInfo>.Run(() =>
            {
                return this.analystics.GetDiff(changeset, change, force);
            });
        }

        private void cmbProject_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            this.ProjectChanged();
            this.btnReAnalyze.IsEnabled = true;
            this.btnExport.IsEnabled = true;
        }

        private async void OnReAnalyzeClicked(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.cmbProject.IsEnabled = false;
                this.btnReAnalyze.IsEnabled = false;
                this.btnExport.IsEnabled = false;
                this.btnCancel.IsEnabled = true;

                this.prgBarStatus.Maximum = this.Changesets.Count;
                this.prgBarStatus.Value = 0;
            });

            try
            {
                foreach (var item in this.Changesets)
                {
                    if (this.cancelSource.IsCancellationRequested)
                        return;

                    item.InProgress = true;
                    
                    item.Info = await this.RefreshStats(item.Changeset, true);
                    
                    item.InProgress = false;

                    this.Dispatcher.Invoke(() =>
                    {
                        this.prgBarStatus.Value += 1;
                    });
                }
            }
            finally
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.cmbProject.IsEnabled = true;
                    this.btnReAnalyze.IsEnabled = true;
                    this.btnExport.IsEnabled = true;
                    this.btnCancel.IsEnabled = false;
                });
            }
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            var window = new ExportWindow(this.CurrentProject.Name, this.settings, this.Changesets);

            window.ShowDialog();
        }

        private async void ChangesetsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var model = this.gridChangesets.SelectedItem as ChangesetViewModel;

            if (model == null || model == this.currentChangeset)
                return;

            this.currentChangeset = model;

            var userModel = await this.GetUserModelAsync(model.Author);
            this.lblUser.Content = model.Author;
            this.txtComment.Text = model.Comment;
            this.imgUser.Source = userModel.Image;
            this.txtChangeDate.Text = model.Datum.ToString();

            this.lblChangesStatus.Content = "Processing...";
            this.lblChangesStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD7, 0xE4, 0xF2));

            this.Changes.Clear();

            foreach (var item in model.Changeset.Changes)
            {
                this.Changes.Add(new ChangeViewModel(item, null));
            }

            foreach (var item in this.Changes.ToArray())
            {
                item.Info = await this.RefreshStats(model.Changeset, item.Change, false);
            }

            if (this.currentChangeset == model)
            {
                this.lblChangesStatus.Content = "Done.";
                this.lblChangesStatus.Background = new SolidColorBrush(Color.FromArgb(255, 160, 245, 76));
            }
        }

        private Task<UserViewModel> GetUserModelAsync(string name)
        {
            return Task<UserViewModel>.Run(() =>
            {
                return this.Dispatcher.Invoke(() => 
                {
                    return this.GetUserModel(name);
                });
            });
        }

        private UserViewModel GetUserModel(string name)
        {
            return this.users.GetOrAdd(name, a => new UserViewModel(a, this.imageService));
        }

        private void OpenChange(object sender, MouseButtonEventArgs e)
        {
            var model = this.gridChanges.SelectedItem as ChangeViewModel;

            if (model == null)
                return;

            this.analystics.VisualDiff(model.Change);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(this.settings).ShowDialog();

            Properties.Settings.Default.Save();
        }

        private async void OnLatestClicked(object sender, RoutedEventArgs e)
        {
            var checkins = this.tfsConnector.GetLatestCheckins("$/", 20);

            var models = new List<ChangesetViewModel>();
            foreach (Changeset checkin in checkins)
            {
                Change change = checkin.Changes.FirstOrDefault();

                if (change == null)
                    continue;

                string projectName = change.Item.ServerItem.Split('/').Skip(1).First();

                TeamProject project = this.Projects.FirstOrDefault(p => p.Name == projectName);

                models.Add(new ChangesetViewModel(project, checkin, this.analystics.GetDiff(checkin, false)));
            }

            var window = new LatestWindow(models, this.GetUserModel);
            window.Show();
        }
    }
}
