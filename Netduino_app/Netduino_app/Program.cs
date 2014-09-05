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
            // write your code here

            OneWire oneWire = new OneWire(new OutputPort(Pins.GPIO_PIN_D13, false));

            byte[] serialNum = new byte[8];
            byte[] scratchPad = new byte[9];

            while(true)
            {
                int port = oneWire.FindFirstDevice(true, false);
 
                while(port != 0)
                {
                    oneWire.SerialNum(serialNum, true);
                    Debug.Print(port.ToString());
                    Debug.Print(DumpHex(serialNum));

                    oneWire.TouchReset();
                    oneWire.WriteByte(0x55);
                    for (int i = 0; i < 8; i++)
                    {
                        oneWire.WriteByte(serialNum[i]);
                    }
                    oneWire.WriteByte(0x44);
                    Thread.Sleep(850);

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

                    Debug.Print(DumpHex(scratchPad));
                    byte crc = ComputeCrc(scratchPad, 8);
                    Debug.Print(crc.ToString("x2"));

                    port = oneWire.FindNextDevice(false, true);
                }

                break;
            }


        }

        public static string DumpHex(byte[] data)
        {
            string[] strings = new string[data.Length];
            for(int i=0;i<data.Length;i++)
            {
                strings[i] = data[i].ToString("x2");
            }
            return string.Concat(strings);
        }

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
