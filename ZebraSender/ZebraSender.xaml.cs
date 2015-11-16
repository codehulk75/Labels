using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;

namespace ZebraSender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private List<string> suFiles = new List<string>();  //setup sheet files--user chosen
        private List<string> lines = new List<string>();    //parsed data extracted from all files chosen
        List<string> ttNames = new List<string>();          //base names of all setup sheet files chosen (path is stripped)  
        private List<string> assyNames = new List<string>();
        private string user = null;
        private string initials = null;
        private string helpStr = null;
        private string ip = null;
        private int port = 9100;
        Popup helpPopUp = new Popup();
        TextBlock popupText = new TextBlock();
        
    public MainWindow()
        {
            InitializeComponent();
            try
            {
                InitializeComponent();
                InitHelpPopUp();        
                dateTextBox.Text = GetDate();
                dateTextBox.UpdateLayout();              
                initialsTextBox.Text = GetInitials();
                initialsTextBox.UpdateLayout();
                assyTextBox.IsEnabled = false;
                addLabelButton.IsEnabled = false;
                ip = GetPrinterIP();
                if (ip == null)
                    MessageBox.Show("Failed to get printer address.\nContact IT to configure a valid printer.", "Printer Config Error");
                ssbutton.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void InitHelpPopUp()
        {
            string hfile = "help.txt";     
            using (StreamReader rdStream = new StreamReader(hfile))
            {
                helpStr = rdStream.ReadToEnd();
            }
            popupText.Text = helpStr;
            popupText.Background = System.Windows.Media.Brushes.Snow;
            popupText.Foreground = System.Windows.Media.Brushes.Black;
            helpPopUp.Child = popupText;
            helpPopUp.PlacementTarget = myGrid;
            helpPopUp.PlacementRectangle = new Rect(0, 0, 700, 500);
            helpPopUp.Placement = PlacementMode.Center;
            helpPopUp.PopupAnimation = PopupAnimation.Fade;
        }

        private string GetInitials()
        {
            user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            user = user.ToUpper();
            int i = 0;
            try
            {
                i = user.LastIndexOf('\\') + 1;
                initials = user.Substring(i+5, 1) + user.Substring(i, 1);
            }
            catch (ArgumentOutOfRangeException argEx)
            {
                MessageBox.Show("You have a short user name!\nYou might have to edit your initials.", argEx.Message);
                initials = user.Substring(user.Length - 1, 1) + user.Substring(i, 1);
            }
            return initials;
        }

        private string GetDate()
        {
            string date;
            date = DateTime.Today.ToString();
            int i = date.LastIndexOf('/');
            date = date.Substring(0, i);
            return date;
        }

        private string GetPrinterIP()
        {
            string ipAddr = null;
            string fil = "printer_addr.txt";
            using ( StreamReader rdStream = new StreamReader(fil) )
            {
                char[] seps = new char[] { ' ' };
                string pattern = @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$";
                string line;
                while ( (line = rdStream.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    if (line[0] == '#')
                    {
                        continue;
                    }
                    string[] lines = line.Split(seps);
                    foreach (var item in lines)
                    {
                        if (Regex.IsMatch(item, pattern))
                        {
                            ipAddr = item;
                            //ensure config file lists valid bei \16 ip for the printer
                            if ( (ipAddr.Substring(0, 7) != "167.67.") && (ipAddr != "127.0.0.1") )
                            {
                                MessageBox.Show("Printer IP("+ipAddr+") is not on the BEI domain.\nPlease contact IT to configure the printer_addr.txt file and restart the application.", "Invalid Printer Address Configured");
                                ipAddr = "-1";
                            }
                            return ipAddr;
                        }
                    }
                }
            }
            return ipAddr;
        }
        private void ssbutton_Click(object sender, RoutedEventArgs e)
        {
            string basefil = null;
            try
            {
                assyTextBox.IsEnabled = true;
                string[] fils = GetSetupSheets();
                if (fils.Length == 0)
                {
                    return;
                }
                suFiles.Clear();    
                lines.Clear();
                assyNames.Clear();
                pnTextBox.Clear();
                locTextBox.Clear();
                commentTextBox.Clear();
                woTextBox.Clear();
                ttNames.Clear();
                suFiles = new List<string>(fils);
                foreach (string file in suFiles)
                {
                    int i = file.LastIndexOf('\\');
                    i++;
                    basefil = file.Substring(i);
                    ssBrowseLabel.Content = "";
                    ssBrowseLabel.UpdateLayout();
                    i = basefil.IndexOf('_');
                    string assy = basefil.Substring(0, i);
                    assyNames.Add(assy);
                    ttNames.Add(basefil);
                    ProcessStartInfo psInfo = new ProcessStartInfo();
                    psInfo.FileName = "zExtract.exe";
                    psInfo.Arguments = "\"" + file + "\"";
                    psInfo.ErrorDialog = true;
                    psInfo.CreateNoWindow = true;
                    psInfo.UseShellExecute = false;
                    psInfo.WorkingDirectory = "";
                    Process p = Process.Start(psInfo);
                    p.WaitForExit();                   
                }

                ReadParts();

                if (assyNames.Count > 1)
                {
                    assyTextBox.Text = "Change Me to Family Name";
                    FileInfo fInfo = new FileInfo(suFiles[0]);
                    ssLabel.Content = fInfo.Directory.Name + " FAMILY";
                }
                else
                {
                    assyTextBox.Text = assyNames[0];
                    ssLabel.Content = basefil;
                }
                assyTextBox.UpdateLayout();
                ssLabel.UpdateLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private string[] GetSetupSheets()
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Setup Sheets (*.rtf)|*.rtf";
            fd.InitialDirectory = @"\\nh-as02\nh-meth-smt-setup$";
            fd.Title = "Choose One or More Setup Sheets";
            fd.Multiselect = true;
            fd.ValidateNames = true;
            try
            {
                fd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem reaching the setup sheet folder...possibly network issues.\nContact IT.", ex.Message);
            }
            return fd.FileNames;
        }
        private void assyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            assyTextBox.SelectAll();
            
        }

        private void initialsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            initialsTextBox.SelectAll();
        }

        private void dateTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            dateTextBox.SelectAll();
        }
        private void woTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            woTextBox.SelectAll();
        }

        private void pnTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            pnTextBox.SelectAll();
        }

        private void locTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            locTextBox.SelectAll();
        }

        private void qtyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            qtyTextBox.SelectAll();
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void qtyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void locTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void pnTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void woTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void dateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void initialsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void assyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (assyTextBox.Text.Length > 0
                && initialsTextBox.Text.Length > 0
                && dateTextBox.Text.Length > 0
                && woTextBox.Text.Length > 0
                && pnTextBox.Text.Length > 0
                && locTextBox.Text.Length > 0
                && qtyTextBox.Text.Length > 0)
            {
                addLabelButton.IsEnabled = true;
            }
            else
            {
                addLabelButton.IsEnabled = false;
            }
        }

        private void qtyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (addLabelButton != null)
            {
                if (assyTextBox.Text.Length > 0
                    && initialsTextBox.Text.Length > 0
                    && dateTextBox.Text.Length > 0
                    && woTextBox.Text.Length > 0
                    && pnTextBox.Text.Length > 0
                    && locTextBox.Text.Length > 0
                    && qtyTextBox.Text.Length > 0)
                {
                    addLabelButton.IsEnabled = true;
                }
                else
                {
                    addLabelButton.IsEnabled = false;
                }
            }
        }

        private void pnTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string inchar = "\r";
            if (string.IsNullOrEmpty(pnTextBox.Text))
                return;
            if (e.Text == inchar)
            {
                List<string> results = new List<string>();
                pnTextBox.Text = pnTextBox.Text.ToUpper();
                string pnum = pnTextBox.Text;
                pnum = StripPrefix(pnum);
                string pattern = @"^" + pnum + @" =>";
                foreach (string line in lines)
                {
                    Match match = Regex.Match(line, pattern);
                    if (match.Success)
                    {
                        results.Add(line);
                    }
                }
                if (results.Count == 0)
                {
                    MessageBox.Show("Part Number not found by auto search.\nEnter machine location by hand."
                        + "\nUse the format Machine/Slot."
                        + "\nDon't forget the forward slash or you will get an 'Index out of bounds' error when you try to print!!", "Oops!!");
                    locTextBox.Clear();
                    locTextBox.Focus();
                    return;
                }
                List<string> location;
                if (firstPassRadioButton.IsChecked == true)
                {
                    location = GetLocation(results, "SMT 1");
                    if (location.Count == 0)
                    {
                        LocationNotFound("1st");
                        return;
                    }
                    if (location.Count > 1)
                    {
                        MessageBox.Show("Warning!\nThis part is in different locations on different assemblies.\nUsing 1st location found on 1st pass.",
                            "CF Mode 1st Pass Family Matching Error");
                    }
                }
                else if (secondPassRadioButton.IsChecked == true)
                {
                    location = GetLocation(results, "SMT 2");
                    if (location.Count == 0)
                    {
                        LocationNotFound("2nd");
                        return;
                    }
                    if (location.Count > 1)
                    {
                        MessageBox.Show("Warning!\nThis part is in different locations on different assemblies.\nUsing 1st location found on 2nd pass.",
                            "CF Mode 2nd Pass Family Matching Error");
                    }
                }
                else //batch button must be checked
                {    
                    location = GetLocation(results, "batch");
                    if (location.Count == 0)
                    {
                        LocationNotFound("either");
                        return;
                    }
                    if (location.Count > 1)
                    {
                        MessageBox.Show("Warning!\nBatch Mode is checked but part is in different locations.\nUsing 1st location found.",
                            "Batch Mode Matching Error");
                    }
                }

                locTextBox.Text = location[0];
                qtyTextBox.Focus();
            }
        }

        private void LocationNotFound(string pass)
        {
            MessageBox.Show("Location not found on " + pass + " Pass:(\nEnter machine location by hand", "Oops!!");
            locTextBox.Clear();
            locTextBox.Focus();
        }
        private void secondPassRadioButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                secondPassRadioButton.IsChecked = true;
            }
        }

        private void firstPassRadioButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                firstPassRadioButton.IsChecked = true;
            }
        }
        private void batchModeRadioButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                batchModeRadioButton.IsChecked = true;
            }
        }

        private void ReadParts()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            int j = dir.LastIndexOf('\\');
            dir = dir.Substring(0, j);
            dir += "\\Local\\Temp";
            int n = 0;
            foreach (string assembly  in assyNames)
            {
                string[] files = Directory.GetFiles(dir, (assyNames[n] + "*.txt"));
                string procdFil = files[0];
                if (!string.IsNullOrEmpty(procdFil))
                {
                    using (StreamReader rdStream = new StreamReader(procdFil))
                    {
                        string line = "asf";
                        for (int i = 0; i < 4; i++)
                        {
                            rdStream.ReadLine();
                        }
                        while (!string.IsNullOrEmpty(line))
                        {
                            line = rdStream.ReadLine();
                            lines.Add(line);
                        }
                        lines.RemoveAt(lines.Count - 1);
                    }
                    File.Delete(procdFil);
                }
                n++;
            }
        }

        private string StripPrefix(string part)
        {
            List<string> prefixs = new List<string>();
            string preFile = "Prefixes.ini";
            string stripped = part;
            using (StreamReader rd = new StreamReader(preFile))
            {
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    if (line[0] == '#')
                    {
                        continue;
                    }
                    prefixs.Add(line);
                }
            }
            foreach (string s in prefixs)
            {
                if (stripped.StartsWith(s))
                {
                    int len = s.Length;
                    stripped = stripped.Substring(len, stripped.Length - s.Length);
                    break;
                }
            }
            return stripped;
        }

        private List<string> GetLocation(List<string> foundLocations, string pass)
        {
            List<string> locs = new List<string>();
            char[] sep = new char[] { ';' };
            if (pass == "batch")
            {
                foreach (string part in foundLocations)
                {
                    string[] temp = part.Split(sep);
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (temp[i] == "SMT 1" || temp[i] == "SMT 2")
                        {
                            string machine = FormatMachine(temp[i + 1]);
                            locs.Add(machine + "/" + temp[i + 2]);
                        }
                    }
                }
            }
            else
            {
                foreach (string part in foundLocations)
                {
                    string[] temp = part.Split(sep);
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (temp[i] == pass)
                        {
                            string machine = FormatMachine(temp[i + 1]);
                            locs.Add(machine + "/" + temp[i + 2]);
                        }
                    }
                }
            }
            if ( locs.Count > 1)
            {
                for( int i = locs.Count-1; i > 0; i-- )
                {
                    if (locs[i] == locs[i - 1])
                        locs.RemoveAt(i);
                }

            }
            return locs;
        }

        private string FormatMachine(string machine)
        {
            string m = machine;
            switch (machine)
            {
                case "GC601":
                    m = "GC60-1";
                    break;
                case "GC602":
                    m = "GC60-2";
                    break;
                case "GI141":
                    m = "GI14-1";
                    break;
                case "GI142":
                    m = "GI14-2";
                    break;
                default:
                    break;
            }
            return m;
        }

        private void woTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            woTextBox.Text = woTextBox.Text.ToUpper();
        }
        private string[] FormatInput()
        {
            char[] seps = { '/' };
            string[] spl = locTextBox.Text.Split(seps);

            //prepare label fields, (except "reels n of n" field  = determined inside addLabelButton_Click())
            string machine = "MACHINE: " + spl[0];
            string station = "STATION: " + spl[1];
            string indate = "INIT/DATE: " + initialsTextBox.Text + " " + dateTextBox.Text;
            string assy = "ASSY: " + assyTextBox.Text;
            string wo = "W/O#: " + woTextBox.Text;
            string qap = "QAP HN1504  3/27/03";
            string pn = "P/N: " + pnTextBox.Text;
            string comments = "Comments: " + commentTextBox.Text;
            string[] data = new string[] { station, indate, assy, wo, qap, pn, comments, machine };

            return data;
        }

        private string CreateZplStr(string[] fields)
        {
            
            //zpl label format string
            //edge of left label is value 110 in dots for 3" x 1" label, right edge value is 719 (203dpi x 3" + 110)
            //top is value 0, bottom is value 203 (1" x 203dpi)		
            string zpl = "^XA^PW832^LH0,0^LT0^FO125,25^A0N37,35^FD" + fields[0] + "^FS"
                    + "^FO710,30,1^A0N25,25^FD" + fields[1] + "^FS"
                    + "^FO125,70^A0N25,25^FD" + fields[2] + "^FS"
                    + "^FO710,70,1^A0N25,25^FD" + fields[3] + "^FS"
                    + "^FO710,110,1^A0N25,25^FD" + "Reel " + fields[4] + " -OF " + fields[5] + "^FS"
                    + "^FO125,150^A0N25,25^FD" + fields[6] + "^FS"
                    + "^FO125,110^A0N25,25^FD" + fields[7] + "^FS"
                    + "^FO130,185^A0N25,25^FD" + fields[8] + "^FS"
                    + "^FO710,150,1^A0N25,25^FD" + fields[9] + "^FS"
                    + "^FO115,15^GB600,197,3,B,2^FS"
                    + "^FO115,55^GB600,0,3^FS"
                    + "^FO115,95^GB600,0,3^FS"
                    + "^FO115,135^GB600,0,3^FS"
                    + "^FO115,175^GB600,0,3^FS^XZ";
            return zpl;
        }

        private void SendLabel(string label)
        {
            try
            {
                //connect to zpl printer using ip+port and send zpl label format command with tcp socket
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                System.IO.StreamWriter writer = new System.IO.StreamWriter(client.GetStream());
                writer.Write(label);
                writer.Flush();
                writer.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Socket Error");
            }
        }

        private int QtyParsed()
        {
            int n;
            bool parsed = Int32.TryParse(qtyTextBox.Text, out n);
            if (!parsed || n < 1 || n > 50)
            {
                MessageBox.Show("Couldn't recognize quantity.\nMake sure it's a whole number between 1 and 50.", "Label Quantity Error");
                return -1;
            }
            else
            {
                return n;
            }
        }

        private void ClearForm()
        {
            pnTextBox.Clear();
            locTextBox.Clear();
            qtyTextBox.Text = "1";
            pnTextBox.Focus();
        }

        private void addLabelButton_Click(object sender, RoutedEventArgs e)
        {
            int n = QtyParsed();
            if (n == -1)
                return;
            try
            {
                string[] input = FormatInput();

                for (int i = n; i > 0; i--)
                {
                    string[] fields = new string[] { input[0], input[1], input[2], input[3], i.ToString(), n.ToString(), input[4], input[5], input[6], input[7] };
                    string zplstr = CreateZplStr(fields);
                    SendLabel(zplstr);
                }
            }
            catch (IndexOutOfRangeException indexEx)
            {
                MessageBox.Show("Not enough info to print.\nDid you add the location by hand and forget the forward slash ('/') between machine and slot?", indexEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            ClearForm();
        }

        private void assyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            assyTextBox.Text = assyTextBox.Text.ToUpper();
        }

        private void assyTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (assyTextBox.Text == "Change Me to Family Name")
                assyTextBox.Clear();
        }

        private void ssLabel_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Label lab = sender as Label;
            string tt = "";
            if (ttNames.Count > 0)
            {
                foreach (string sheetname in ttNames)
                {
                    tt += sheetname + "\n";
                }
                tt = tt.Substring(0, tt.Length - 1);
            }
            else
            {
                tt = "No files chosen yet.";
            }
            lab.ToolTip = tt;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ( e.Key == Key.F1)
            {
               helpPopUp.IsOpen = true;                           
            }
            else if ( e.Key == Key.Escape)
            {
                helpPopUp.IsOpen = false;
            }
        }
    }
}

