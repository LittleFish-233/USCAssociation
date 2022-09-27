using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USCAssociation.RobotClient.DataModels.Robots;
using USCAssociation.RobotClient.DataRepositories;
using USCAssociation.RobotClient.Tools.Extensions;

namespace USCAssociation.RobotClient.Services.SensitiveWords
{
    public class SensitiveWordService:ISensitiveWordService
    {
        private readonly IRepository<SensitiveWord> _sensitiveWordRepository;

        public SensitiveWordService(IRepository<SensitiveWord> sensitiveWordRepository)
        {
            _sensitiveWordRepository = sensitiveWordRepository;
        }

        public List<string> Check(List<string> texts)
        {
            var items = _sensitiveWordRepository.GetAll().Select(s => s.Name).ToList();
            var words = new List<string>();
            foreach (var item in texts)
            {
                words.AddRange(item.FindStringListInText(items));
            }

            return words;
        }

        public List<string> Check(string text)
        {
            var items = _sensitiveWordRepository.GetAll().Select(s => s.Name).ToList();
            return text.FindStringListInText(items);
        }
    }
}
