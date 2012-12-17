using System.ComponentModel;

namespace TfsStatisticsWpf.Models
{
    public class SelectableChangesetViewModel : INotifyPropertyChanged
    {
        private bool selected;

        public event PropertyChangedEventHandler PropertyChanged;

        public SelectableChangesetViewModel(ChangesetViewModel baseModel)
        {
            this.Changeset = baseModel;
            this.Changeset.PropertyChanged += (s, e) => this.TriggerChanged("Changeset");
        }

        public ChangesetViewModel Changeset { get; private set; }

        public bool Selected
        {
            get { return this.selected; }

            set
            {
                if (this.selected = value)
                {
                    this.selected = value;
                    this.TriggerChanged("Selected");
                }
            }
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
