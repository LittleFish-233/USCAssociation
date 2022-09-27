using MeowMiraiLib.Msg.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels.Messages;
using USCAssociation.RobotClient.DataModels.Robots;

namespace USCAssociation.RobotClient.Services.Messages
{
    public interface IMessageService
    {
        RobotReply GetAutoReply(string message, RobotReplyRange range);

        Task<SendMessageModel> ProcMessageAsync(RobotReplyRange range, string reply, string message, string regex, long qq, string name);

        Message[] ProcMessageToMirai(string vaule);

    }
}
