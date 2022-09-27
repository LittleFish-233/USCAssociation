using MeowMiraiLib.Msg.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels.Robots;

namespace USCAssociation.RobotClient.DataModels.Messages
{

    public class SendMessageModel
    {
        public RobotReplyRange Range { get; set; }

        public long SendTo { get; set; }

        public Message[] MiraiMessage { get; set; } = Array.Empty<Message>();
    }
}
