using System;
using System.IO;
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
using System.Text.RegularExpressions;



namespace ZebraSender
{
    /// <summary>
    /// Interaction logic for EditPrefixes.xaml
    /// </summary>
    
    public partial class EditPrefixes : Window
    {
        private List<string> usedPrefixes = new List<string>();
        private List<string> ignoredPrefixes = new List<string>();
        private string prefixFile = ((MainWindow)Application.Current.MainWindow).PrefixFile;
        public EditPrefixes()
        {
            InitializeComponent();
            try
            {
                foreach (var item in ((MainWindow)Application.Current.MainWindow).Prefixes)
                {
                    usedPrefixes.Add(item);
                    usedListBox.Items.Add(item);
                }
                foreach (var item in ((MainWindow)Application.Current.MainWindow).IgnoredPrefixes)
                {
                    ignoredPrefixes.Add(item);
                    ignoredListBox.Items.Add(item);
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show("Error during prefix read from memory (EditPrefixes()).\n\n" + ex.Message);
                Close();
            }
            
        }

        private void addPrefixButton_Click(object sender, RoutedEventArgs e)
        {
            if (addPrefixTextBox.Text.Length < 1)
                return;
            string strInput = addPrefixTextBox.Text.ToUpper();
            if (Regex.IsMatch(strInput, @"^[A-Z_-]+$"))
            {
                if(usedListBox.Items.Contains(strInput))
                {
                    MessageBox.Show(strInput + " is already in the list!", "Duplicate Prefix", MessageBoxButton.OK, MessageBoxImage.Hand);
                    return;
                }
                usedListBox.Items.Insert(0, strInput);
                if (!strInput.StartsWith("P-"))
                {
                    System.Windows.Forms.DialogResult res = System.Windows.Forms.MessageBox.Show("Would you like to add the P-" + strInput + " and P-C-" + strInput + " versions as well?", "Add All Variants?", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                    if(res == System.Windows.Forms.DialogResult.Yes)
                    {
                        usedListBox.Items.Insert(0, "P-C-" + strInput);
                        usedListBox.Items.Insert(1,"P-" + strInput);
                    }
                }
            }
            else
            {
                MessageBox.Show("Prefixes must contain only letters, hyphens, or underscores.", "Invalid Input Characters.");
            }
        }

        private void ignoreButton_Click(object sender, RoutedEventArgs e)
        {
            List<object> toRemove = new List<object>();
            foreach (var item in usedListBox.SelectedItems)
            {
                ignoredListBox.Items.Insert(0,item);
                toRemove.Add(item);
            }
            foreach (var item in toRemove)
            {
                usedListBox.Items.Remove(item);
            }
        }

        private void activateButton_Click(object sender, RoutedEventArgs e)
        {
            List<object> toRemove = new List<object>();
            foreach (var item in ignoredListBox.SelectedItems)
            {
                usedListBox.Items.Insert(0, item);
                toRemove.Add(item);
            }
            foreach (var item in toRemove)
            {
                ignoredListBox.Items.Remove(item);
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> toBeDeleted = new List<string>();


            foreach (string item in usedListBox.SelectedItems)
            {
                toBeDeleted.Add(item);
            }
            foreach (string item in ignoredListBox.SelectedItems)
            {
                toBeDeleted.Add(item);
            }
            if (toBeDeleted.Count > 0)
            {
                string dels = string.Join("\n", toBeDeleted);
                System.Windows.Forms.DialogResult res = System.Windows.Forms.MessageBox.Show("Are you sure you want to permanently delete these prefixes?\n"
                    +dels, "Delete Prefixes",System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    foreach (var item in toBeDeleted )
                    {
                        usedListBox.Items.Remove(item);
                        ignoredListBox.Items.Remove(item);
                    }
                }
            }
        }

        private void usedListBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            ignoredListBox.UnselectAll();
        }

        private void ignoredListBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            usedListBox.UnselectAll();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var samesame = ((MainWindow)Application.Current.MainWindow).Prefixes.Count == usedListBox.Items.Count
                        && ((MainWindow)Application.Current.MainWindow).Prefixes.All(usedListBox.Items.Contains);
            try
            {
                usedPrefixes.Clear();
                ignoredPrefixes.Clear();
                foreach (string item in usedListBox.Items)
                {
                    usedPrefixes.Add(item);
                }
                foreach (string item in ignoredListBox.Items)
                {
                    ignoredPrefixes.Add(item);
                }
                List<string> writeToFileStrs = usedPrefixes.Concat(ignoredPrefixes).ToList();
                SortFileStrs(writeToFileStrs);

                using (StreamWriter writer = new StreamWriter(prefixFile))
                {
                    foreach (string item in writeToFileStrs)
                    {
                        writer.WriteLine(item);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error saving new prefixes (okButton_Click() event)).\n\n" + ex.Message, "Can't save new prefixes...");
            }
           
            ((MainWindow)Application.Current.MainWindow).Prefixes = usedPrefixes.Distinct().ToList();
            ((MainWindow)Application.Current.MainWindow).IgnoredPrefixes = ignoredPrefixes.Distinct().ToList();
            if (!samesame)
            {
                UpdateProgressWindow pr = new UpdateProgressWindow("Updating Prefixes...");
                pr.Show();
            }        
            Close();
        }
        public void SortFileStrs(List<string> strList)
        {
            strList.Sort(delegate (string a, string b)
            {
                string pdash = "P-";
                string pdashcdash = "P-C-";
                string tempA = null;
                string tempB = null;
                if (a.StartsWith(pdashcdash))
                {
                    tempA = a.Remove(0, 4);
                }
                else if (a.StartsWith(pdash))
                {
                    tempA = a.Remove(0, 2);
                }
                else
                {
                    tempA = a;
                }

                if (b.StartsWith(pdashcdash))
                {
                    tempB = b.Remove(0, 4);
                }
                else if (b.StartsWith(pdash))
                {
                    tempB = b.Remove(0, 2);
                }
                else
                {
                    tempB = b;
                }
                if (tempA.CompareTo(tempB) == 0)
                {
                    if (a.Length == b.Length)
                        return 0;
                    else if (a.Length > b.Length)
                        return -1;
                    else
                        return 1;
                }
                else
                    return tempA.CompareTo(tempB);
            });
            strList.Remove("P-C-");
            strList.Remove("P-");          
            strList.Add("P-C-");
            strList.Add("P-");
        }
    }
}
