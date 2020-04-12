using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Joycon4CS;
using System.Threading;

namespace FreePieIO_Module
{
    class Program
    {
        /* Todo list
        Move JoyCon connection/communication to this project
        Find a good position for the Raspberry Pi to improve position tracking accuracy
        Add/implement a second Raspberry Pi for better accuracy
        */

        [StructLayout(LayoutKind.Sequential)]
        private struct Data
        {
            public float Yaw;
            public float Pitch;
            public float Roll;

            public float X;
            public float Y;
            public float Z;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("freepie_io.dll")]
        private static extern int freepie_io_6dof_slots();

        [DllImport("freepie_io.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int freepie_io_6dof_read(int index, int length, out Data data);

        [DllImport("freepie_io.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int freepie_io_6dof_write(int index, int length, Data[] data);

        //The port to receive the data from
        private const int port = 2193;

        //Index:   0 -> Controller Buttons   1 -> Headset   2 -> Left Controller   3 -> Right Controller
        private const int WriteToIndex = 1;

        //Multiplier
        static double x1m = 0.02;
        static double x2m = 1;
        static double x3m = 1;
        static double y1m = -0.02;
        static double y2m = 1;
        static double y3m = 1;
        static double z1m = 0.02;
        static double z2m = 1;
        static double z3m = 1;
        static double roll2m = 1;
        static double roll3m = 1;
        static double pitch2m = 1;
        static double pitch3m = 1;
        static double yaw2m = 1;
        static double yaw3m = 1;

        //Offsets
        static double z1o = -2;

        //Position/Rotation Values
        static double X1 = 0;
        static double X2 = 0;
        static double X3 = 0;
        static double Y1 = 0;
        static double Y2 = 0;
        static double Y3 = 0;
        static double Z1 = 0;
        static double Z2 = 0;
        static double Z3 = 0;
        static double ROLL2 = 0;
        static double ROLL3 = 0;
        static double PITCH2 = 0;
        static double PITCH3 = 0;
        static double YAW2 = 0;
        static double YAW3 = 0;


        //Joycon Communication
        static JoyconManager joyconManager = new JoyconManager();

        //Packet
        static Data[] packets;
        /*
         * Controller Button Packet (Slot 0):
         * 
         * YawByte[0] -> L_Trigger
         * YawByte[1] -> R_Trigger
         * YawByte[2] -> L_System
         * YawByte[3] -> R_System
         * PitchByte[0] -> L_Menu
         * PitchByte[1] -> R_Menu
         * PitchByte[2] -> L_Grip
         * PitchByte[3] -> R_Grip
         * RollByte[0] -> L_TouchPadPress
         * RollByte[1] -> R_TouchPadPress
         * RollByte[2] -> [not used]
         * RollByte[3] -> [not used]
         * XByte[0] -> L_TouchPadAxisX
         * XByte[1] -> R_TouchPadAxisX
         * XByte[2] -> L_TouchPadAxisY
         * XByte[3] -> R_TouchPadAxisY
         * Y -> [not used]
         * Z -> [not used]
         */


        static void Main(string[] args)
        {

            Console.WriteLine("Beginning Setup");

            SetFreePIEDllPath();

            packets = new Data[4];

            joyconManager.Scan();

            Thread.Sleep(1000);

            if (joyconManager.j.Count < 2)
            {
                Console.WriteLine("0 or 1 JoyCon found" + Environment.NewLine + "Joycons disabled");
            }
            else
            {
                Console.WriteLine("Found " + joyconManager.j.Count + " JoyCons");
                joyconManager.Start();
            }
            
            Task.Run(async () =>
            {
                using (var udp = new UdpClient(port))
                {
                    string[] data;
                    while (true)
                    {
                        //This UdpClient receives Data from the Raspberry Pi
                        var rec = await udp.ReceiveAsync();
                        string sup = Encoding.ASCII.GetString(rec.Buffer);
                        data = Regex.Split(sup, "aaaa");//Any non numeric seperator

                        //At the moment im only transfering 3 numbers (double)
                        X1 = Convert.ToDouble(data[0]) * x1m;
                        Y1 = Convert.ToDouble(data[1]) * y1m;
                        Z1 = (Convert.ToDouble(data[2]) * z1m) - z1o;
                    }
                }
            });


            var yaw = 0f;

            while (true)
            {
                packets[0] = new Data
                {
                    Yaw = ((joyconManager.j[1].GetButton(Joycon.Button.PLUS) ? 1:0) << 24) + ((joyconManager.j[0].GetButton(Joycon.Button.MINUS) ? 1 : 0) << 16) + ((joyconManager.j[1].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0),
                    Pitch = ((joyconManager.j[1].GetButton(Joycon.Button.SL) ? 1 : 0) << 24) + ((joyconManager.j[0].GetButton(Joycon.Button.SR) ? 1 : 0) << 16) + ((joyconManager.j[1].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0),
                    Roll = ((joyconManager.j[1].GetButton(Joycon.Button.STICK) ? 1 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.STICK) ? 1 : 0),
                    X = ((int)((joyconManager.j[1].GetStick()[1] + 0.03f)* 0.5f * 255 + 0.5f * 255) << 24) + ((int)(joyconManager.j[0].GetStick()[1] * 0.5f * 255 + 0.5f * 255) << 16) + ((int)((joyconManager.j[0].GetStick()[1] + 0.03f) * 0.5f * 255 + 0.5f * 255) << 8) + (int)(joyconManager.j[0].GetStick()[0] * 0.5f * 255 + 0.5f * 255),
                    Z = yaw
                };
                packets[1] = new Data
                {
                    //Yaw = yaw,
                    X = (float) X1,
                    Y = (float) Y1,
                    Z = (float) Z1
                };
                packets[2] = new Data
                {
                    X = (float) X2,
                    Y = (float) Y2,
                    Z = (float) Z2,
                    Yaw = (float)(joyconManager.j[0].GetVector().eulerAngles.Z * 180 / Math.PI) * -1,
                    Pitch = (float)(joyconManager.j[0].GetVector().eulerAngles.Y * 180 / Math.PI) - 180,
                    Roll = (float) (joyconManager.j[0].GetVector().eulerAngles.X * 180 / Math.PI)
                };
                packets[3] = new Data
                {
                    X = (float) X3,
                    Y = (float) Y3,
                    Z = (float) Z3,
                    Yaw = (float)(joyconManager.j[1].GetVector().eulerAngles.Z * 180 / Math.PI) * -1 - DriftFilter.currentValue,
                    Pitch = (float)(joyconManager.j[1].GetVector().eulerAngles.Y * 180 / Math.PI) - 180,
                    Roll = (float)(joyconManager.j[1].GetVector().eulerAngles.X * 180 / Math.PI)
                };
                yaw = yaw + 0.00001f;//Just so I can see if the program is still sending values
                if (yaw > 180)
                    yaw = -180;

                var result = freepie_io_6dof_write(0, 4, packets);
                if (result != 0)
                    throw new Exception("Could not write to IO slots");

            }
        }

        private static void SetFreePIEDllPath()//Example code by the FreePie developer to load the FreePieIO dll
        {
            var path = Registry.GetValue(string.Format("{0}\\Software\\{1}", Registry.CurrentUser, "FreePIE"), "path", null) as string;
            SetDllDirectory(path);
        }
    }
}
