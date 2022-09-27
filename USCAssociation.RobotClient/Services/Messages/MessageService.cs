using MeowMiraiLib.Msg.Type;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using USCAssociation.RobotClient.DataModels;
using USCAssociation.RobotClient.DataModels.Messages;
using USCAssociation.RobotClient.DataModels.Robots;
using USCAssociation.RobotClient.DataModels.SMS;
using USCAssociation.RobotClient.DataRepositories;
using USCAssociation.RobotClient.Services.SensitiveWords;
using USCAssociation.RobotClient.Services.SMSVerifications;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly IRepository<RobotReply> _robotReplyRepository;
        private readonly IRepository<RobotFace> _robotFaceRepository;
        private readonly ISensitiveWordService _sensitiveWordService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageService> _logger;
        private readonly ISMSVerificationService _SMSVerificationService;

        public MessageService(IRepository<RobotReply> robotReplyRepository, IRepository<RobotFace> robotFaceRepository,
            ILogger<MessageService> logger, ISMSVerificationService SMSVerificationService,
        IConfiguration configuration,
            ISensitiveWordService sensitiveWordService)
        {
            _robotReplyRepository = robotReplyRepository;
            _sensitiveWordService = sensitiveWordService;
            _logger = logger;
            _configuration = configuration;
            _robotFaceRepository = robotFaceRepository;
            _SMSVerificationService = SMSVerificationService;
        }

        /// <summary>
        /// 获取可能的回复
        /// </summary>
        /// <param name="message"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public RobotReply GetAutoReply(string message, RobotReplyRange range)
        {

            DateTime now = DateTime.Now.ToCstTime();
            List<IGrouping<int, RobotReply>> replies = _robotReplyRepository.GetAll().Where(s => s.IsHidden == false && (s.Range == RobotReplyRange.All || s.Range == range) && now.TimeOfDay <= s.BeforeTime.TimeOfDay && now.TimeOfDay >= s.AfterTime.TimeOfDay && Regex.IsMatch(message, s.Key))
                .GroupBy(s => s.Priority)
                .OrderByDescending(s => s.Key)
                .ToList();

            if (replies.Count == 0)
            {
                return null;
            }

            int index = new Random().Next(0, replies.FirstOrDefault().Count());
            RobotReply reply = replies.FirstOrDefault().ToList()[index];

            //检查是否含有变量替换 如果有 则检查输入是否包含敏感词
            if (reply.Value.Contains('$'))
            {
                List<string> words = _sensitiveWordService.Check(message);

                if (words.Count != 0)
                {
                    return new RobotReply(message, _configuration["SensitiveReply"] ?? "团子不知道哦~");
                }
            }

            return reply;
        }

        /// <summary>
        /// 处理消息回复
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="message"></param>
        /// <param name="regex"></param>
        /// <param name="qq"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgError"></exception>
        public async Task<SendMessageModel> ProcMessageAsync(RobotReplyRange range, string reply, string message, string regex, long qq, string name)
        {

            List<KeyValuePair<string, string>> args = new();
            try
            {
                await ProcMessageArgument(reply, message, qq, name, args);
                ProcMessageReplaceInput(reply, message, regex, args);
                ProcMessageFace(reply, args);
            }
            catch (ArgError arg)
            {
                reply = arg.Error;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取变量值失败");
                reply = "呜呜呜~";
            }


            //检测敏感词
            List<string> words = _sensitiveWordService.Check(args.Where(s => s.Key == "sender" || (s.Key.Contains('[') && s.Key.Contains(']'))).Select(s => s.Value).ToList());

            if (words.Count != 0)
            {
                string msg = $"对{name}({qq})的消息回复中包含敏感词\n消息：{message}\n回复：{reply}\n\n参数替换列表：\n";
                foreach (KeyValuePair<string, string> item in args)
                {
                    msg += $"{item.Key} -> {item.Value}\n";

                }
                msg += $"\n触发的敏感词：\n";
                foreach (string item in words)
                {
                    msg += $"{item}\n";
                }

                _logger.LogError(msg);

                throw new ArgError(msg);
            }

            //替换参数
            foreach (KeyValuePair<string, string> item in args)
            {
                reply = reply.Replace(item.Key, item.Value);
            }

            return range == RobotReplyRange.Channel
                ? throw new InvalidOperationException("暂不支持QQ频道")
                : new SendMessageModel
                {
                    MiraiMessage = ProcMessageToMirai(reply),
                    Range = range
                };
        }

        /// <summary>
        /// 将纯文本回复转换成可发送的消息数组
        /// </summary>
        /// <param name="vaule"></param>
        /// <returns></returns>
        public Message[] ProcMessageToMirai(string vaule)
        {
            if (string.IsNullOrWhiteSpace(vaule))
            {
                return null;
            }

            List<Message> messages = new();

            while (true)
            {
                if (vaule.Contains("[image="))
                {
                    string imageStr = vaule.MidStrEx("[image=", "]");

                    if (string.IsNullOrWhiteSpace(imageStr) == false)
                    {
                        vaule = vaule.Replace("[image=" + imageStr + "]", "");
                        //修正一部分图片链接缺省协议
                        if (imageStr.Contains("http") == false)
                        {
                            imageStr = "https:" + imageStr;
                        }
                        messages.Add(new Image(url: imageStr.Replace("http://image.cngal.org/", "https://image.cngal.org/")));
                    }
                }
                else if (vaule.Contains("[声音="))
                {
                    string voiceStr = vaule.MidStrEx("[声音=", "]");

                    if (string.IsNullOrWhiteSpace(voiceStr) == false)
                    {
                        vaule = vaule.Replace("[声音=" + voiceStr + "]", "");
                        messages.Add(new Voice(url: voiceStr.Replace("http://res.cngal.org/", "https://res.cngal.org/")));

                    }
                }

                else if (vaule.Contains("[@"))
                {
                    string idStr = vaule.MidStrEx("[@", "]");
                    if (long.TryParse(idStr, out long id))
                    {

                        vaule = vaule.Replace("[@" + idStr + "]", "");
                        messages.Add(new At(id, idStr));
                    }
                }
                else
                {
                    break;
                }


            }


            if (string.IsNullOrWhiteSpace(vaule) == false)
            {
                messages.Add(new Plain(vaule));
            }

            return string.IsNullOrWhiteSpace(vaule) && messages.Count == 0 ? null : messages.ToArray();
        }

        /// <summary>
        /// 执行参数替换
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="message"></param>
        /// <param name="regex"></param>
        /// <param name="args"></param>
        public void ProcMessageReplaceInput(string reply, string message, string regex, List<KeyValuePair<string, string>> args)
        {
            if (string.IsNullOrWhiteSpace(regex))
            {
                return;
            }



            List<string> splits = Regex.Split(message, regex).Where(s => string.IsNullOrWhiteSpace(s) == false).ToList();


            for (int i = 0; i < splits.Count; i++)
            {
                if (reply.Contains($"[{i + 1}]"))
                {
                    args.Add(new KeyValuePair<string, string>($"[{i + 1}]", splits[i].ToString()));
                }
            }
        }

        /// <summary>
        /// 获取参数替换列表
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="message"></param>
        /// <param name="qq"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task ProcMessageArgument(string reply, string message, long qq, string name, List<KeyValuePair<string, string>> args)
        {
            while (true)
            {
                string argument = reply.MidStrEx("$(", ")");

                if (string.IsNullOrWhiteSpace(argument))
                {
                    break;
                }

                string value = argument switch
                {
                    "time" => DateTime.Now.ToCstTime().ToString("HH:mm"),
                    "qq" => qq.ToString(),
                    "weather" => _configuration["weather"],
                    "sender" => name,
                    "n" => "\n",
                    "r" => "\r",
                    "facelist" => "该功能暂未实装",
                    _ => await GetArgValue(argument, message, qq)
                };

                reply = reply.Replace("$(" + argument + ")", value);

                args.Add(new KeyValuePair<string, string>("$(" + argument + ")", value));
            }
        }

        /// <summary>
        /// 处理表情
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="args"></param>
        public void ProcMessageFace(string reply, List<KeyValuePair<string, string>> args)
        {
            if (string.IsNullOrWhiteSpace(reply))
            {
                return;
            }

            foreach (RobotFace item in _robotFaceRepository.GetAll().Where(s => s.IsHidden == false))
            {
                if (reply.Contains($"[{item.Key}]"))
                {
                    args.Add(new KeyValuePair<string, string>($"[{item.Key}]", item.Value));
                }
            }

            for (int i = 1; i < 4; i++)
            {
                if (reply.Contains($"[{i}]") && args.Any(s => s.Key == $"[{i}]") == false)
                {
                    args.Add(new KeyValuePair<string, string>($"[{i}]", ""));
                }
            }
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="infor"></param>
        /// <param name="qq"></param>
        /// <returns></returns>
        public async Task<string> GetArgValue(string name, string infor, long qq)
        {
            return await GetArgValue(name, infor, qq, new Dictionary<string, string>());
        }

        public async Task<string> GetArgValue(string name, string infor, long qq, Dictionary<string, string> adds)
        {

            if (name == "bilibili" || name == "baidu" || name == "wps")
            {
                VerificationCodeType type = name switch
                {
                    "baidu" => VerificationCodeType.Baidu,
                    "bilibili" => VerificationCodeType.Bilibili,
                    "wps" => VerificationCodeType.WPS,
                    _ => VerificationCodeType.Baidu
                };

                //借出账号
                try
                {
                    var borrower= _SMSVerificationService.AddBorrower(qq, type);
                    return $"成功借出{type.GetDisplayName()}账号，添加团子好友后团子才能告诉你验证码哦~\n记得在{borrower.EndTime:M月dd日H点mm分}前归还账号哦~\n回复“归还账号”即可~";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }
            else if (name == "return")
            {
               if( _SMSVerificationService.ReturnAccount(qq))
                {
                    return "成功归还账号ヾ(≧▽≦*)o";
                }
            }
                return null;
        }
    }
}
