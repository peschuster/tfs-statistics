using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using MongoDB.Driver;
using TfsStatisticsWpf.Models;
using TfsStatisticsWpf.Properties;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public SettingsViewModel SettingsModel { get; private set; }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            this.SettingsModel = new SettingsViewModel(Settings.Default);

            if (string.IsNullOrEmpty(Settings.Default.TfsConnection))
                Settings.Default.Upgrade();

            if (string.IsNullOrEmpty(Settings.Default.TfsConnection) || !this.CanConnectToMongo(Settings.Default.MongoConnection))
            {
                bool? result = new SettingsWindow(this.SettingsModel).ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                Settings.Default.Save();
            }

            this.MainWindow = new MainWindow(this.SettingsModel);

            this.MainWindow.ShowDialog();
        }

        private bool CanConnectToMongo(string connectionString)
        {
            try
            {
                var client = new MongoClient(connectionString);
                
                var db = client
                    .GetServer()
                    .GetDatabase(MongoDbCache.DatabaseName, new MongoCredentials(MongoDbCache.UserName, MongoDbCache.Password));
                
                db.CollectionExists("test");
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Application_DispatcherUnhandledException_1(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

        }
    }
}
