using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Interfaces
{
    public interface IMessagingServiceFactory
    {
        IMessagingService CreateMessagingService();
    }
}