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
using TfsStatisticsWpf.Controls;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf
{
    /// <summary>
    /// Interaktionslogik für Latest.xaml
    /// </summary>
    public partial class LatestWindow : MetroWindow
    {
        public LatestWindow(IEnumerable<ChangesetViewModel> items, Func<string, UserViewModel> userFactory)
        {
            this.InitializeComponent();

            bool odd = true;
            foreach (var item in items)
            {
                odd = !odd;
                var control = new ChangesetControl(item, userFactory);

                if (odd)
                {
                    control.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }

                this.list.Items.Add(control);
            }
        }
    }
}
