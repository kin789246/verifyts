using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace verifyts
{
    class SigcheckHelper
    {
        //private const string logFileName = "temp.log";

        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;
        public SigcheckHelper()
        {
            outputlog = new StringBuilder();
            startInfo = new ProcessStartInfo
            {
                FileName = "exe\\sigcheck.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };
        }

        public Task<string> DumpCatContent(string fileName)
        {
            outputlog.Clear();

            return Task.Run(() =>
            {
                startInfo.Arguments = "/accepteula -d " + "\"" + fileName + "\"";
                Process getOsVer = new Process();
                getOsVer.StartInfo = startInfo;

                ExecuteProc(getOsVer);

                return outputlog.ToString();
            });
        }

        private void ExecuteProc(Process process)
        {
            process.Start();
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            return;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                string rgx = @"OS: *";
                if (Regex.IsMatch(e.Data, rgx))
                {
                    outputlog.Append(e.Data).AppendLine();
                    return;
                }
            }
        }
    }
}

// OS: _v100_X64_RS3,_v100_X64_RS4,_v100_X64_RS5,_v100_X64_19H1
// OS: _v100_X64_RS5,_v100_X64_19H1
// OS: _v100_X64_RS5
