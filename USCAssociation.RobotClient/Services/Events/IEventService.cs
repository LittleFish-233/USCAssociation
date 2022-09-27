using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.Services.Events
{
    public interface IEventService
    {
        string GetCurrentTimeEvent();

        string GetProbabilityEvents();
    }
}
