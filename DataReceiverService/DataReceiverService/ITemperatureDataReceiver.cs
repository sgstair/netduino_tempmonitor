using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{

    [ServiceContract]
    public interface ITemperatureDataReceiver
    {

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "Submit?Names={SensorNames}&Values={SensorValues}&Valid={SensorValid}")]
        void SubmitRawData(byte[] SensorNames, byte[] SensorValues, byte[] SensorValid);
    }
}
