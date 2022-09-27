using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.DataModels
{
    public class RobotFace
    {
        public long Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public string Note { get; set; }

        public bool IsHidden { get; set; }
    }
}
