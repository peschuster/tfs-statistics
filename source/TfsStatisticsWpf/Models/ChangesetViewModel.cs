using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsStatisticsWpf.Models
{
    public class ChangesetViewModel : INotifyPropertyChanged
    {
        private IChangeInfo info;

        private bool inProgress;

        public event PropertyChangedEventHandler PropertyChanged;

        internal ChangesetViewModel(TeamProject project, Changeset changeset, IChangeInfo info)
        {
            this.Project = project;
            if (changeset == null)
                throw new ArgumentNullException("changeset");
            
            this.Info = info;
            this.Changeset = changeset;
        }

        public Changeset Changeset { get; private set; }

        public TeamProject Project { get; private set; }

        public IChangeInfo Info
        {
            get { return this.info; }
            
            set 
            {
                if (this.info != value)
                {
                    this.info = value;

                    this.TriggerChanged("Info", "AddedLines", "RemovedLines", "FileCount");
                }
            }
        }

        public string Id 
        {
            get { return this.Changeset.ChangesetId.ToString(); }
        }

        public DateTime Datum
        {
            get { return this.Changeset.CreationDate; }
        }

        public string Author
        {
            get { return this.Changeset.Committer; }
        }

        public string Comment
        {
            get { return this.Changeset.Comment; }
        }

        public string FileCount
        {
            get 
            { 
                return string.Format(
                    "{0}/{1}", 
                    this.Info == null || this.Info.Name == null ? "?" : this.Info.FileCount.ToString(), 
                    this.Changeset.Changes.Count(x => x.Item.ItemType == ItemType.File));
            }
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
