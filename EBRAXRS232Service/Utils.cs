using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Configuration;
using System.IO.Ports;
using System.Runtime.InteropServices;


namespace EBRAXRS232Service
{
    public enum EBRAXEvents
    {
        COMOpen,
        COMSilent,
        Starting,
        COMReading,
        Stoping
    }

    public enum EBRAXStatusTypes
    {
        OK = 0,
        Activated = 1,
        Alarmed = 5,
        Conceal = 4,
        DamagedT = 6,
        DamagedTA = 7,
        Unknown = 9,
        COMNoOpen = 80,
        COMNoRead = 81,
        COMUnknow = 89
    }

    public class Utils
    {
        private static EventLogger ELog = new EventLogger();

        public static SerialPort COMPort()
        {
            SerialPort ComP = new SerialPort();
            
            ComP.PortName = RS232Port.Default.PortName;
            ComP.BaudRate = RS232Port.Default.BaudRate;
            ComP.Parity = RS232Port.Default.Parity;
            ComP.StopBits = RS232Port.Default.StopBits;
            ComP.DataBits = RS232Port.Default.DataBits;
            ComP.Handshake = RS232Port.Default.Handshake;
            ComP.WriteTimeout = RS232Port.Default.WriteTimeout;
            ComP.ReadTimeout = RS232Port.Default.ReadTimeout;

            return ComP;
        }

        public static int WriteToLog(string message, 
                                      EBRAXStatus lastReadStatus, 
                                      EBRAXStatus actualReadStatus, 
                                      string EventLogSource, 
                                      string EventLogLog,
                                      bool SendNDCStatus)
        {
            int result = -1;

            string LogFile = EBRAXRS232Service.pathToExe + RS232PortReaderSrv.Default.EBRAXStatusLogFile;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogFile, true))
            {
                try
                {
                    if (LogWriteDecide(lastReadStatus, actualReadStatus))
                    {
                        file.WriteLine(message);
                        result = 10;
                    }
                }
                catch (Exception e)
                {
                    ELog.SSource = EventLogSource;
                    ELog.SLog = EventLogLog;
                    ELog.Error(e.Message);
                    ELog.Error(e.StackTrace);
                    result = 20;
                }
            }

            if (SendNDCStatus && LogWriteDecide(lastReadStatus, actualReadStatus)) 
            {
                string[] strSplitted = message.Split('\t');
                result += NDCStatusSender(int.Parse(strSplitted[3]), false, EventLogSource, EventLogLog);
            }

            return result;
        }

        private static int NDCStatusSender(int valStatus, 
                                           bool sol,
                                           string EventLogSource, 
                                           string EventLogLog) 
        {
            string e1 = "P"; //DIG
            string e2; //Device Status. Always 9 for EBRAX
            string e3; //Error Severity. This can be good, warning or fatal.
            int result = -1;

            switch (valStatus)
            {
                case 0:
                case 1:
                    e2 = "0" + valStatus.ToString();
                    break;
                case 4:
                case 5:
                    e2 = "7" + valStatus.ToString();
                    break;
                case 6:
                case 7:
                case 9:
                    e2 = "9" + valStatus.ToString();
                    break;
                default:
                    e2 = valStatus.ToString();
                    break;
            }

            switch (valStatus)
            {
                case 0:
                    e3 = "GOOD";
                    break;
                case 1:
                    e3 = "WARNING";
                    break;
                case 4:
                    e3 = "ALARM_TO_OFF";
                    break;
                case 5:
                    e3 = "ALARMED";
                    break;
                case 6:
                    e3 = "TRANDAM_AlROFF";
                    break;
                case 7:
                    e3 = "TRANDAM_AlRON";
                    break;
                case 80:
                case 81:
                    e3 = "COM_ERROR";
                    break;
                default:
                    e3 = "UNKNOWN";
                    break;
            }

            string[] sendArgs = { "false", e1, e2, e3 };
            
            try 
            {
                SendStatus(sendArgs);
            }
            catch (Exception e) 
            {
                ELog.SSource = EventLogSource;
                ELog.SLog = EventLogLog;
                ELog.Error("Error Sending Status to NDC.", e);
            }

            return result;
        }

        private static void SendStatus(string[] eargs)
        {
            string pathToExe;
            int i;
            string cmd;
            string arguments = string.Empty;

            pathToExe = System.Reflection.Assembly.GetEntryAssembly().Location;
            pathToExe = pathToExe.Substring(0, pathToExe.LastIndexOf("\\") + 1);
            i = -1;
            foreach (string item in eargs)
            {
                i++;
                arguments += "-e" + i.ToString() + " " + eargs[i] + " ";

            }
            cmd = pathToExe + RS232PortReaderSrv.Default.RunProgram + " " + arguments.Trim();
            ProcessAsUser.Launch(cmd);
        }

        private static bool LogWriteDecide(EBRAXStatus lastReadStatus, EBRAXStatus actualReadStatus)
        {
            bool writeToLog = false;

            if ((lastReadStatus.Event == EBRAXEvents.Starting) ||
                (actualReadStatus.Event == EBRAXEvents.COMOpen) ||
                (actualReadStatus.Event == EBRAXEvents.COMSilent) ||
                (actualReadStatus.Event == EBRAXEvents.Stoping) ||
                !EBRAXStatus.Equal(lastReadStatus, actualReadStatus))
                writeToLog = true;

            if (actualReadStatus.Status == EBRAXStatusTypes.OK)
            {
                if (lastReadStatus.Status != EBRAXStatusTypes.Activated &&
                    lastReadStatus.Status != EBRAXStatusTypes.OK)
                    writeToLog = true;
                else
                    writeToLog = false;
            }

            if (actualReadStatus.Status == EBRAXStatusTypes.Activated)
            {
                if (actualReadStatus.WarnCounter == RS232PortReaderSrv.Default.WarningTimeout)
                    writeToLog = true;
                else
                    writeToLog = false;
            }

            if (lastReadStatus.Status == EBRAXStatusTypes.Activated &&
                actualReadStatus.Status == EBRAXStatusTypes.OK &&
                lastReadStatus.WarnCounter > RS232PortReaderSrv.Default.WarningTimeout)
                writeToLog = true;

            return writeToLog;
        }
    }

    public class EventLogger
    {
        private string sSource;
        private string sLog;
        private bool bLogExists;

        public bool BLogExists
        {
            get { return bLogExists; }
        }

        public string SSource
        {
            get { return sSource; }
            set { sSource = value; }
        }

        public string SLog
        {
            get { return sLog; }
            set { sLog = value; }
        }

        public EventLogger(string Source, string Log)
        {
            sSource = Source;
            sLog = Log;
            CreateSource();
        }

        public EventLogger()
        {
            bLogExists = false;
        }

        private void CreateSource()
        {
            if (!EventLog.SourceExists(sSource))
            {
                try
                {
                    EventLog.CreateEventSource(sSource, sLog);
                    bLogExists = true;
                }
                catch
                {
                    bLogExists = false;
                }
            }
            else
            {
                bLogExists = true;
            }
        }

        private void WriteToEventLog(string sEvent, EventLogEntryType eventType)
        {
            EventLog myLog = new EventLog();
            myLog.Source = sSource;
            myLog.Log = sLog;

            myLog.WriteEntry(sEvent, eventType);
        }

        public void Info(string sEvent)
        {
            CreateSource();
            if (bLogExists)
                WriteToEventLog(sEvent, EventLogEntryType.Information);
        }

        public void Warning(string sEvent)
        {
            CreateSource();
            if (bLogExists)
                WriteToEventLog(sEvent, EventLogEntryType.Warning);
        }

        public void Error(string sEvent)
        {
            CreateSource();
            if (bLogExists)
                WriteToEventLog(sEvent, EventLogEntryType.Error);
        }

        public void Error(string sTopic, Exception ex)
        {
            CreateSource();
            if (bLogExists)
                WriteToEventLog(sTopic + "\n" + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
        }
    }

    public class EBRAXStatus
    {
        private DateTime timeStamp;
        private EBRAXStatusTypes strStatus;
        private EBRAXEvents strEvent;
        private int warnCounter;

        public int WarnCounter
        {
            get { return warnCounter; }
            set { warnCounter = value; }
        }

        public EBRAXEvents Event
        {
            get { return strEvent; }
            set { strEvent = value; }
        }

        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }


        public EBRAXStatusTypes Status
        {
            get { return strStatus; }
            set { strStatus = value; }
        }

        public static bool Equal(EBRAXStatus Status1, EBRAXStatus Status2)
        {
            bool equal = false;
            if ((Status1.Status == Status2.Status) && 
                (Status1.Event == Status2.Event))
                equal = true;
            return equal;
        }

        public void Copy(EBRAXStatus Status)
        {
            timeStamp = Status.timeStamp;
            strStatus = Status.Status;
            strEvent = Status.Event;
            warnCounter = Status.WarnCounter;
        }

        public EBRAXStatus()
        {
            timeStamp = DateTime.Now;
            strEvent = EBRAXEvents.Starting;
            strStatus = EBRAXStatusTypes.COMUnknow;
            warnCounter = 0;
        }

        public EBRAXStatus(DateTime ts, EBRAXStatusTypes st, EBRAXEvents evt)
        {
            timeStamp = ts;
            strEvent = evt;
            strStatus = st;
        }
    }
}
