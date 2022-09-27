using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels.SMS;

namespace USCAssociation.RobotClient.Services.SMSVerifications
{
    public interface ISMSVerificationService
    {
        BorrowRecord GetCurrentBorrower(VerificationCodeType type);

        VerificationCode AddVerificationCode(string content);

        BorrowRecord AddBorrower(long qq, VerificationCodeType type);

        bool ReturnAccount(long qq);
    }
}
