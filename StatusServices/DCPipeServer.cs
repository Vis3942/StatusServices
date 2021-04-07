using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogUtils;

namespace StatusServices
{
    /// <summary>
    /// for use with Status Services:
    /// Allows 2 clients to join: CentralMaster and DCDataServices
    /// </summary>
    public class DCPipeServer
    {
        #region vars
        private string _pipe_name = "";
        private readonly List<NamedPipeServerStream> serverPipes = new List<NamedPipeServerStream>();
        //Task serverTask = null;
        //private StreamWriter swriter = null;
        //private StreamReader sreader = null;
        private CancellationTokenSource pipeServerTokenSrc = new CancellationTokenSource();
        public Action<string> receivedData = null;
        private readonly BlockingCollection<string> dataQueue = new BlockingCollection<string>();
        private bool serverStarted = false;
        NamedPipeServerStream serverPipe1 = null;
        NamedPipeServerStream serverPipe2 = null;
        readonly ManualResetEvent mre = new ManualResetEvent(false);
        #endregion // vars

        #region property
        public string PipeName
        {
            get { return _pipe_name; }
            set { _pipe_name = value; }
        }

        public bool IsConnected
        {
            get
            {
                if ((serverPipe1 != null) || (serverPipe2 != null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion // property

        public DCPipeServer(string pipe_name)
        {
            _pipe_name = pipe_name;
            mre.Reset();
        }


        public void StartServer()
        {
            if (serverStarted == false)
            {
                Task.Factory.StartNew(() => ProcessReceivedMessages(), pipeServerTokenSrc.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                Task.Factory.StartNew(() => StartPipedServerAsync(), pipeServerTokenSrc.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                serverStarted = true;
            }
        }


        public void StopServer()
        {
            if (serverStarted)
            {
                pipeServerTokenSrc.Cancel();
                foreach (var serverPipe in serverPipes)
                {
                    if (serverPipe.IsConnected)
                    {
                        serverPipe.Disconnect();
                    }
                    //serverPipe = null;
                    serverPipes.Remove(serverPipe);
                }
                System.Threading.Thread.Sleep(1000);
                pipeServerTokenSrc = new CancellationTokenSource();
                serverStarted = false;
            }
        }

        private void ProcessConnection(NamedPipeServerStream npss)
        {
            StreamWriter swriter = new StreamWriter(npss)
            {
                AutoFlush = true
            };
            StreamReader sreader = new StreamReader(npss);
            try
            {
                var task = Task.Factory.StartNew(() => ReadDataFromPipe(sreader, pipeServerTokenSrc.Token), pipeServerTokenSrc.Token,
                    TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                if (task.IsFaulted)
                {
                    //break;
                }
                else
                {
                    task.Wait();
                }
                DCLogger.Information("read task completed");

            }
            catch (Exception e)
            {
                DCLogger.Error(e, "During read task");
            }
            finally
            {
                if (swriter != null)
                {
                    //swriter.Flush();
                    //swriter.Close();
                    swriter = null;
                }
                if (sreader != null)
                {
                    //sreader.Close();
                    sreader = null;
                }
                if ((npss != null) && (npss.IsConnected))
                {
                    npss.Disconnect();
                }
                //serverPipes.Remove(npss);
                if (npss == serverPipe1)
                {
                    npss = null;
                    serverPipe1 = null;
                }
                else if (npss == serverPipe2)
                {
                    npss = null;
                    serverPipe2 = null;
                }
                mre.Set();
                System.Threading.Thread.Sleep(500);
                mre.Reset();
                DCLogger.Verbose($"server pipe 1 is null = {serverPipe1 == null}");
                DCLogger.Verbose($"server pipe 2 is null = {serverPipe2 == null}");
            }
        }
        private async Task StartPipedServerAsync()
        {
            while (!pipeServerTokenSrc.IsCancellationRequested)
            {
                if (serverPipe1 == null)
                {
                    serverPipe1 = new NamedPipeServerStream(_pipe_name, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await serverPipe1.WaitForConnectionAsync(pipeServerTokenSrc.Token);
                    DCLogger.Information("got connection...Pipe 1");
                    if (serverPipe1.IsConnected)
                    {
                        Task.Factory.StartNew(() => ProcessConnection(serverPipe1), pipeServerTokenSrc.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                    }
                }
                else if (serverPipe2 == null)
                {
                    serverPipe2 = new NamedPipeServerStream(_pipe_name, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await serverPipe2.WaitForConnectionAsync(pipeServerTokenSrc.Token);
                    DCLogger.Information("got connection...Pipe 2");
                    if (serverPipe2.IsConnected)
                    {
                        Task.Factory.StartNew(() => ProcessConnection(serverPipe2), pipeServerTokenSrc.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                    }
                }
                else
                {
                    // no more pipes to connect
                    //System.Threading.Thread.Sleep(3000);
                    mre.WaitOne();
                }
            }
        }

        //public void WriteToPipe(string message)
        //{
        //    if (((serverPipe != null) && (serverPipe.IsConnected)) && (swriter != null))
        //    {
        //        swriter.WriteLine(message);
        //    }
        //}

        public void ReadDataFromPipe(StreamReader sreader, CancellationToken cts)
        {
            while ((!cts.IsCancellationRequested) && (sreader != null))
            {
                Task<string> t = sreader.ReadLineAsync();
                if (t.IsFaulted)
                {
                    break;
                }
                t.Wait(cts);
                string recv_str = t.Result;
                if (recv_str == null)
                {
                    break;
                }
                dataQueue.Add(recv_str, cts);
            }
            //if (!cts.IsCancellationRequested)
            //{
            //    pipeServerTokenSrc.Cancel();
            //}
            if (sreader != null)
            {
                sreader.Close();
            }
            // do you need to restart the connection
        }

        private void ProcessReceivedMessages()
        {
            string recv_msg;
            while (dataQueue.TryTake(out recv_msg, Timeout.Infinite, pipeServerTokenSrc.Token))
            {
                if (receivedData != null)
                {
                    receivedData(recv_msg);
                }
                else
                {
                    DCLogger.Information(string.Format("No receiver to send to: got: {0}", recv_msg));
                }
            }
        }

    }
}
