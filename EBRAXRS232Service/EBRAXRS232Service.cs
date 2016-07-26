using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Threading;


namespace EBRAXRS232Service
{
    public partial class EBRAXRS232Service : ServiceBase
    {
        public static string pathToExe;
        private static DateTime EBRAXlastTimeRead;
        private static EBRAXStatus EBRAXActualStatus;
        private static EBRAXStatus EBRAXPreviousStatus;
        private static Int16 EBRAXCycle;
        private static System.Timers.Timer CheckIfRead;
        private static SerialPort EBRAXRS232;

        public EBRAXRS232Service()
        {
            InitializeComponent();
            //
            CheckIfRead = new System.Timers.Timer();
            CheckIfRead.Elapsed += new ElapsedEventHandler(CheckIfRead_Elapsed);
            // Get where is the exe file
            pathToExe = System.Reflection.Assembly.GetEntryAssembly().Location;
            pathToExe = pathToExe.Substring(0, pathToExe.LastIndexOf("\\") + 1);
            // Config Event Logger
            if (!System.Diagnostics.EventLog.SourceExists(RS232PortReaderSrv.Default.LogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(RS232PortReaderSrv.Default.LogSource, RS232PortReaderSrv.Default.LogLog);
            }
            evtLogger.Source = RS232PortReaderSrv.Default.LogSource;
            evtLogger.Log = RS232PortReaderSrv.Default.LogLog;
            EBRAXRS232 = Utils.COMPort();
            EBRAXRS232.DataReceived +=new SerialDataReceivedEventHandler(EBRAXRS232_DataReceived);
        }

        protected override void OnStart(string[] args)
        {
            if (RS232PortReaderSrv.Default.LogLevel >= 3) 
                Info("Starting EBRAXRS232Service");
            //Set Read Satus Variables
            EBRAXActualStatus = new EBRAXStatus();
            EBRAXPreviousStatus = new EBRAXStatus();
            EBRAXlastTimeRead = DateTime.Now;
            EBRAXCycle = 0;
            //Will try to open port
            OpenPort();
            //Configuring time

            CheckIfRead.Interval = RS232PortReaderSrv.Default.ReadSleepTime;
            CheckIfRead.Enabled = true;
        }

        protected override void OnStop()
        {
            int retryCount = 0;
            CheckIfRead.Enabled = false;
            while (!ClosePort())
            {
                Thread.Sleep(3000);
                retryCount++;
                if (retryCount == 3)
                    break;
            }
        }

        ////////////////////////////////
        //         Timer Event        //
        ////////////////////////////////

        private void CheckIfRead_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimeSpan TsSinceLastRead;
            bool reportToNDC;

            if (RS232PortReaderSrv.Default.LogLevel >= 3)
                Info("Tick // Cycle: " + EBRAXCycle.ToString() + " // Port: " + EBRAXRS232.IsOpen.ToString());
            //updating cycle count
            if (EBRAXCycle == RS232PortReaderSrv.Default.MaxCycle)
                EBRAXCycle = 1;
            else
                EBRAXCycle++;

            // If Max # Cycle then Report NDC
            if (EBRAXCycle == RS232PortReaderSrv.Default.MaxCycle)
                reportToNDC = true;
            else
                reportToNDC = false;

            // Check if port is open. true: Check last time read, false: Try to open and sleep;
            if (EBRAXRS232.IsOpen)
            {
                //Check last time port was read
                TsSinceLastRead = DateTime.Now - EBRAXlastTimeRead;

                if (TsSinceLastRead.TotalSeconds >= (RS232PortReaderSrv.Default.ReadSleepTime / 1000))
                {
                    string message;
                    string messageTail = string.Empty;


                    message = string.Format("EBRAX PORT {0} has not been read since: {1:D2}d {2:D2}h {3:D2}m {4:D2}s.",
                                           EBRAXRS232.PortName,
                                           TsSinceLastRead.Days,
                                           TsSinceLastRead.Hours,
                                           TsSinceLastRead.Minutes,
                                           TsSinceLastRead.Seconds);

                    // Try to reopen every ReopenCycle
                    if (EBRAXCycle % RS232PortReaderSrv.Default.ReopenTryCycle == 0)
                        messageTail = "Will Close and Open Port to try to fix issue.";

                    // Log to event log
                    if (RS232PortReaderSrv.Default.LogLevel >= 0)
                        Error(message + "\n" + messageTail);
                    // Log to application log
                    EBRAXActualStatus.Event = EBRAXEvents.COMOpen;
                    EBRAXActualStatus.Status = EBRAXStatusTypes.COMNoRead;
                    EBRAXActualStatus.TimeStamp = DateTime.Now;
                    Utils.WriteToLog(EBRAXActualStatus.TimeStamp.ToString() + "\t" +
                             EBRAXActualStatus.Event.ToString() + "\t" +
                             EBRAXActualStatus.Status.ToString() + "\t" +
                             "81" + "\t" +
                             ConfigurationManager.AppSettings[RS232PortReaderSrv.Default.Culture + "_" + "81"].ToString() +
                             message + messageTail,
                             EBRAXPreviousStatus,
                             EBRAXActualStatus,
                             RS232PortReaderSrv.Default.LogSource,
                             RS232PortReaderSrv.Default.LogLog,
                             reportToNDC);
                    EBRAXPreviousStatus.Copy(EBRAXActualStatus);

                    // Closing and opening Port
                    if (EBRAXCycle % RS232PortReaderSrv.Default.ReopenTryCycle == 0)
                    {
                        ClosePort();
                        OpenPort();
                    }
                }
            }
            else
            {
                if (!OpenPort()) // Try to open port 
                {
                    //Any other try just log locally
                    EBRAXActualStatus.Event = EBRAXEvents.COMOpen;
                    EBRAXActualStatus.Status = EBRAXStatusTypes.COMNoOpen;
                    EBRAXActualStatus.TimeStamp = DateTime.Now;
                    Utils.WriteToLog(EBRAXActualStatus.TimeStamp.ToString() + "\t" +
                            EBRAXActualStatus.Event.ToString() + "\t" +
                            EBRAXActualStatus.Status.ToString() + "\t" +
                            "80" + "\t" +
                            ConfigurationManager.AppSettings[RS232PortReaderSrv.Default.Culture + "_" + "80"].ToString(),
                            EBRAXPreviousStatus,
                            EBRAXActualStatus,
                            RS232PortReaderSrv.Default.LogSource,
                            RS232PortReaderSrv.Default.LogLog,
                            reportToNDC);
                    EBRAXPreviousStatus.Copy(EBRAXActualStatus);
                }
            }
        }

        ////////////////////////////////
        //     Data in Port Event     //
        ////////////////////////////////

        private void EBRAXRS232_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            EBRAXlastTimeRead = DateTime.Now;
            EBRAXActualStatus.Event = EBRAXEvents.COMReading;
            EBRAXActualStatus.Status = (EBRAXStatusTypes)Enum.Parse(typeof(EBRAXStatusTypes), indata.Substring(1, 1));
            EBRAXActualStatus.TimeStamp = DateTime.Now;

            // If a warning state comes increase WarnCounter
            if (EBRAXActualStatus.Status == EBRAXStatusTypes.Activated)
                EBRAXActualStatus.WarnCounter = EBRAXPreviousStatus.WarnCounter + 1;
            else // If not reset to 0
                EBRAXActualStatus.WarnCounter = 0;

            Utils.WriteToLog(EBRAXActualStatus.TimeStamp.ToString() + "\t" +
                             EBRAXActualStatus.Event.ToString() + "\t" +
                             EBRAXActualStatus.Status.ToString() + "\t" +
                             indata.Substring(1, 1) + "\t" +
                             ConfigurationManager.AppSettings[RS232PortReaderSrv.Default.Culture + "_" + indata.Substring(1, 1)].ToString(),
                             EBRAXPreviousStatus,
                             EBRAXActualStatus,
                             RS232PortReaderSrv.Default.LogSource,
                             RS232PortReaderSrv.Default.LogLog,
                             true);
            EBRAXPreviousStatus.Copy(EBRAXActualStatus);
        }

        ////////////////////////////////
        //       RS232 Handling       //
        ////////////////////////////////

        private bool OpenPort()
        {
            bool portIsOpen = false;
            try
            {
                EBRAXRS232.Open();
                portIsOpen = EBRAXRS232.IsOpen;
                if (RS232PortReaderSrv.Default.LogLevel >= 3)
                    Info("Open EBRAX Port (" + EBRAXRS232.PortName + "): OPEN_SUCCESS");
            }
            catch (Exception ex)
            {
                if (RS232PortReaderSrv.Default.LogLevel >= 3)
                    Error("Error trying to open EBRAX Port (" + EBRAXRS232.PortName + "): OPEN_FAIL", ex);
            }
            return portIsOpen;
        }

        private bool ClosePort()
        {
            bool portIsClosed = false;
            if (EBRAXRS232.IsOpen)
            {
                try 
                { 
                    EBRAXRS232.Close();
                    portIsClosed = true;
                    if (RS232PortReaderSrv.Default.LogLevel >= 3)
                        Info("Trying to close EBRAX Port (" + EBRAXRS232.PortName + "): CLOSE_SUCCESS");
                }
                catch (Exception ex)
                {
                    if (RS232PortReaderSrv.Default.LogLevel >= 3)
                        Error("Trying to close EBRAX Port (" + EBRAXRS232.PortName + "): CLOSE_FAIL", ex);
                }
            }
            else
            {
                if (RS232PortReaderSrv.Default.LogLevel >= 3)
                    Info("Trying to close EBRAX Port (" + EBRAXRS232.PortName + "): CLOSE_NOT_OPEN");
                portIsClosed = true;
            }
            return portIsClosed;
        }

        ////////////////////////////////
        //      Logging Handling      //
        ////////////////////////////////

        private void WriteToEvtLog(string sEvent, EventLogEntryType eventType)
        {
            evtLogger.WriteEntry(sEvent, eventType);
        }

        public void Info(string sEvent)
        {
            WriteToEvtLog(sEvent, EventLogEntryType.Information);
        }

        public void Warning(string sEvent)
        {
            WriteToEvtLog(sEvent, EventLogEntryType.Warning);
        }

        public void Error(string sEvent)
        {
            WriteToEvtLog(sEvent, EventLogEntryType.Error);
        }

        public void Error(string sEvent, Exception ex)
        {
            WriteToEvtLog(sEvent + "\n" + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
        }
    }
}
