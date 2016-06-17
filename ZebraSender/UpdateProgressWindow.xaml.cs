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
using System.Threading;
using System.ComponentModel;

namespace ZebraSender
{
    /// <summary>
    /// Interaction logic for UpdateProgressWindow.xaml
    /// </summary>
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow(string setting)
        {
            InitializeComponent();
            Title = setting;

        }

        private void UpdateProgressWindow1_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += worker_DoWork;
            bw.ProgressChanged += worker_ProgressChanged;
            bw.RunWorkerAsync();
        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i <= 100; i+=20)
            {
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(400);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb.Value = e.ProgressPercentage;
            if(pb.Value == 100)
            {
                Title = "Done!";
                Thread.Sleep(300);
                Close();
            }
        }
    }
}
