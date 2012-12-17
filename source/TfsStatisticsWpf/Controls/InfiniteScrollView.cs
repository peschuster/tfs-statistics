using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TfsStatisticsWpf.Controls
{
    public class InfiniteScrollViewer : ScrollViewer
    {
        public InfiniteScrollViewer()
        {
                this.ScrollChanged += this.OnScrollChanged;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }
    }
}
