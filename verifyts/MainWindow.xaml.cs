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
        public MainWindow()
        {
            InitializeComponent();
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
                    await ParseCatCategory(catFiles);
                }
                
                WholeGrid.IsEnabled = true;
            }
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
            //CatCategory category = CatCategory.Expired;
            
            foreach (var name in catFiles)
            {
                log = await signtool.VerifyAttested(name);
                OutputTB.Inlines.Add(AddString(name + " | "));
                if (log.Contains(successVerified))
                {
                    //category = CatCategory.AttestedSigned;
                    OutputTB.Inlines.Add(AddString("Attested Signed | "));
                }
                else if (log.Contains(expiredStr))
                {
                    //category = CatCategory.Expired;
                    OutputTB.Inlines.Add(AddString("Sign Expired | "));
                }
                else if (log.Contains(notTrust))
                {
                    OutputTB.Inlines.Add(AddString("Not Trust Sign | "));
                }
                else if (log.Contains(notPossesEKU))
                {
                    log = await signtool.VerifyLifetime(name);
                    if (log.Contains(successVerified))
                    {
                        //category = CatCategory.LegacyTestSigned;
                        OutputTB.Inlines.Add(AddString("Legacy Test Signed | "));
                    }
                    else
                    {
                        //category = CatCategory.WHQL;
                        OutputTB.Inlines.Add(AddString("WHQL Signed | "));
                    }
                }
                else
                {
                    OutputTB.Inlines.Add(AddString("WHQL Signed | "));
                }

                string osVersion = await sigcheck.DumpCatContent(name);
                OutputTB.Inlines.Add(AddString( osVersion.Trim() + "\n"));
            }

            Process.Start(logFileName);
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

            WriteLog(text);

            return run;
        }

        private Run AddString(string text)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(Colors.Black);
            run.Background = new SolidColorBrush(Colors.White);

            WriteLog(text);

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
