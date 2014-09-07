using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single, ConcurrencyMode=ConcurrencyMode.Single)]
    public class TemperatureDataReceiver : ITemperatureDataReceiver
    {

        // Call this using a url like:
        // http://localhost:2542/TemperatureData/Submit?Names={SENSORNAMES}&Values={SENSORVALUES}&Valid={SENSORVALID} 
        // Where the {} values are base64 encoded byte arrays.
        // This function takes 8 bytes per sensor name, 2 bytes per sensor value, and 1 byte per sensor valid.
        public void SubmitRawData(byte[] SensorNames, byte[] SensorValues, byte[] SensorValid)
        {
            // Debugging stuff, disable when done building service.
            System.Diagnostics.Debug.WriteLine("SubmitRawData");
            System.Diagnostics.Debug.WriteLine("SensorNames " + string.Join(" ", SensorNames.Select(b => b.ToString("x2"))));
            System.Diagnostics.Debug.WriteLine("SensorValues " + string.Join(" ", SensorValues.Select(b => b.ToString("x2"))));
            System.Diagnostics.Debug.WriteLine("SensorValid " + string.Join(" ", SensorValid.Select(b => b.ToString())));



        }
    }
}
