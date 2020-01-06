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
        private const string logFileName = "catresult.log";
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
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            //folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;

            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();

            //</ Dialog >
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //----< Selected Folder >----
                //load cat list
                WholeGrid.IsEnabled = false;

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
                ResultTB.Inlines.Add(AddString("This is WHQL driver."));
                ResultTB.Inlines.Add(AddString(" Logs is saved to " + logFileName));
                WriteLog("This is WHQL driver.");
            }
            else
            {
                ResultTB.Inlines.Add(AddString("This is "));
                ResultTB.Inlines.Add(AddString("NOT WHQL ", Colors.Red, Colors.White));
                ResultTB.Inlines.Add(AddString("driver."));
                ResultTB.Inlines.Add(AddString(" Logs is saved to " + logFileName));
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
                OutputTB.Inlines.Add(AddString(item.ToString()));
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
        private static void WriteLog(string text)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(logFileName, true))
            {
                file.Write(text);
            }
        }
    }
}
