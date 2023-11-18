using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TelloDroneControl
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private State state = new State();
        private const int bufSize = 8 * 1024;
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        string lastState = "";

        public MainPage()
        {
            InitializeComponent();
            
           //Client("192.168.10.1", 8889);

            //subscribe to Tello connection events
            Tello.onConnection += (Tello.ConnectionState newState) =>
            {
                if (newState == Tello.ConnectionState.Connected)
                {
                    //When connected update maxHeight to 5 meters
                    Tello.setMaxHeight(5);
                }
                //Show connection messages.
                Console.WriteLine("Tello " + newState.ToString());
                lastState = newState.ToString();
            };

            //subscribe to Tello update events. Called when update data arrives from drone.
            Tello.onUpdate += (int cmdId) =>
            {
                if (cmdId == 86)//ac update
                    Console.WriteLine("FlyMode:" + Tello.state.flyMode + " Height:" + Tello.state.height);
            };

            Tello.startConnecting();//Start trying to connect.

            var str = "";
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;


            CounterBtn.Text = lastState;

            SemanticScreenReader.Announce(CounterBtn.Text);
            /*

            var client = new UdpClient();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.10.1"), 8889); // endpoint where server is listening (testing localy)
            
            
            client.Connect(ep);

            byte[] bytes = Encoding.ASCII.GetBytes("“command”");
            

            // send data
            client.Send(bytes, bytes.Length);

            // then receive data
            var receivedData = client.Receive(ref ep);  // Exception: An existing connection was forcibly closed by the remote host

            string someString = Encoding.ASCII.GetString(receivedData);

            CounterBtn.Text = someString;

            SemanticScreenReader.Announce(CounterBtn.Text);*/
        }

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Client(string address, int port)
        {
            socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndSend(ar);
                Console.WriteLine("SEND: {0}, {1}", bytes, text);
            }, state);
        }

        private void Receive()
        {
            socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = socket.EndReceiveFrom(ar, ref epFrom);
                socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));

                CounterBtn.Text = Encoding.ASCII.GetString(so.buffer, 0, bytes);

                SemanticScreenReader.Announce(CounterBtn.Text);
            }, state);
        }
    }



}
