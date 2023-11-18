using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelloDroneControl
{
    internal class Message
    {
        public string message = string.Empty;
        public string answer = string.Empty;
        public DateTime sentTime;
        public DateTime answerReceivedTime;

        public Message(string _message) {
            message = _message;
            sentTime = DateTime.Now;
        }
    }
}
