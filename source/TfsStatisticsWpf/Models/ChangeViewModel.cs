using System.ComponentModel;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsStatisticsWpf.Models
{
    public class ChangeViewModel : INotifyPropertyChanged
    {
        private IChangeInfo info;

        private bool inProgress;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChangeViewModel(Change change, IChangeInfo info)
        {
            this.Change = change;
            this.info = info;
        }

        public Change Change { get; private set; }

        public IChangeInfo Info
        {
            get { return this.info; }

            set
            {
                if (this.info != value)
                {
                    this.info = value;

                    this.TriggerChanged("Info", "AddedLines", "RemovedLines", "Analyzed");
                }
            }
        }

        public string Name
        {
            get { return this.Change.Item.ServerItem; }
        }

        public bool Analyzed
        {
            get { return this.Info != null; }
        }

        public int? AddedLines
        {
            get { return this.Info == null ? default(int?) : this.Info.AddedLines; }
        }

        public int? RemovedLines
        {
            get { return this.Info == null ? default(int?) : this.Info.RemovedLines; }
        }

        public bool InProgress
        {
            get { return this.inProgress; }

            set
            {
                if (this.inProgress != value)
                {
                    this.inProgress = value;

                    this.TriggerChanged("InProgress", "RowColor");
                }
            }
        }

        public string RowColor
        {
            get { return this.InProgress ? "Red" : "White"; }
        }

        private void TriggerChanged(params string[] name)
        {
            if (this.PropertyChanged == null)
                return;

            foreach (string item in name)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(item));
            }
        }
    }
}
