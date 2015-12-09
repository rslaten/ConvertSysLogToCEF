﻿using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;

//CEF Specific Code
namespace ConvertSysLogToCEF
{
    public static class CEF
    {
        delegate void AccptTCPClient(ref TcpClient client, TcpListener listener);
        public static volatile bool Running;
        private static String LogFile = AppDomain.CurrentDomain.BaseDirectory + "\\ConvertSysLogToCEFConversion.log";
        private static String OldLogFile = AppDomain.CurrentDomain.BaseDirectory + "\\ConvertSysLogToCEFConversion.lo_";
        private static String ConfigurationFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\ConvertSysLogToCEFConversion.ini";
        private static Hashtable keyPairs = new Hashtable();
        
        //CEF Conversion Functions
        private static string ConvertToCEF(string Version, string DeviceVendor, string DeviceProduct, string DeviceVersion, int SignatureID, int Severity, string SysLog)
        {
            string question = GetQuestion(SysLog);
            string answers = GetAnswers(SysLog);
            string ret = Version + "|" + DeviceVendor + "|" + DeviceProduct + "|" + DeviceVersion + "|" + SignatureID + "|" + question + "|" + Severity + "|" + answers;
            return ret;
        }
        private static string GetQuestion(string SysLog)
        {
            string ret = Between(SysLog, "[", "@");
            return ret;
        }
        private static string GetAnswers(string SysLog)
        {
            string answerSection = Between(SysLog, "[", "]");
            string[] answersSplit = answerSection.Split(new Char[] { ' ' });

            //Filter out question and return just the answers
            int i = 0;
            string ret = null;
            int upper = answersSplit.Length - 1;
            foreach (string answer in answersSplit)
            {
                if (i != 0 && i != upper)
                {
                    ret += answer + " ";
                }
                else if (i == upper)
                {
                    ret += answer;
                }
                i++;
            }
            return ret;
        }
        public static string Between(this string Source, string FindFrom, string FindTo)
        {
            int start = Source.IndexOf(FindFrom);
            int to = Source.IndexOf(FindTo, start + FindFrom.Length);
            if (start < 0 || to < 0) return "";
            string ret = Source.Substring(
                           start + FindFrom.Length,
                           to - start - FindFrom.Length);
            return ret;
        }

        //Networking Functions
        public static void AcceptClient(ref TcpClient client, TcpListener listener)
        {
            if (client == null)
                client = listener.AcceptTcpClient();
        }
        public static void ConvertSysLogMessages()
        {
            //Initialize settings
            ParseConfigurationFile(ConfigurationFilePath);

            //Get send and receive ports
            int receivePort, sendPort;
            Int32.TryParse(GetSetting("ConvertSysLogToCEF", "ReceivePort"), out receivePort);
            Int32.TryParse(GetSetting("ConvertSysLogToCEF", "SendPort"), out sendPort);

            //Get CEF column values that are integers
            int signatureID, severity;
            Int32.TryParse(GetSetting("ConvertSysLogToCEF", "SignatureID"), out signatureID);
            Int32.TryParse(GetSetting("ConvertSysLogToCEF", "Severity"), out severity);
            
            //Get CEF column values that are string based
            string version = GetSetting("ConvertSysLogToCEF", "Version");
            string deviceVendor = GetSetting("ConvertSysLogToCEF", "DeviceVendor");
            string deviceProduct = GetSetting("ConvertSysLogToCEF", "DeviceProduct");
            string deviceVersion = GetSetting("ConvertSysLogToCEF", "DeviceVersion");

            //Start listening for data
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, receivePort));
            try
            {
                WriteErrorLog("Listening for new incoming TCP connections");
                listener.Start();

                TcpClient handler = null;
                while (Running)
                {
                    AccptTCPClient receive = new AccptTCPClient(AcceptClient);

                    Thread receiver = new Thread(() => receive(ref handler, listener));
                    receiver.IsBackground = true;
                    receiver.Start();

                    while (Running && receiver.IsAlive && handler == null)
                        Thread.Sleep(500);
                    
                    if (handler != null)
                    {
                        WriteErrorLog("Incoming TCP connection established");
                        TcpClient send = new TcpClient("localhost", sendPort);
                        NetworkStream receiveStream = handler.GetStream();
                        NetworkStream sendStream = send.GetStream();
                        StreamReader reader = new StreamReader(receiveStream);
                        StreamWriter writer = new StreamWriter(sendStream);
                        string line = null;

                        do
                        {
                            line = reader.ReadLine();

                            if (!(string.IsNullOrEmpty(line)))
                            {
                                WriteErrorLog("DataIn: " + line);
                                string cef = ConvertToCEF(version, deviceVendor, deviceProduct, deviceVersion, signatureID, severity, line);
                                writer.Write(cef + "\n");
                                WriteErrorLog("DataOut: " + cef);
                            }
                        } while (!(string.IsNullOrEmpty(line)));
                        reader.Dispose();
                        writer.Dispose();
                        receiveStream.Dispose();
                        sendStream.Dispose();
                        WriteErrorLog("Connection completed");
                        handler = null;
                    }
                }

                WriteErrorLog("Stopping listening for incoming TCP connections");
                listener.Stop();
            }
            catch(Exception e)
            {
                WriteErrorLog(e);
            }
        }

        //Configuration File Handling Functions
        public static void ParseConfigurationFile(String ConfigFilePath)
        {
            TextReader iniFile = null;
            String line = null;
            String currentRoot = null;
            String[] keyPair = null;

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    iniFile = new StreamReader(ConfigFilePath);

                    line = iniFile.ReadLine();

                    while (line != null)
                    {
                        line = line.Trim();

                        if (line != "")
                        {
                            if (line.StartsWith("[") && line.EndsWith("]"))
                            {
                                currentRoot = line.Substring(1, line.Length - 2);
                            }
                            else
                            {
                                keyPair = line.Split(new char[] { '=' }, 2);

                                SectionPair sectionPair;
                                String value = null;

                                if (currentRoot == null)
                                    currentRoot = "ROOT";

                                sectionPair.Section = currentRoot;
                                sectionPair.Key = keyPair[0];

                                if (keyPair.Length > 1)
                                    value = keyPair[1];

                                keyPairs.Add(sectionPair, value);
                            }
                        }

                        line = iniFile.ReadLine();
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (iniFile != null)
                        iniFile.Close();
                }
            }

            //Validate Settings
            int receivePort, sendPort, signatureID, severity;

            bool parsedReceivePort = Int32.TryParse(GetSetting("ConvertSysLogToCEF", "SendPort"), out receivePort);
            if (!(parsedReceivePort))
            {
                WriteErrorLog("Unable to get ReceivePort, setting to 17480");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "ReceivePort";
                keyPairs.Add(sectionPair, 17480);
            }
            else
                WriteErrorLog("Setting: ReceivePort = " + receivePort);

            bool parsedSendPort = Int32.TryParse(GetSetting("ConvertSysLogToCEF", "SendPort"), out sendPort);
            if (!(parsedSendPort))
            {
                WriteErrorLog("Unable to get SendPort, setting to 17481");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "SendPort";
                keyPairs.Add(sectionPair, 17481);
            }
            else
                WriteErrorLog("Setting: SendPort = " + sendPort);

            bool parsedSignatureID = Int32.TryParse(GetSetting("ConvertSysLogToCEF", "SignatureID"), out signatureID);
            if (!(parsedSignatureID))
            {
                WriteErrorLog("Unable to get SignatureID, setting to 0");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "SignatureID";
                keyPairs.Add(sectionPair, 0);
            }
            else
                WriteErrorLog("Setting: SignatureID = " + signatureID);

            bool parsedSeverity = Int32.TryParse(GetSetting("ConvertSysLogToCEF", "Severity"), out severity);
            if (!(parsedSeverity))
            {
                WriteErrorLog("Unable to get Severity, setting to 0");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "Severity";
                keyPairs.Add(sectionPair, 0);
            }
            else
                WriteErrorLog("Setting: Severity = " + severity);

            string version = GetSetting("ConvertSysLogToCEF", "Version");
            if (string.IsNullOrEmpty(version))
            {
                WriteErrorLog("Unable to get Version, setting to CEF:0");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "Version";
                keyPairs.Add(sectionPair, "CEF:0");
            }
            else
                WriteErrorLog("Setting: Version = " + version);

            string deviceVendor = GetSetting("ConvertSysLogToCEF", "DeviceVendor");
            if (string.IsNullOrEmpty(deviceVendor))
            {
                WriteErrorLog("Unable to get DeviceVendor, setting to Tanium");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "DeviceVendor";
                keyPairs.Add(sectionPair, "Tanium");
            }
            else
                WriteErrorLog("Setting: DeviceVendor = " + deviceVendor);

            string deviceVersion = GetSetting("ConvertSysLogToCEF", "DeviceVersion");
            if (string.IsNullOrEmpty(deviceVersion))
            {
                WriteErrorLog("Unable to get DeviceVersion, setting to 6.5.314.4316");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "DeviceVersion";
                keyPairs.Add(sectionPair, "6.5.314.4316");
            }
            else
                WriteErrorLog("Setting: DeviceVersion = " + deviceVersion);

            string deviceProduct = GetSetting("ConvertSysLogToCEF", "DeviceProduct");
            if (string.IsNullOrEmpty(deviceProduct))
            {
                WriteErrorLog("Unable to get DeviceProduct, setting to TaniumApplicationServer");
                SectionPair sectionPair;
                sectionPair.Section = "ConvertSysLogToCEF";
                sectionPair.Key = "DeviceProduct";
                keyPairs.Add(sectionPair, "TaniumApplicationServer");
            }
            else
                WriteErrorLog("Setting: DeviceProduct = " + deviceProduct);
        }

        private struct SectionPair
        {
            public String Section;
            public String Key;
        }

        public static String GetSetting(String sectionName, String settingName)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName;
            sectionPair.Key = settingName;

            return (String)keyPairs[sectionPair];
        }

        //Logging Functions
        public static void WriteErrorLog(Exception Ex)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(LogFile, true);
                sw.WriteLine(DateTime.Now.ToString() + ": " + Ex.Source.ToString().Trim() + "; " + Ex.Message.ToString().Trim());
                sw.Flush();
                sw.Close();
            }
            catch { }
        }
        public static void WriteErrorLog(string Message)
        {
            StreamWriter sw = null;
            FileInfo logFile = new FileInfo(LogFile);
            FileInfo oldLogFile = new FileInfo(OldLogFile);

            if (logFile.Length >= 10 * 1048576)
            {
                //Rotate log file
                if (File.Exists(OldLogFile))
                    File.Delete(OldLogFile);

                bool fileExists = true;
                while (fileExists)
                {
                    if (File.Exists(OldLogFile))
                    {
                        Thread.Sleep(500);
                        WriteErrorLog("Waiting on log file to roll...");
                    }
                    else
                    {
                        logFile.MoveTo(OldLogFile);
                        fileExists = false;
                    }
                }
            }
            try
            {
                sw = new StreamWriter(LogFile, true);
                sw.WriteLine(DateTime.Now.ToString() + ": " + Message);
                sw.Flush();
                sw.Close();
            }
            catch { }
        }
    }
}