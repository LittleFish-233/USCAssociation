using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.DataModels.SMS
{
    public class VerificationCode
    {
        public string Code { get; set; }

        public DateTime Time { get; set; }

        public VerificationCodeType Type { get; set; }
    }

    public enum VerificationCodeType
    {
        [Display(Name ="B站")]
        Bilibili,
        [Display(Name = "百度网盘")]
        Baidu,
        [Display(Name = "金山办公")]
        WPS,

    }
}
