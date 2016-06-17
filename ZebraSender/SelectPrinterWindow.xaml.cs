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
using System.Text.RegularExpressions;

namespace ZebraSender
{
    /// <summary>
    /// This is basically a custom choose printer window, and it as access to a few of the main window's varibles
    /// It will update the current printer label on the main window, update the currentPrinter variable, and
    /// also save the new choice to the printer_addr.txt file, so the app will remember the new printer next
    /// time it launches ("printer_addr.txt" is a configurable text file in the app directory)
    /// </summary>
    public partial class SelectPrinterWindow : Window
    {       
        private string originalPrinter = ((MainWindow)Application.Current.MainWindow).Printer;
        private string printerfile = ((MainWindow)Application.Current.MainWindow).PrintFile;
        private List<string> fileLines = new List<string>();
        public SelectPrinterWindow()
        {
            InitializeComponent();
            currentPrinterTextBox.Text = originalPrinter;         
            Dictionary<string, string> printers = new Dictionary<string, string>(((MainWindow)Application.Current.MainWindow).AvailablePrinters);
            foreach (var pr in printers)
            {
                listBox.Items.Add(pr.Key);
            }
        }

        private void setPrinterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                currentPrinterTextBox.Text = listBox.SelectedItem.ToString();
                ((MainWindow)Application.Current.MainWindow).Printer = currentPrinterTextBox.Text;
                ((MainWindow)Application.Current.MainWindow).printerLabel.Content = currentPrinterTextBox.Text;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error setting the printer.\n\n" + ex.Message);
            }
           
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (StreamReader rd = new StreamReader(printerfile))
                {
                    string line = null;
                    while ((line = rd.ReadLine()) != null)
                    {
                        fileLines.Add(line);
                    }
                }
                for (int i = 0; i < fileLines.Count; ++i)
                {
                    if (Regex.IsMatch(fileLines[i], originalPrinter) && !Regex.IsMatch(fileLines[i], @"^#"))
                    {
                        fileLines[i] = fileLines[i].Insert(0, "#");
                    }
                    if (Regex.IsMatch(fileLines[i], currentPrinterTextBox.Text) && Regex.IsMatch(fileLines[i], @"^#"))
                    {
                        fileLines[i] = fileLines[i].TrimStart('#');
                    }
                }
                using (StreamWriter writer = new StreamWriter(printerfile))
                {
                    foreach (string prtrline in fileLines)
                    {
                        writer.WriteLine(prtrline);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error saving new printer.\n" + ex.Message, "Can't Save New Printer");
            }
            
            if (originalPrinter != currentPrinterTextBox.Text)
            {
                UpdateProgressWindow pr = new UpdateProgressWindow("Updating Printer Preference...");
                pr.Show();
            }         
            Close();
        }
    }
}
