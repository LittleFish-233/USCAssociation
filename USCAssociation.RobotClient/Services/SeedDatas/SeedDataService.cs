using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels;
using USCAssociation.RobotClient.DataModels.Robots;
using USCAssociation.RobotClient.DataRepositories;

namespace USCAssociation.RobotClient.Services.SeedDatas
{
    public class SeedDataService:ISeedDataService
    {
        private readonly IRepository<RobotReply> _robotReplyRepository;
        private readonly IRepository<RobotFace> _robotFaceRepository;
        private readonly IRepository<RobotGroup> _robotGroupRepository;

        public SeedDataService(IRepository<RobotGroup> robotGroupRepository, IRepository<RobotFace> robotFaceRepository, IRepository<RobotReply> robotReplyRepository)
        {
            _robotGroupRepository = robotGroupRepository;
            _robotFaceRepository = robotFaceRepository;
            _robotReplyRepository = robotReplyRepository;
        }

        public void InitData()
        {
            if(_robotReplyRepository.GetAll().Any()==false)
            {
                _robotReplyRepository.Insert(new RobotReply("团子", "团子在哦~"));
                _robotReplyRepository.Insert(new RobotReply("借B站账号", "$(bilibili)", RobotReplyRange.Group));
                _robotReplyRepository.Insert(new RobotReply("借(百度网盘|网盘|文库|百度文库)账号", "$(baidu)", RobotReplyRange.Group));
                _robotReplyRepository.Insert(new RobotReply("借(金山|WPS|wps)账号", "$(wps)", RobotReplyRange.Group));
                _robotReplyRepository.Insert(new RobotReply(@"(归还|还)账号", "$(return)"));
                _robotReplyRepository.Insert(new RobotReply(@"([\\s\\S]*)借([\\s\\S]*)(会员|账号)([\\s\\S]*)", "添加团子好友，回复以下内容即可\n“借B站账号”\n“借百度网盘账号”"));
            }
            if (_robotGroupRepository.GetAll().Any() == false)
            {
                _robotGroupRepository.Insert(new RobotGroup(483089593));
            }
        }
    }
}
