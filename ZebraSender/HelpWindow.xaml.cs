using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Xps.Packaging;

namespace ZebraSender
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            try
            {
                XpsDocument doc = new XpsDocument("zLabels User Guide.xps", FileAccess.Read);
                documentViewer.Document = doc.GetFixedDocumentSequence();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Couldn't open help document.\n" + ex.Message, "Even Help Can't Help");
            }           
        }
    }
}
