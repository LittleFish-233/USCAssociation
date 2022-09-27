using MeowMiraiLib;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using USCAssociation.RobotClient.DataModels.Messages;
using USCAssociation.RobotClient.DataModels.Robots;
using USCAssociation.RobotClient.DataRepositories;
using USCAssociation.RobotClient.Services.Events;
using USCAssociation.RobotClient.Services.Messages;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.Services.QQClients
{
    public class QQClientService : IQQClientService,IDisposable
    {
        private readonly IRepository<RobotGroup> _robotGroupRepository;
        private readonly IRepository<PostLog> _postLogRepository;
        private readonly IConfiguration _configuration;
        private readonly IMessageService _messageService;
        private readonly IEventService _eventService;
        private readonly ILogger<QQClientService> _logger;
        private readonly Client _client;

        System.Timers.Timer t= new(1000 * 60);
        System.Timers.Timer t2 = new(1000 * 60);

        public QQClientService(IRepository<RobotGroup> robotGroupRepository, IRepository<PostLog> postLogRepository,
            IConfiguration configuration, ILogger<QQClientService> logger, IEventService eventService,
        IMessageService messageService)
        {
            _robotGroupRepository = robotGroupRepository;
            _configuration = configuration;
            _messageService = messageService;
            _postLogRepository = postLogRepository;
            _logger= logger;
            _eventService = eventService;

            var url = $"ws://{_configuration["MiraiUrl"]}/all?verifyKey={_configuration["NormalVerifyKey"]}&qq={_configuration["QQ"]}";
            _client = new Client(url);

            Init();

        }

        public void Init()
        {
            var c = GetMiraiClient();

            _logger.LogInformation("成功初始化 Mirai 客户端");

            _ = c.ConnectAsync();

            //定时任务计时器
            t.Start(); //启动计时器
            t.Elapsed += async (s, e) =>
            {
                var message = _eventService.GetCurrentTimeEvent();
                if (string.IsNullOrWhiteSpace(message) == false)
                {
                    var result = await _messageService.ProcMessageAsync(RobotReplyRange.Group, message, "", null, 0, null);

                    if (result != null)
                    {
                        foreach (var item in _robotGroupRepository.GetAll().Where(s => s.IsHidden == false && s.ForceMatch == false))
                        {
                            result.SendTo = item.GroupId;
                            await SendMessage(result);
                        }
                    }
                }
            };

            //随机任务计时器
            t2.Start(); //启动计时器
            t2.Elapsed += async (s, e) =>
            {
                var message = _eventService.GetProbabilityEvents();
                if (string.IsNullOrWhiteSpace(message) == false)
                {
                    var result = await _messageService.ProcMessageAsync(RobotReplyRange.Group, message, "", null, 0, null);

                    if (result != null)
                    {
                        foreach (var item in _robotGroupRepository.GetAll().Where(s => s.IsHidden == false && s.ForceMatch == false))
                        {
                            result.SendTo = item.GroupId;
                            await SendMessage(result);
                        }
                    }
                }
            };

            //好友消息事件
            c.OnFriendMessageReceive += async (s, e) =>
            {
                try
                {
                    await ReplyFromFriendAsync(s, e);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "无法回复好友消息");
                }

            };


            //群聊消息事件
            c.OnGroupMessageReceive += async (s, e) =>
            {
                try
                {
                    await ReplyFromGroupAsync(s, e);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "无法回复群聊消息");
                }

            };
        }

        /// <summary>
        /// 回复好友消息
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public async Task ReplyFromFriendAsync(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            await ReplyMessageAsync(RobotReplyRange.Friend, ConversionMeaasge(e), s.id, s.id, s.nickname);
        }

        public async Task ReplyFromGroupAsync(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            //忽略未关注的群聊
            RobotGroup group = _robotGroupRepository.GetAll().FirstOrDefault(x => x.GroupId == s.group.id && x.IsHidden == false);
            if (group == null)
            {
                return;
            }
            //检查是否符合强制匹配
            string message = ConversionMeaasge(e);
            if (group.ForceMatch)
            {
                string name = _configuration["RobotName"] ?? "团子";
                if ((name != null && message.Contains(name) == false) || message.Contains("介绍") == false)
                {
                    return;
                }
            }
            await ReplyMessageAsync(RobotReplyRange.Group, message, s.group.id, s.id, s.memberName);
        }

        private string ConversionMeaasge(MeowMiraiLib.Msg.Type.Message[] e)
        {
            string message = e.MGetPlainString();
            Message at = e.FirstOrDefault(s => s.type == "At");
            if (at != null)
            {
                long atTarget = (at as At).target;
                message += "[@" + atTarget.ToString() + "]";
            }
            return message;
        }

        /// <summary>
        /// 回复消息
        /// </summary>
        private async Task ReplyMessageAsync(RobotReplyRange range, string message, long sendto, long memberId, string memberName)
        {
            //尝试找出所有匹配的回复
            RobotReply reply = _messageService.GetAutoReply(message, range);

            if (reply == null)
            {
                return;
            }
            SendMessageModel result = new();


            try
            {
                //处理消息
                result = await _messageService.ProcMessageAsync(range, reply.Value, message, reply.Key, memberId, memberName);
            }
            catch (ArgError ae)
            {
                if (long.TryParse(_configuration["WarningQQGroup"], out long warningQQGroup))
                {
                    //发送警告
                    await SendMessage(new SendMessageModel { SendTo = warningQQGroup, MiraiMessage = new MeowMiraiLib.Msg.Type.Message[] { new Plain(ae.Error) }, Range = RobotReplyRange.Group });

                }
            }


            if (result != null)
            {
                //检查上限
                if (await CheckLimit(range, memberId, memberName) == false)
                {
                    //发送消息
                    result.SendTo = sendto;
                    await SendMessage(result);
                }
            }
            //添加发送记录
            _ = _postLogRepository.Insert(new PostLog
            {
                Message = message,
                PostTime = DateTime.Now.ToCstTime(),
                QQ = memberId,
                Reply = reply.Value
            });
        }

        /// <summary>
        /// 检查是否超过限制
        /// </summary>
        /// <param name="sendto"></param>
        /// <param name="memberId"></param>
        /// <param name="memberName"></param>
        /// <returns>是否超过限制</returns>
        private async Task<bool> CheckLimit(RobotReplyRange range, long memberId, string memberName)
        {
            //判断该用户是否连续10次互动 1分钟内
            int singleCount = _postLogRepository.GetAll().Count(x => (DateTime.Now.ToCstTime() - x.PostTime).TotalMinutes <= 1 && x.QQ == memberId);
            int totalCount = _postLogRepository.GetAll().Count(x => (DateTime.Now.ToCstTime() - x.PostTime).TotalMinutes <= 1);

            //读取上限次数配置
            if (!long.TryParse(_configuration["SingleLimit"], out long singleLimit))
            {
                singleLimit = 5;
            }
            if (!long.TryParse(_configuration["TotalLimit"], out long totalLimit))
            {
                totalLimit = 10;
            }

            //检查上限
            if (singleCount == singleLimit)
            {
                SendMessageModel result = await _messageService.ProcMessageAsync(range, $"[黑化微笑][@{memberId}]如果恶意骚扰人家的话，我会请你离开哦…", null, null, memberId, memberName);
                await SendMessage(result);
                return true;
            }
            else if (singleCount > singleLimit)
            {
                return true;
            }

            if (singleCount == totalLimit)
            {
                SendMessageModel result = await _messageService.ProcMessageAsync(range, $"核心温度过高，正在冷却......", null, null, memberId, memberName);
                await SendMessage(result);
                return true;
            }
            else if (singleCount > totalLimit)
            {
                return true;
            }

            return false;
        }

        public async Task SendMessage(RobotReplyRange range, long id,string text)
        {
            await SendMessage(new SendMessageModel
            {
                SendTo = id,
                MiraiMessage = _messageService.ProcMessageToMirai(text),
                Range = range,
            });
        }

        public Task SendMessage(SendMessageModel model)
        {

            if (model.MiraiMessage == null)
            {
                return Task.CompletedTask;
            }

            MeowMiraiLib.Client client = GetMiraiClient();

            if (model.Range == RobotReplyRange.Channel)
            {
                throw new InvalidOperationException("暂不支持QQ频道");
            }
            else if (model.Range == RobotReplyRange.Friend)
            {

                (bool isTimedOut, Newtonsoft.Json.Linq.JObject Return) j = model.MiraiMessage.SendToFriend(model.SendTo, client);
                Console.WriteLine(j);
            }
            else
            {
                (bool isTimedOut, Newtonsoft.Json.Linq.JObject Return) j = model.MiraiMessage.SendToGroup(model.SendTo, client);
                Console.WriteLine(j);
            }

            return Task.CompletedTask;
        }

        public Client GetMiraiClient()
        {
                return _client;
      
        }

        public void Dispose()
        {
           if(t!=null)
            {
                t.Dispose();
                t = null;
            }
            if (t2 != null)
            {
                t2.Dispose();
                t2 = null;
            }
        }
    }
}
