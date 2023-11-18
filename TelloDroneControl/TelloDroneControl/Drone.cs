using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelloDroneControl
{
    internal class Drone
    {
        private static UdpUser clientCommand;
        private static UdpUser clientState;
        private static UdpUser clientVideo;
        private static DateTime lastMessageTime;//for connection timeouts.

        public static int iFrameRate = 5;//How often to ask for iFrames in 50ms. Ie 2=10x 5=4x 10=2xSecond 5 = 4xSecond

        private static ushort sequence = 1;


        private static CancellationTokenSource cancelTokens = new CancellationTokenSource();//used to cancel listeners

        public static FlyData state = new FlyData();
        private static int wifiStrength = 0;

        private static Action<string>? writeLog;


        private static Message? currentlyPendingMessage = null;
        private static Queue<Message> messageQueue = new Queue<Message>();

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Paused,//used to keep from disconnecting when starved for input.
            UnPausing//Transition. Never stays in this state. 
        }
        public static ConnectionState connectionState = ConnectionState.Disconnected;

        public static void StartConnecting(Action<string> _writeLog)
        {
            writeLog -= _writeLog;
            writeLog += _writeLog;

            writeLog?.Invoke("Start Connecting");
            clientCommand = UdpUser.ConnectTo("192.168.10.1", 8889);
            StartListeners();
            StartSendingCommands();
            //Thread to handle connecting.
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    try
                    {

                        switch (connectionState)
                        {
                            case ConnectionState.Disconnected:

                                

                                connectionState = ConnectionState.Connecting;

                                messageQueue.Enqueue(new Message("command"));

                                lastMessageTime = DateTime.Now;

                                

                                break;
                            case ConnectionState.Connecting:
                            case ConnectionState.Connected:
                                var elapsed = DateTime.Now - lastMessageTime;
                                if (elapsed.Seconds > 10)
                                {
                                    Console.WriteLine("Connection timeout :");
                                    //writeLog?.Invoke("Connection timeout");
                                    //Disconnect();
                                }
                                break;
                            case ConnectionState.Paused:
                                lastMessageTime = DateTime.Now;//reset timeout so we have time to recover if enabled. 
                                break;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Connection thread error:" + ex.Message);
                        writeLog?.Invoke("Connection thread error:" + ex.Message);
                    }
                }
            });

        }




        public static void Disconnect()
        {
            //kill listeners
            cancelTokens.Cancel();
            clientCommand.Client.Close();


            connectionState = ConnectionState.Disconnected;

        }

        private static void StartSendingCommands()
        {
            CancellationToken token = cancelTokens.Token;

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested)//handle canceling thread.
                            break;
                        Thread.Sleep(500);

                        if(messageQueue.Count > 0 && currentlyPendingMessage == null)
                        {
                            currentlyPendingMessage = messageQueue.Dequeue();

                            byte[] packet = Encoding.UTF8.GetBytes(currentlyPendingMessage.message);
                            
                            writeLog?.Invoke("Sent: " + currentlyPendingMessage.message);
                            clientCommand.Send(packet);
                            currentlyPendingMessage.sentTime = DateTime.Now;
                        }

                        
                    }

                    catch (Exception eex)
                    {
                        Console.WriteLine("Receive thread error:" + eex.Message);
                        Disconnect();
                        break;
                    }
                }
            }, token);

            
        }

        private static void StartListeners()
        {
            cancelTokens = new CancellationTokenSource();
            CancellationToken token = cancelTokens.Token;
            writeLog?.Invoke("Starting listeners...");
            //wait for reply messages from the tello and process. 
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested)//handle canceling thread.
                            break;
                        var received = await clientCommand.Receive();
                        lastMessageTime = DateTime.Now;//for timeouts
                        
                        if (connectionState == ConnectionState.Connecting)
                        {
                            if (received.Message.StartsWith("ok"))
                            {
                                connectionState = ConnectionState.Connected;
                                writeLog?.Invoke("Connected");
                            }
                        }

                        if (currentlyPendingMessage != null)
                        {
                            TimeSpan timeDifference = lastMessageTime - currentlyPendingMessage.sentTime;
                            writeLog?.Invoke("Message: "+ currentlyPendingMessage.message +" Answer: "+ received.Message + "Respond Time: "+ timeDifference.TotalSeconds);

                            currentlyPendingMessage = null;
                        }



                    }

                    catch (Exception eex)
                    {
                        Console.WriteLine("Receive thread error:" + eex.Message);
                        Disconnect();
                        break;
                    }
                }
            }, token);
            //video server
            var videoServer = new UdpListener(6038);
            //var videoServer = new UdpListener(new IPEndPoint(IPAddress.Parse("192.168.10.2"), 6038));

            Task.Factory.StartNew(async () => {
                //Console.WriteLine("video:1");
                var started = false;

                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested)//handle canceling thread.
                            break;
                        var received = await videoServer.Receive();
                        if (received.bytes[2] == 0 && received.bytes[3] == 0 && received.bytes[4] == 0 && received.bytes[5] == 1)//Wait for first NAL
                        {
                            var nal = (received.bytes[6] & 0x1f);
                            //if (nal != 0x01 && nal!=0x07 && nal != 0x08 && nal != 0x05)
                            //    Console.WriteLine("NAL type:" +nal);
                            started = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Video receive thread error:" + ex.Message);

                        //dont disconnect();
                        //                        break;
                    }
                }
            }, token);


        }

        public static void TakeOff()
        {
            if (connectionState != ConnectionState.Connected) return;

            messageQueue.Enqueue(new Message("takeoff"));
        }

        public static void Land()
        {
            if (connectionState != ConnectionState.Connected) return;

            messageQueue.Enqueue(new Message("land"));
        }
    }
}
