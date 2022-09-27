using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.DataModels.Messages
{
    public class EventExecuteInfor
    {
        public long Id { get; set; }

        public string Note { get; set; }

        public bool RealExecute { get; set; }

        public DateTime LastRunTime { get; set; }
    }
}
