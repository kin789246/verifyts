using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace verifyts
{
    public enum CatCategory
    {
        WHQL,
        AttestedSigned,
        LegacyTestSigned,
        Expired
    }

    class SigntoolHelper
    {
        private const string attesteddOid = "1.3.6.1.4.1.311.10.3.5.1";
        private const string lifetimeOid = "1.3.6.1.4.1.311.10.3.13";

        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;
        public SigntoolHelper()
        {
            outputlog = new StringBuilder();
            startInfo = new ProcessStartInfo
            {
                FileName = "signtool.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };
        }

        public Task<string> VerifyAttested(string fileName)
        {
            outputlog.Clear();

            return Task.Run(() =>
            {
                startInfo.Arguments = "verify /u " + attesteddOid + " \"" + fileName + "\"";
                Process verifyAttestedOid = new Process();
                verifyAttestedOid.StartInfo = startInfo;

                ExecuteProc(verifyAttestedOid);

                return outputlog.ToString();
            });
        }

        public Task<string> VerifyLifetime(string fileName)
        {
            outputlog.Clear();

            return Task.Run(() =>
            {
                startInfo.Arguments = "verify /u " + lifetimeOid + " \"" + fileName + "\"";
                Process verifyLifetime = new Process();
                verifyLifetime.StartInfo = startInfo;

                ExecuteProc(verifyLifetime);

                return outputlog.ToString();
            });
        }

        private void ExecuteProc(Process process)
        {
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock(outputlog)
                {
                    outputlog.Append(e.Data).AppendLine();
                }
                //Debug.WriteLine("err:" + e.Data);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (outputlog)
                {
                    outputlog.Append(e.Data).AppendLine();
                }
                //Debug.WriteLine("std:" + e.Data);
            }
        }
    }
}


//D:\work\TS>signtool.exe verify /u 1.3.6.1.4.1.311.10.3.5.1 hdxrtext.cat
//File: hdxrtext.cat
//Index  Algorithm Timestamp
//========================================
//0      sha256     RFC3161

//Successfully verified: hdxrtext.cat

//D:\work\TS>signtool.exe verify /u 1.3.6.1.4.1.311.10.3.5 igdlh.cat
//File: igdlh.cat
//Index  Algorithm  Timestamp
//========================================
//SignTool Error: WinVerifyTrust returned error: 0x800B0101
//        A required certificate is not within its validity period when verifying against the current system clock or the timestamp in the signed file.

//Number of errors: 1

//D:\work\TS>signtool.exe verify /u 1.3.6.1.4.1.311.10.3.5.1 igdlhwhql.cat
//File: igdlhwhql.cat
//Index  Algorithm  Timestamp
//========================================
//SignTool Error: The signer does not possess the specified EKUs.

//Number of errors: 1
//D:\work\TS>signtool.exe verify /u 1.3.6.1.4.1.311.10.3.5.1 "D:\work\driver\Falfel1.x_Win10_19H1\08 Audio\6.0.8721.1_Legacy_TS\WIN32\HDARt.cat"
//File: D:\work\driver\Falfel1.x_Win10_19H1\08 Audio\6.0.8721.1_Legacy_TS\WIN32\HDARt.cat
//Index  Algorithm Timestamp
//========================================
//SignTool Error: A certificate chain processed, but terminated in a root
//        certificate which is not trusted by the trust provider.

//Number of errors: 1
