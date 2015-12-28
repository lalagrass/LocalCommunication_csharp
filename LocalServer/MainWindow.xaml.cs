using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MyClass.AsynchronousServer mServer;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Closing += MainWindow_Closing;
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mServer.StopListening();
            MyClass.StaticUtils.SetListView(null);
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            MyClass.StaticUtils.SetListView(listView);
            mServer = new MyClass.AsynchronousServer();
            new Thread(delegate()
            {
                mServer.StartListening();
            }).Start();
        }
    }
}
