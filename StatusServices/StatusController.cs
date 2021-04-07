using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogUtils;

namespace StatusServices
{
    public enum ApplicationStates
    {
        Initialized = 0,
        AppRunStateFalse = 1,
        AppRunStateTrue = 2
    }


    public class StatusController
    {
        readonly LEDManager ledManager = new LEDManager();
        private readonly BlockingCollection<string> controlQueue = new BlockingCollection<string>();

        private ApplicationStates CMApplicationState = ApplicationStates.Initialized;
        private ApplicationStates DCDSApplicationState = ApplicationStates.Initialized;
        private readonly System.Timers.Timer cm_timer = null;
        private readonly System.Timers.Timer ds_timer = null;
        private readonly System.Timers.Timer www_timer = null;
        private int cm_counter = 0;
        private int ds_counter = 0;
        private string cm_msg_recv = "";
        private string dcds_msg_recv = "";
        private bool internet_available = false;
        private bool prev_internet_available = false;
        public StatusController()
        {
            int loop_sleep_secs = (int)(2.5 * AppOptions.GetAppOptions.PingIntervalSecs);
            cm_timer = new System.Timers.Timer(loop_sleep_secs*1000);
            cm_timer.Elapsed += Cm_timer_Elapsed;
            cm_timer.AutoReset = true;
            cm_timer.Enabled = false;
            cm_counter = 0;
            ds_timer = new System.Timers.Timer(loop_sleep_secs*1000);
            ds_timer.Elapsed += Ds_timer_Elapsed;
            ds_timer.AutoReset = true;
            ds_timer.Enabled = false;
            www_timer = new System.Timers.Timer(60 * 1000);
            www_timer.Elapsed += Www_timer_Elapsed;
            www_timer.AutoReset = true;
            www_timer.Enabled = true;
        
        }



        private void Www_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            try
            {
                PingReply reply = pingSender.Send("www.google.com", timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    DCLogger.Verbose($"Address: {reply.Address.ToString()}" );
                    DCLogger.Verbose($"RoundTrip time: {reply.RoundtripTime}" );
                    //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                    //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                    //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                    internet_available = true;
                }
                else
                {
                    internet_available = false;
                }

            }
            catch (Exception)
            {
                internet_available = false;
            }        
            if (prev_internet_available != internet_available)
            {
                DCLogger.Debug($"status change: internet available = {internet_available}");
                SetStatusLEDStates();
                DCLogger.Information($"status app mode {Enum.GetName(typeof(LEDModes), ledManager.StatusLEDMode)}");
                DCLogger.Information($"pl status  mode {Enum.GetName(typeof(LEDModes), ledManager.PLStatusLEDMode)}");
                prev_internet_available = internet_available;
            }
        }

        private void Ds_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Interlocked.Increment(ref ds_counter);
            if (ds_counter >= 2)
            {
                DCDSApplicationState = ApplicationStates.Initialized;
                ds_timer.Enabled = false;
                DCLogger.Information("Data Services Timeout.");
                SetStatusLEDStates();
                SetPLNLEDStates();
                DCLogger.Debug($"status app mode {Enum.GetName(typeof(LEDModes), ledManager.StatusLEDMode)}");
                DCLogger.Debug($"pl status  mode {Enum.GetName(typeof(LEDModes), ledManager.PLStatusLEDMode)}");
            }
        }

        private void Cm_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Interlocked.Increment(ref cm_counter);
            if (cm_counter >= 2)
            {
                CMApplicationState = ApplicationStates.Initialized;
                cm_timer.Enabled = false;
                DCLogger.Information("Central Master Timeout.");
                SetStatusLEDStates();
                SetPLNLEDStates();
                DCLogger.Debug($"status app mode {Enum.GetName(typeof(LEDModes), ledManager.StatusLEDMode)}");
                DCLogger.Debug($"pl status  mode {Enum.GetName(typeof(LEDModes), ledManager.PLStatusLEDMode)}");
            }
        }

        private void SetStatusLEDStates()
        {
            // conditions for off
            if ( (CMApplicationState == ApplicationStates.Initialized) && (DCDSApplicationState == ApplicationStates.Initialized) )
            {
                ledManager.StatusLEDMode = LEDModes.Off;
                return;
            }

            // conidtions for slow blink
            if ( 
                 ((CMApplicationState != ApplicationStates.Initialized) && (DCDSApplicationState == ApplicationStates.Initialized)) ||
                 ((CMApplicationState == ApplicationStates.Initialized) && (DCDSApplicationState != ApplicationStates.Initialized))
                )
            {
                ledManager.StatusLEDMode = LEDModes.SlowBlink;
                return;
            }

            if ((CMApplicationState != ApplicationStates.Initialized) && (DCDSApplicationState != ApplicationStates.Initialized))
            {
                if (internet_available && (DCDSApplicationState == ApplicationStates.AppRunStateTrue))
                {
                    ledManager.StatusLEDMode = LEDModes.On;
                }
                else
                {
                    ledManager.StatusLEDMode = LEDModes.FastBlink;
                }
                return;
            }


            //// set appropriate LEDs
            //if ((DCDSApplicationState == ApplicationStates.Initialized) && (CMApplicationState == ApplicationStates.Initialized))
            //{
            //    ledManager.StatusLEDMode = LEDModes.Off;
            //}
            //else if ((DCDSApplicationState == ApplicationStates.Initialized) && (CMApplicationState != ApplicationStates.Initialized))
            //{
            //    ledManager.StatusLEDMode = LEDModes.SlowBlink;
            //}
            //else if ((DCDSApplicationState != ApplicationStates.Initialized) && (CMApplicationState == ApplicationStates.Initialized))
            //{
            //    ledManager.StatusLEDMode = LEDModes.SlowBlink;
            //}
            //else if ((DCDSApplicationState == ApplicationStates.AppRunStateTrue) && (CMApplicationState != ApplicationStates.Initialized))
            //{
            //    ledManager.StatusLEDMode = LEDModes.On;
            //}
            //else if ((DCDSApplicationState != ApplicationStates.Initialized) && (CMApplicationState != ApplicationStates.Initialized))
            //{
            //    ledManager.StatusLEDMode = LEDModes.FastBlink;
            //}
        }

        private void SetPLNLEDStates()
        {
            // pln status led
            if (CMApplicationState == ApplicationStates.Initialized)
            {
                ledManager.PLStatusLEDMode = LEDModes.Off;
            }
            else if (CMApplicationState == ApplicationStates.AppRunStateFalse)
            {
                ledManager.PLStatusLEDMode = LEDModes.SlowBlink;
            }
            else if (CMApplicationState == ApplicationStates.AppRunStateTrue)
            {
                ledManager.PLStatusLEDMode = LEDModes.On;
            }

        }

        public void ControlAction()
        {
            // run regular application
            DCPipeServer dCPipeServer = new DCPipeServer("DCStatusPipe");
            dCPipeServer.StartServer();
            dCPipeServer.receivedData += ReceivedStatus;
            DCDSApplicationState = ApplicationStates.Initialized;
            CMApplicationState = ApplicationStates.Initialized;
            Task.Factory.StartNew(() => ledManager.Manage(), TaskCreationOptions.AttachedToParent);
            ApplicationStates prevCMState = ApplicationStates.Initialized;
            ApplicationStates prevDCDSState = ApplicationStates.Initialized;
            string recv_msg;
            while (true)
            {
                if (controlQueue.TryTake(out recv_msg, 5*60*1000))
                {
                    string process_msg = recv_msg.ToLower();
                    if ( (process_msg.StartsWith("cm")) && (!cm_msg_recv.Equals(process_msg) ) )
                    {
                        cm_msg_recv = process_msg;
                        DCLogger.Debug($"in control action get process msg {process_msg}");
                    }
                    if ((process_msg.StartsWith("dcds")) && (!dcds_msg_recv.Equals(process_msg)))
                    {
                        dcds_msg_recv = process_msg;
                        DCLogger.Debug($"in control action get process msg {process_msg}");
                    }

                    if ((process_msg.StartsWith("cm")) && (!cm_timer.Enabled) )
                    {
                        cm_timer.Enabled = true;
                    }
                    if ((process_msg.StartsWith("dcds")) && (!ds_timer.Enabled) )
                    {
                        ds_timer.Enabled = true;
                    }


                    prevCMState = CMApplicationState;
                    prevDCDSState = DCDSApplicationState;
                    if (process_msg.StartsWith("cm-pln-true"))
                    {
                        Interlocked.Add(ref cm_counter, -cm_counter);
                        CMApplicationState = ApplicationStates.AppRunStateTrue;
                    }
                    else if (process_msg.StartsWith("cm-pln-false"))
                    {
                        Interlocked.Add(ref cm_counter, -cm_counter);
                        CMApplicationState = ApplicationStates.AppRunStateFalse;
                    }
                    else if (process_msg.StartsWith("dcds-www-true"))
                    {
                        Interlocked.Add(ref ds_counter, -ds_counter);
                        DCDSApplicationState = ApplicationStates.AppRunStateTrue;
                    }
                    else if (process_msg.StartsWith("dcds-www-false"))
                    {
                        Interlocked.Add(ref ds_counter, -ds_counter);
                        DCDSApplicationState = ApplicationStates.AppRunStateFalse;
                    }

                    SetStatusLEDStates();
                    SetPLNLEDStates();
                    if ((prevCMState != CMApplicationState) || (prevDCDSState != DCDSApplicationState))
                    {
                        DCLogger.Information($"status app mode {Enum.GetName(typeof(LEDModes), ledManager.StatusLEDMode)}");
                        DCLogger.Information($"pl status  mode {Enum.GetName(typeof(LEDModes), ledManager.PLStatusLEDMode)}");
                        DCLogger.Information($"cm count = {cm_counter};  dcds count = {ds_counter}");
                    }
                }
            }
        }

        private void ReceivedStatus(string msg)
        {
            controlQueue.Add(msg);
        }
    }
}
