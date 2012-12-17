using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TfsStatisticsWpf.Controls
{
    public class InfiniteScrollViewer : ScrollViewer
    {
        private int fetched = 0;

        public InfiniteScrollViewer()
        {
            this.ScrollChanged += this.OnScrollChanged;
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var control in this.NextItems().Take(this.InitialItemCount))
            {
                this.ItemList.Items.Add(control);
            }

            this.fetched += this.InitialItemCount;
        }

        private IEnumerable<Control> NextItems()
        {
            foreach (var item in this.Items.Skip(this.fetched))
            {
                var control = this.ControlFactory(item);

                if (control != null)
                    yield return control;
            }
        }

        public IEnumerable<object> Items { get; set; }

        public Func<object, Control> ControlFactory { get; set; }

        public int InitialItemCount { get; set; }

        public ItemsControl ItemList
        {
            get { return this.Content as ItemsControl; }
        }

        private Task<Control> BuildUp(object o)
        {
            return Task<Control>.Run(() => this.ControlFactory(o));
        }

        private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (e.VerticalOffset + e.ViewportHeight + 50 >= e.ExtentHeight)
            {

                this.Dispatcher.BeginInvoke(((ThreadStart)delegate() { this.Add10Items(e); }));
            }
        }

        private void Add10Items(ScrollChangedEventArgs e)
        {
            double off = e.VerticalOffset;

            foreach (var control in this.NextItems().Take(10))
            {
                this.ItemList.Items.Add(control);
            }

            this.fetched += 10;
        }
    }
}
