using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TelloDroneControl
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        string lastState = "";

        public MainPage()
        {
            InitializeComponent();


        }


        private void OnConnectClicked(object sender, EventArgs e)
        {

            Drone.StartConnecting(WriteNewLog);//Start trying to connect.

            
        }
        private void OnDisconnectClicked(object sender, EventArgs e)
        {
            Drone.Disconnect();
        }
        private void OnTakeOffClicked(object sender, EventArgs e)
        {
            Drone.TakeOff();
        }
        private void OnLandClicked(object sender, EventArgs e)
        {
            Drone.Land();
        }

        private void WriteNewLog(string log)
        {
            var myLabel = new Label
            {
                Text = count+": "+ log,
                // Set other properties as needed
                FontSize = 20,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            count++;

            Dispatcher.Dispatch(() =>
            {
                // Add the label to the stack layout
                logStackLayout.Children.Insert(0, myLabel);
            });
            
        }

    }



}
