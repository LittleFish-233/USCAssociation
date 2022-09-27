using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.DataModels.SMS
{
    public class BorrowRecord
    {
        public long QQ { get; set; }

        public VerificationCodeType Type { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public BorrowRecord() { }

        public BorrowRecord(long qq, VerificationCodeType type) {

            QQ = qq;
            Type = type;

            StartTime = DateTime.Now.ToCstTime();
            EndTime = StartTime.AddHours(type switch
            {
                VerificationCodeType.Bilibili => 12,
                _ => 5
            });
        }
    }
}
