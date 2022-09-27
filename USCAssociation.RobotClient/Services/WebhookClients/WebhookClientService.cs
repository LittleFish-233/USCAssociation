using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using USCAssociation.RobotClient.DataModels.Messages;
using USCAssociation.RobotClient.DataModels.Webhooks;
using USCAssociation.RobotClient.Services.Messages;
using USCAssociation.RobotClient.Services.QQClients;
using USCAssociation.RobotClient.Services.SMSVerifications;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.Services.WebhookClients
{
    public class WebhookClientService : IWebhookClientService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ISMSVerificationService _SMSVerificationService;
        private readonly IQQClientService _qqClientService;
        private readonly ILogger<WebhookClientService> _logger;

        public WebhookClientService(HttpClient httpClient, IConfiguration configuration, ISMSVerificationService SMSVerificationService, IQQClientService qqClientService, ILogger<WebhookClientService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _SMSVerificationService = SMSVerificationService;
            _qqClientService = qqClientService;
            _logger = logger;

            Init();
        }

        private System.Timers.Timer t = new(1000 * 5);


        public void Init()
        {
            //定时任务计时器
            t.Start(); //启动计时器
            t.Elapsed += async (s, e) =>
            {
                try
                {
                    WebhookRequestModel model = await _httpClient.GetFromJsonAsync<WebhookRequestModel>(_configuration["WebhookUrl"]);
                    WebhookDataModel request = model.Data.Where(s => s.Method == "POST").MaxBy(s => s.Request.Timestamp);

                    string content = request?.Request?.Content;

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return;
                    }

                    DataModels.SMS.VerificationCode code = _SMSVerificationService.AddVerificationCode(content);

                    if (code == null)
                    {
                        return;
                    }

                    var borrower = _SMSVerificationService.GetCurrentBorrower(code.Type);
                    if (borrower == null)
                    {
                        return;
                    }

                    await _qqClientService.SendMessage(DataModels.Robots.RobotReplyRange.Friend, borrower.QQ, $"【{code.Type.GetDisplayName()}】登入验证码：{code.Code}");
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "获取验证码失败");
                }

            };

        }

        public void Dispose()
        {
            if (t != null)
            {
                t.Dispose();
                t = null;
            }
        }
    }
}
