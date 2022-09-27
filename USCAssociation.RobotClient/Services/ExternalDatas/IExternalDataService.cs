using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.Services.ExternalDatas
{
    public interface IExternalDataService
    {
        Task<string> GetWeather();
    }
}
