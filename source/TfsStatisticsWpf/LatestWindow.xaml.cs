using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsStatisticsWpf.Controls;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für Latest.xaml
    /// </summary>
    public partial class LatestWindow : MetroWindow
    {
        private readonly IEnumerable<ChangesetViewModel> items;

        private readonly Func<string, UserViewModel> userFactory;

        private bool odd;

        public LatestWindow(IEnumerable<ChangesetViewModel> items, Func<string, UserViewModel> userFactory)
        {
            this.userFactory = userFactory;
            this.items = items;

            this.InitializeComponent();

            this.scroller.InitialItemCount = 20;
            this.scroller.Items = items;
            this.scroller.ControlFactory = (object o) =>
            {
                var item = o as ChangesetViewModel;

                if (item == null)
                    return null;

                this.odd = !this.odd;
                var control = new ChangesetControl(item, this.userFactory);

                if (this.odd)
                {
                    control.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }

                return control;
            };
        }
    }
}
