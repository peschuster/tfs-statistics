using System;
using System.Globalization;
using System.Windows.Controls;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf.Controls
{
    /// <summary>
    /// Interaktionslogik für ChangesetControl.xaml
    /// </summary>
    public partial class ChangesetControl : UserControl
    {
        public ChangesetControl(ChangesetViewModel model, Func<string, UserViewModel> userFactory)
        {
            this.DataContext = this;
            
            this.Model = model;
            this.User = userFactory(model.Author);

            this.InitializeComponent();
        }

        public ChangesetViewModel Model { get; private set; }

        public UserViewModel User { get; private set; }
    }
}
