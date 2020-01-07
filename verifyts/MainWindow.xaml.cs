using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace verifyts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string logFileName;
        private List<CatData> catDatas;
        public MainWindow()
        {
            InitializeComponent();
            ResultTB.Text = "";
        }

        private async void SelDrvFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = false;
            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            //folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            folderDialog.SelectedPath = await LoadLastPath();

            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();

            //</ Dialog >
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //----< Selected Folder >----
                //load cat list
                WholeGrid.IsEnabled = false;

                SaveLastPath(folderDialog.SelectedPath);

                ResultTB.Inlines.Clear();
                OutputTB.Inlines.Clear();
                OutputTB.Inlines.Add(AddString("Analyzing cat files in " + folderDialog.SelectedPath + "\n"));
                OutputSV.ScrollToEnd();

                var catFiles = Directory.GetFiles(folderDialog.SelectedPath, "*.cat", SearchOption.AllDirectories);
                
                if (catFiles.Length == 0)
                {
                    OutputTB.Inlines.Add(AddString("No cat file is found.\n"));
                    OutputSV.ScrollToEnd();
                }
                else
                {
                    logFileName = System.IO.Path.GetFileName(folderDialog.SelectedPath) + DateTime.Now.ToString("_yyyyMMdd_hhmmss") + ".log";
                    DeleteLogFile();

                    catDatas = new List<CatData>();
                    await ParseCatCategory(catFiles);
                    GetOutputText();
                    GetResult();
                    //Process.Start(logFileName);
                }

                WholeGrid.IsEnabled = true;
            }
        }

        private void GetResult()
        {
            if (catDatas == null)
            {
                return;
            }
            bool isWhql = true;
            foreach (var item in catDatas)
            {
                if (item.CatCategory != "WHQL Signed")
                {
                    isWhql = false;
                    break;
                }
            }
            if (isWhql)
            {
                ResultTB.Inlines.Add(AddString("This is "));
                ResultTB.Inlines.Add(AddString("WHQL ", Colors.Blue, Colors.White));
                ResultTB.Inlines.Add(AddString("driver."));
                ResultTB.Inlines.Add(AddString(" Logs are saved to " + logFileName));
                WriteLog("This is WHQL driver.");
            }
            else
            {
                ResultTB.Inlines.Add(AddString("This is "));
                ResultTB.Inlines.Add(AddString("NOT WHQL ", Colors.Red, Colors.White));
                ResultTB.Inlines.Add(AddString("driver."));
                ResultTB.Inlines.Add(AddString(" Logs are saved to " + logFileName));
                WriteLog("This is NOT WHQL driver.");
            }
        }

        private void GetOutputText()
        {
            if (catDatas == null)
            {
                return;
            }

            foreach (var item in catDatas)
            {
                //OutputTB.Inlines.Add(AddString(item.ToString()));
                OutputTB.Inlines.Add(AddString("=====================" + Environment.NewLine + item.CatName + Environment.NewLine));
                if (item.CatCategory == "WHQL Signed")
                {
                    OutputTB.Inlines.Add(AddString(item.CatCategory + Environment.NewLine));
                }
                else
                {
                    OutputTB.Inlines.Add(AddString(item.CatCategory + Environment.NewLine, Colors.Red, Colors.White));
                }
                
                OutputTB.Inlines.Add(AddString(item.OsSupport + Environment.NewLine + "=====================" + Environment.NewLine + Environment.NewLine));
                WriteLog(item.ToString());
            }

            OutputSV.ScrollToEnd();
        }

        private void DeleteLogFile()
        {
            if (System.IO.File.Exists(logFileName))
            {
                // Use a try block to catch IOExceptions, to
                // handle the case of the file already being
                // opened by another process.
                try
                {
                    System.IO.File.Delete(logFileName);
                }
                catch (System.IO.IOException e)
                {
                    OutputTB.Inlines.Add(AddString(e.Message));
                    OutputSV.ScrollToEnd();
                }
            }
        }

        private async Task ParseCatCategory(string[] catFiles)
        {
            const string expiredStr = "A required certificate is not within its validity";
            const string notTrust = "certificate which is not trusted by the trust provider";
            const string notPossesEKU = "The signer does not possess the specified EKUs";
            const string successVerified = "Successfully verified";

            string log = "";
            SigntoolHelper signtool = new SigntoolHelper();
            SigcheckHelper sigcheck = new SigcheckHelper();
            
            foreach (var name in catFiles)
            {
                CatData tempCatData = new CatData();
                tempCatData.CatName = name;

                log = await signtool.VerifyAttested(name);
                //OutputTB.Inlines.Add(AddString(name + " | "));
                if (log.Contains(successVerified))
                {
                    tempCatData.CatCategory = "Attested Signed";
                    //category = CatCategory.AttestedSigned;
                }
                else if (log.Contains(expiredStr))
                {
                    tempCatData.CatCategory = "Sign Expired";
                    //category = CatCategory.Expired;
                }
                else if (log.Contains(notTrust))
                {
                    tempCatData.CatCategory = "Not Trusted Sign";
                }
                else if (log.Contains(notPossesEKU))
                {
                    log = await signtool.VerifyLifetime(name);
                    if (log.Contains(successVerified))
                    {
                        tempCatData.CatCategory = "Legacy Test Signed";
                        //category = CatCategory.LegacyTestSigned;
                    }
                    else
                    {
                        tempCatData.CatCategory = "WHQL Signed";
                        //category = CatCategory.WHQL;
                    }
                }
                else
                {
                    tempCatData.CatCategory = "WHQL Signed";
                }

                tempCatData.OsSupport = await sigcheck.DumpCatContent(name);
                tempCatData.OsSupport = tempCatData.OsSupport.Trim();
                catDatas.Add(tempCatData);
            }
        }

        private async void DebugBtn_Click(object sender, RoutedEventArgs e)
        {
            //SigntoolHelper signtool = new SigntoolHelper();
            //OutputTB.Inlines.Add(AddString(await signtool.VerifyAttested("igdlh.cat")));
            //OutputTB.Inlines.Add(AddString(await signtool.VerifyAttested("igdlhwhql.cat")));
            //OutputTB.Inlines.Add(AddString(await signtool.VerifyAttested("extinf.cat")));
            //OutputTB.Inlines.Add(AddString(await signtool.VerifyAttested("hdxrtext.cat")));
            //OutputSV.ScrollToEnd();
            WholeGrid.IsEnabled = false;

            SigcheckHelper sigcheck = new SigcheckHelper();
            OutputTB.Inlines.Add(AddString(await sigcheck.DumpCatContent("igdlhwhql.cat")));
            OutputSV.ScrollToEnd();

            WholeGrid.IsEnabled = true;
        }

        private Run AddString(string text, Color foreColor, Color bgColor)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(foreColor);
            run.Background = new SolidColorBrush(bgColor);

            //WriteLog(text);

            return run;
        }

        private Run AddString(string text)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(Colors.Black);
            run.Background = new SolidColorBrush(Colors.White);

            //WriteLog(text);

            return run;
        }
        private void WriteLog(string text)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(logFileName, true))
            {
                file.Write(text);
            }
        }

        private void SaveLastPath(string text)
        {
            string fileName = "verifyts.ini";
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(fileName, false))
            {
                file.Write(text);
            }
        }

        private async Task<string> LoadLastPath()
        {
            string lastPath = "";
            string fileName = "verifyts.ini";
            using (StreamReader reader = new StreamReader(fileName))
            {
                while (reader.Peek() > -1)
                {
                    string s = await reader.ReadLineAsync();
                    lastPath = s.Trim();
                    break;
                }
            }

            return lastPath;
        }
    }
}
