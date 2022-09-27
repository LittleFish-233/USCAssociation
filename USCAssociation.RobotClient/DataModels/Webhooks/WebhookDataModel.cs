using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.DataModels.Webhooks
{
    public class WebhookRequestModel
    {
        public List<WebhookDataModel> Data { get; set; }=new List<WebhookDataModel>();

        public int Total { get; set; }
    }

    public class WebhookDataModel
    {
        public string Method { get; set; }
        public WebhookDataRequestModel Request { get; set; }=new WebhookDataRequestModel();
    }

    public class WebhookDataRequestModel
    {
        public string Content { get; set; }

        public string Timestamp { get; set; }
    }
}
