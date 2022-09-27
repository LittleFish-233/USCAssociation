using MeowMiraiLib.Msg.Sender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels.Messages;
using USCAssociation.RobotClient.DataModels.Robots;

namespace USCAssociation.RobotClient.Services.QQClients
{
    public interface IQQClientService
    {
        MeowMiraiLib.Client GetMiraiClient();

        Task SendMessage(SendMessageModel model);

        Task ReplyFromGroupAsync(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e);

        Task ReplyFromFriendAsync(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e);

        Task SendMessage(RobotReplyRange range, long id, string text);
    }
}
