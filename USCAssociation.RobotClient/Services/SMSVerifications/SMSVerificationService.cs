using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels;
using USCAssociation.RobotClient.DataModels.SMS;
using USCAssociation.RobotClient.DataRepositories;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.Services.SMSVerifications
{
    public class SMSVerificationService:ISMSVerificationService
    {
        private readonly IRepository<BorrowRecord> _borrowRecordRepository;
        private readonly IRepository<VerificationCode> _verificationCodeRepository;

        public SMSVerificationService(IRepository<BorrowRecord> borrowRecordRepository, IRepository<VerificationCode> verificationCodeRepository)
        {
            _borrowRecordRepository = borrowRecordRepository;
            _verificationCodeRepository = verificationCodeRepository;
        }

        public BorrowRecord GetCurrentBorrower(VerificationCodeType type)
        {
            var model = _borrowRecordRepository.GetAll().FirstOrDefault(s => s.Type == type && s.EndTime > DateTime.Now.ToCstTime());

            return model;
        }

        public bool ReturnAccount(long qq)
        {
            var model = _borrowRecordRepository.GetAll().FirstOrDefault(s => s.QQ == qq&& s.EndTime > DateTime.Now.ToCstTime());
            if(model!=null)
            {
                model.EndTime = DateTime.Now.ToCstTime();
                _borrowRecordRepository.Save();
                return true;
            }

            return false;

        }

        public VerificationCode AddVerificationCode(string content)
        {
            //前置检查
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            if (content.Contains("换绑"))
            {
                return null;
            }

            var model = new VerificationCode();

            //提取验证码
            if (content.Contains("哔哩哔哩"))
            {
                model.Code = content.MidStrEx("验证码", "，");

                model.Type = VerificationCodeType.Bilibili;

            }
            else if (content.Contains("百度帐号"))
            {
                model.Code = content.MidStrEx("验证码：", " 。");

                model.Type = VerificationCodeType.Baidu;
            }
            else if (content.Contains("金山办公"))
            {
                model.Code = content.MidStrEx("验证码", "，");

                model.Type = VerificationCodeType.WPS;
            }
            else
            {
                return null;
            }

            //检查是否存在相同的
            if (_verificationCodeRepository.GetAll().Any(s => s.Type == model.Type && s.Code == model.Code))
            {
                return null;
            }

            model.Time = DateTime.Now.ToCstTime();

            _verificationCodeRepository.Insert(model);

            return model;
        }

        public BorrowRecord AddBorrower(long qq, VerificationCodeType type)
        {
            //是否借阅当前平台
            if (_borrowRecordRepository.GetAll().Any(s => s.QQ == qq && s.Type == type && s.EndTime > DateTime.Now.ToCstTime()))
            {
                throw new Exception($"你已经借出了{type.GetDisplayName()}账号哦~");
            }
            //检查是否借阅了其他平台
            if (_borrowRecordRepository.GetAll().Any(s=>s.QQ==qq&&s.Type!=type&&s.EndTime>DateTime.Now.ToCstTime()))
            {
                throw new Exception("不能同时借出两个账号哦~");
            }
            //检查当前平台是否被借阅
            var temp = _borrowRecordRepository.GetAll().FirstOrDefault(s => s.QQ != qq && s.Type == type && s.EndTime > DateTime.Now.ToCstTime());
            if (temp!=null)
            {
                throw new Exception($"{type.GetDisplayName()}账号已经被别人(QQ:{temp.QQ})借出了哦~");
            }

            //借出
            var model=new BorrowRecord(qq, type);

            return _borrowRecordRepository.Insert(model);

        }
    }
}
