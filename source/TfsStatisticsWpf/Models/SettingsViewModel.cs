using System.ComponentModel;

namespace TfsStatisticsWpf.Models
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal SettingsViewModel(Properties.Settings settings)
        {
            this.Settings = settings;
        }

        private Properties.Settings Settings { get; set; }

        public string TfsConnection
        {
            get 
            { 
                return this.Settings.TfsConnection; 
            }

            set
            {
                if (value != this.Settings.TfsConnection)
                {
                    this.Settings.TfsConnection = value;
                    this.TriggerPropertyChanged("TfsConnection");
                }
            }
        }

        public string DomainController
        {
            get
            {
                return this.Settings.DomainController;
            }

            set
            {
                if (value != this.Settings.DomainController)
                {
                    this.Settings.DomainController = value;
                    this.TriggerPropertyChanged("DomainController");
                }
            }
        }

        public string DirectoryImageProperty
        {
            get
            {
                return this.Settings.DirectoryImageProperty;
            }

            set
            {
                if (value != this.Settings.DirectoryImageProperty)
                {
                    this.Settings.DirectoryImageProperty = value;
                    this.TriggerPropertyChanged("DirectoryImageProperty");
                }
            }
        }

        public string MongoConnection
        {
            get
            {
                return this.Settings.MongoConnection;
            }

            set
            {
                if (value != this.Settings.MongoConnection)
                {
                    this.Settings.MongoConnection = value;
                    this.TriggerPropertyChanged("MongoConnection");
                }
            }
        }

        public string GraphiteHost
        {
            get
            {
                return this.Settings.GraphiteHost;
            }

            set
            {
                if (value != this.Settings.GraphiteHost)
                {
                    this.Settings.GraphiteHost = value;
                    this.TriggerPropertyChanged("GraphiteHost");
                }
            }
        }

        public string GraphiteMetricFormat
        {
            get
            {
                return this.Settings.GraphiteMetricFormat;
            }

            set
            {
                if (value != this.Settings.GraphiteMetricFormat)
                {
                    this.Settings.GraphiteMetricFormat = value;
                    this.TriggerPropertyChanged("GraphiteMetricFormat");
                }
            }
        }

        public string GraphiteBaseKey
        {
            get
            {
                return this.Settings.GraphiteBaseKey;
            }

            set
            {
                if (value != this.Settings.GraphiteBaseKey)
                {
                    this.Settings.GraphiteBaseKey = value;
                    this.TriggerPropertyChanged("GraphiteBaseKey");
                }
            }
        }

        private void TriggerPropertyChanged(params string[] names)
        {
            if (this.PropertyChanged == null)
                return;

            foreach (string name in names)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
