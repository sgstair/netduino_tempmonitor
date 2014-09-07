using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single, ConcurrencyMode=ConcurrencyMode.Single)]
    public class TemperatureDataReceiver : ITemperatureDataReceiver
    {
        TempMonitorSql db;
        public TemperatureDataReceiver()
        {
            db = new TempMonitorSql();
        }

        // Call this using a url like:
        // http://localhost:2542/TemperatureData/Submit?Names={SENSORNAMES}&Values={SENSORVALUES}&Valid={SENSORVALID} 
        // Where the {} values are base64 encoded byte arrays.
        // This function takes 8 bytes per sensor name, 2 bytes per sensor value, and 1 byte per sensor valid.
        public void SubmitRawData(byte[] SensorNames, byte[] SensorValues, byte[] SensorValid)
        {
            // Debugging stuff, disable when done building service.
            System.Diagnostics.Debug.WriteLine("SubmitRawData {0}", DateTime.Now);
            System.Diagnostics.Debug.WriteLine("SensorNames " + string.Join(" ", SensorNames.Select(b => b.ToString("x2"))));
            System.Diagnostics.Debug.WriteLine("SensorValues " + string.Join(" ", SensorValues.Select(b => b.ToString("x2"))));
            System.Diagnostics.Debug.WriteLine("SensorValid " + string.Join(" ", SensorValid.Select(b => b.ToString())));

            int count = SensorValid.Length;
            DateTime timeStamp = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                TemperatureData d = new TemperatureData();
                d.Time = timeStamp;
                d.Device = new byte[8];
                Array.Copy(SensorNames, i * 8, d.Device, 0, 8);

                int value = SensorValues[i * 2] + SensorValues[i * 2 + 1] * 256;
                if (value >= 0x800) value -= 0x1000;

                double? realValue = value / 16.0;

                if(SensorValid[i] == 0)
                {
                    realValue = null;
                }
                d.Value = realValue;

                db.ExecuteCommand("insert into Temperature (Time, Device, Value) values ({0}, {1}, {2});", d.Time, d.Device, d.Value);

            }
            System.Diagnostics.Debug.WriteLine("Completed {0}", DateTime.Now);

        }
    }
}
