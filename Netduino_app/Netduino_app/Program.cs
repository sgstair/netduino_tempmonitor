using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Netduino_app
{
    public class Program
    {
        public static void Main()
        {
            // Store data for delivery to the webservice
            const int MaxDevices = 32;
            int deviceCount = 0;
            byte[] DeviceSerial = new byte[MaxDevices* 8];
            byte[] DeviceTemperature = new Byte[MaxDevices* 2];
            bool[] DeviceValid = new bool[MaxDevices];

            // Sensor bus
            OneWire oneWire = new OneWire(new OutputPort(Pins.GPIO_PIN_D13, false));

            // Scratch data for interfacing with the sensor.
            byte[] serialNum = new byte[8];
            byte[] scratchPad = new byte[9];

            while(true)
            {
                deviceCount = 0;
                int port = oneWire.FindFirstDevice(true, false);
 
                while(port != 0)
                {
                    // Read the serial number
                    oneWire.SerialNum(serialNum, true);
                    //Debug.Print(port.ToString());
                    //Debug.Print(DumpHex(serialNum));
                    Array.Copy(serialNum, 0, DeviceSerial, deviceCount * 8, 8);
                    DeviceValid[deviceCount] = false;

                    // start temperature conversion
                    oneWire.TouchReset();
                    oneWire.WriteByte(0x55);
                    for (int i = 0; i < 8; i++)
                    {
                        oneWire.WriteByte(serialNum[i]);
                    }
                    oneWire.WriteByte(0x44);

                    // Wait for temperature conversion to be complete
                    // This means the exact time of sampling is going to be skewed by about a second per sensor
                    // (There are other sources of sample time skew too, and I'm not timestamping the data locally to avoid this)
                    // If we were to do the conversions simultaneously that would be more complex and probably increase noise in the system.
                    // For my use case this skew is acceptable.
                    Thread.Sleep(850);


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
                            DeviceValid[deviceCount] = true;
                            Array.Copy(scratchPad, 0, DeviceTemperature, deviceCount * 8, 8);
                            break;
                        }
                    }
                    deviceCount++;


                    port = oneWire.FindNextDevice(true, false);
                }

                // Create web request and submit data to webservice.



                // Wait before sampling again.
                Thread.Sleep(5000);
            }


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
