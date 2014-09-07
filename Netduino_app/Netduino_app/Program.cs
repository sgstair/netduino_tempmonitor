using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Netduino_app
{
    public class Program
    {
        const string TargetWebServer = "192.168.1.17";

        public static void Main()
        {
            // Light the LED when doing stuff and when something goes wrong.
            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);

            // Optionally configure networking (specific to Netduino plus 2)
            Debug.Print("Configuring network...");
            Thread.Sleep(500);
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            ni.EnableDynamicDns();
            ni.EnableDhcp();
            Thread.Sleep(500);

            int init = 10;
            while(true)
            {
                led.Write(!led.Read());
                Thread.Sleep(500);
                ni = NetworkInterface.GetAllNetworkInterfaces()[0];

                if (ni.IPAddress.Substring(0, 7) == "192.168")
                    break;

                init++;
                if(init>20)
                {
                    init = 0;
                    //ni.EnableDynamicDns();
                    //ni.EnableDhcp();
                }
            }

            Debug.Print("Network configured.");

            



            // Store data for delivery to the webservice
            const int MaxDevices = 32;
            int deviceCount = 0;
            byte[] DeviceSerial = new byte[MaxDevices* 8];
            byte[] DeviceTemperature = new byte[MaxDevices* 2];
            byte[] DeviceValid = new byte[MaxDevices];

            // Sensor bus
            OneWire oneWire = new OneWire(new OutputPort(Pins.GPIO_PIN_D13, false));

            // Scratch data for interfacing with the sensor.
            byte[] serialNum = new byte[8];
            byte[] scratchPad = new byte[9];

            while(true)
            {
                Debug.Print("Starting polling loop.");
                deviceCount = 0;
                led.Write(true);
                int port = oneWire.FindFirstDevice(true, false);
 
                while(port != 0)
                {
                    // Read the serial number
                    led.Write(true);

                    oneWire.SerialNum(serialNum, true);
                    //Debug.Print(port.ToString());
                    //Debug.Print(DumpHex(serialNum));
                    Array.Copy(serialNum, 0, DeviceSerial, deviceCount * 8, 8);
                    DeviceValid[deviceCount] = 0;

                    // start temperature conversion
                    oneWire.TouchReset();
                    oneWire.WriteByte(0x55);
                    for (int i = 0; i < 8; i++)
                    {
                        oneWire.WriteByte(serialNum[i]);
                    }
                    oneWire.WriteByte(0x44);
                    led.Write(false);

                    // Wait for temperature conversion to be complete
                    // This means the exact time of sampling is going to be skewed by about a second per sensor
                    // (There are other sources of sample time skew too, and I'm not timestamping the data locally to avoid this)
                    // If we were to do the conversions simultaneously that would be more complex and probably increase noise in the system.
                    // For my use case this skew is acceptable.
                    Thread.Sleep(850);

                    led.Write(true);
                    // Attempt read 3 times.
                    for (int retry = 0; retry < 3; retry++)
                    {
                        oneWire.TouchReset();
                        oneWire.WriteByte(0x55);
                        for (int i = 0; i < 8; i++)
                        {
                            oneWire.WriteByte(serialNum[i]);
                        }
                        oneWire.WriteByte(0xBE);
                        for (int i = 0; i < 9; i++)
                        {
                            scratchPad[i] = (byte)oneWire.ReadByte();
                        }

                        //Debug.Print(DumpHex(scratchPad));
                        byte crc = ComputeCrc(scratchPad, 8);
                        //Debug.Print(crc.ToString("x2"));

                        // If read is valid, store value and stop trying to read.
                        if (crc == scratchPad[8])
                        {
                            DeviceValid[deviceCount] = 1;
                            Array.Copy(scratchPad, 0, DeviceTemperature, deviceCount * 2, 2);
                            break;
                        }
                    }
                    deviceCount++;


                    port = oneWire.FindNextDevice(true, false);
                }
                led.Write(false);

                Debug.Print("Got device data. Num Devices = " + deviceCount.ToString());

                // Create web request and submit data to webservice.
                // Call using a url like:
                // http://localhost:2542/TemperatureData/Submit?Names={SENSORNAMES}&Values={SENSORVALUES}&Valid={SENSORVALID} 
                StringBuilder sb = new StringBuilder(1024);

                sb.Append("http://");
                sb.Append(TargetWebServer);
                sb.Append(":2542");
                                
                sb.Append("/TemperatureData/Submit?Names=");
                sb.Append(Base64Encode(DeviceSerial, deviceCount * 8));
                sb.Append("&Values=");
                sb.Append(Base64Encode(DeviceTemperature, deviceCount * 2));
                sb.Append("&Valid=");
                sb.Append(Base64Encode(DeviceValid, deviceCount));

                // Need to replace a few characters in the base64 output that are causing problems.
                sb.Replace("+", "%2b");

                string requestTarget = sb.ToString();

                Debug.Print("Sending WebService request: " + requestTarget);

                led.Write(true);

                try
                {
                    // If the target is ever unavailable, this code just gets completely stuck
                    // Hopefully in the future this can be worked around.



                    WebRequest wr = HttpWebRequest.Create(requestTarget);
                    wr.Method = "POST";
                    wr.Timeout = 10000;
                    wr.ContentLength = 0;


                    
                    HttpWebResponse response = (HttpWebResponse)wr.GetResponse();
                    led.Write(false);

                    // Signal on error.
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        // Something went wrong.
                        led.Write(true);

                        Debug.Print("Error condition: " + response.StatusCode.ToString() + " " + response.StatusDescription);
                        Debug.Print(response.ContentLength.ToString());
                        Stream stream = response.GetResponseStream();
                        while(stream.CanRead)
                        {
                            byte[] data = new byte[100];
                            sb.Clear();
                            int len = stream.Read(data, 0, 100);
                            if (len == 0) break;
                            for(int i=0;i<len;i++)
                            {
                                sb.Append((char)data[i]);
                            }
                            Debug.Print(sb.ToString());
                        }
                        stream.Close();

                    }
                    response.Close();


                }
                catch (Exception ex)
                {
                    Debug.Print("Unexpected Exception " + ex.ToString());
                }

                // Wait before sampling again.
                Debug.Print("Delay before next loop...");
                Thread.Sleep(25000);

            }


        }

        static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
            ni.EnableDynamicDns();
            ni.EnableDhcp();
        }

        public static string Base64Encode(byte[] data, int dataLength)
        {
            const string base64lut = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
            int length = ((dataLength+2)/3)*4;
            StringBuilder sb = new StringBuilder(length);
            int loc = 0;
            while (loc < dataLength)
            {
                int len = 3;
                if ((dataLength - loc) < len) { len = dataLength - loc; }
                int a, b, c;
                a = b = c = 0;
                a = data[loc];
                if (len >= 2) b = data[loc + 1];
                if (len >= 3) c = data[loc + 2];

                int ob;

                ob = a >> 2;
                sb.Append(base64lut[ob]);

                ob = ((a << 4) | (b >> 4)) & 0x3F;
                sb.Append(base64lut[ob]);

                if(len == 1)
                {
                    sb.Append("=");
                }
                else
                {
                    ob = ((b << 2) | (c >> 6)) & 0x3F;
                    sb.Append(base64lut[ob]);
                }

                if(len<3)
                {
                    sb.Append("=");
                }
                else
                {
                    ob = c & 0x3F;
                    sb.Append(base64lut[ob]);
                }

                loc += 3;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Debugging function to convert a byte array to a hex string.
        /// </summary>
        public static string DumpHex(byte[] data)
        {
            string[] strings = new string[data.Length];
            for(int i=0;i<data.Length;i++)
            {
                strings[i] = data[i].ToString("x2");
            }
            return string.Concat(strings);
        }

        /// <summary>
        /// DS18B20 CRC function (8-bit CRC, LSB first, initial vector zero, X^8 + X^5 + X^4 + 1 = 0x(topbit)31, reversed to 0x8C(topbit) )
        /// </summary>
        public static byte ComputeCrc(byte[] data, int length)
        {
            const byte crc = 0x8C;
            byte output = 0;
            byte temp;
            for(int i=0;i<length;i++)
            {
                temp = data[i];
                for (int n = 0; n < 8; n++)
                {
                    if (((temp ^ output) & 1) == 1)
                    {
                        output = (byte)((output >> 1) ^ crc);
                    }
                    else
                    {
                        output = (byte)(output >> 1);
                    }
                    temp = (byte)(temp >> 1);
                }
            }
            return output;
        }

    }
}
