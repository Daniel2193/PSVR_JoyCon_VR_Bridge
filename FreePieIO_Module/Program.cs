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

namespace FreePieIO_Module
{
    class Program
    {
        /* Todo list
        Move JoyCon connection/communication to this project
        Find a good position for the Raspberry Pi to improve position tracking accuracy
        Add/implement a second Raspberry Pi
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

        //0 -> Controller buttons   1 -> Headset   2 -> Left Controller   3 -> Right Controller
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


        //Values
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
        static double yAW2 = 0;
        static double YAW3 = 0;

        static void Main(string[] args)
        {
            SetFreePIEDllPath();

            
            Task.Run(async () =>
            {
                using (var udp = new UdpClient(2193))
                {
                    string[] data;
                    while (true)
                    {
                        //This UdpClient rreceives Data from a Raspberry Pi
                        var rec = await udp.ReceiveAsync();
                        string sup = Encoding.ASCII.GetString(rec.Buffer);
                        data = Regex.Split(sup, "lol");

                        //At the moment im only transfering 3 numbers (double)
                        X1 = Convert.ToDouble(data[0]) * x1m;
                        Y1 = Convert.ToDouble(data[1]) * y1m;
                        Z1 = (Convert.ToDouble(data[2]) * z1m) - 2;
                    }
                }
            });


            var yaw = 3f;
            var arr = new Data[1];

            while (true)
            {
                arr[0] = new Data
                {
                    Yaw = yaw,
                    X = (float) X1,
                    Y = (float) Y1,
                    Z = (float) Z1
                };
                yaw = yaw + 0.00001f;
                if (yaw > 180)
                    yaw = -180;

                var result = freepie_io_6dof_write(WriteToIndex, 1, arr);
                if (result != 0)
                    throw new Exception(string.Format("Could not write to slot {0}", WriteToIndex));

            }
        }

        private static void SetFreePIEDllPath()
        {
            var path = Registry.GetValue(string.Format("{0}\\Software\\{1}", Registry.CurrentUser, "FreePIE"), "path", null) as string;
            SetDllDirectory(path);
            Console.WriteLine("Init done");
        }
    }
}
