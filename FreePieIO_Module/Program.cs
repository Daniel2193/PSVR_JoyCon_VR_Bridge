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


        //Joycons
        static JoyconManager joyconManager = new JoyconManager();
        static int DriftSamples = 20;
        static int DriftSampleDelay = 500;
        static bool[] calibFinished;
        static double DriftYawL = 0;
        static double DriftPitchL = 0;
        static double DriftRollL = 0;
        static double DriftYawR = 0;
        static double DriftPitchR = 0;
        static double DriftRollR = 0;
        static double[] DriftYawOffset;

        static bool DriftFilterOnAllAxis = false;

        //Packet
        static Data[] packets;
        /*
         * Controller Button Packet / Slot 0:
         * 
         * YawByte[0] -> L_Trigger (0-255)
         * YawByte[1] -> R_Trigger (0-255)
         * YawByte[2] -> L_System (0;1)
         * YawByte[3] -> R_System (0;1)
         * PitchByte[0] -> L_Menu (0;1)
         * PitchByte[1] -> R_Menu (0;1)
         * PitchByte[2] -> L_Grip (0;1)
         * PitchByte[3] -> R_Grip (0;1)
         * RollByte[0] -> L_TouchPadPress (0:1)
         * RollByte[1] -> R_TouchPadPress (0:1)
         * RollByte[2] -> [not used]
         * RollByte[3] -> [not used]
         * XByte[0] -> L_TouchPadAxisX (0-255)
         * XByte[1] -> R_TouchPadAxisX (0-255)
         * XByte[2] -> L_TouchPadAxisY (0-255)
         * XByte[3] -> R_TouchPadAxisY (0-255)
         * Y -> [not used]
         * Z -> [not used]
         */


        static void Main(string[] args)
        {

            Console.WriteLine("Beginning Setup");

            SetFreePIEDllPath();

            packets = new Data[4];
            DriftYawOffset = new double[2];

            joyconManager.Scan();

            Thread.Sleep(1000);

            if (joyconManager.j.Count < 2)
            {
                Console.WriteLine("0 or 1 JoyCon found" + Environment.NewLine + "Joycons disabled (yet)");
            }
            else
            {
                Console.WriteLine("Found " + joyconManager.j.Count + " JoyCons");
                joyconManager.Start();
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("Calibrating Sensors -> leave Joycons on a flat surface until calibration is finished");
                Console.WriteLine("To Start the calibration press ENTER/RETURN");
                Console.WriteLine("Awaiting calibration...");
                calibFinished = new bool[2];
                calibFinished[0] = false;
                calibFinished[1] = false;
                Console.ReadLine();
                Console.WriteLine("Beginning calibration. This will take");
                calibration(0);
                calibration(1);
                Thread.Sleep(DriftSamples*DriftSampleDelay+500);
                resetOrientation(0);
                resetOrientation(1);
                Console.WriteLine("Calibration complete");


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

            DateTime tick = DateTime.Now;
            while (true)
            {
                if (joyconManager.j[0].GetButtonDown(Joycon.Button.CAPTURE))
                    resetOrientation(0);
                if (joyconManager.j[1].GetButtonDown(Joycon.Button.HOME))
                    resetOrientation(1);
                packets[0] = new Data
                {
                    Yaw = ((joyconManager.j[1].GetButton(Joycon.Button.PLUS) ? 1:0) << 24) + ((joyconManager.j[0].GetButton(Joycon.Button.MINUS) ? 1 : 0) << 16) + ((joyconManager.j[1].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0),
                    Pitch = ((joyconManager.j[1].GetButton(Joycon.Button.SL) ? 1 : 0) << 24) + ((joyconManager.j[0].GetButton(Joycon.Button.SR) ? 1 : 0) << 16) + ((joyconManager.j[1].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0),
                    Roll = ((joyconManager.j[1].GetButton(Joycon.Button.STICK) ? 1 : 0) << 8) + (joyconManager.j[0].GetButton(Joycon.Button.STICK) ? 1 : 0),
                    X = ((int)((joyconManager.j[1].GetStick()[1] + 0.03f)* 0.5f * 255 + 0.5f * 255) << 24) + ((int)(joyconManager.j[0].GetStick()[1] * 0.5f * 255 + 0.5f * 255) << 16) + ((int)((joyconManager.j[0].GetStick()[1] + 0.03f) * 0.5f * 255 + 0.5f * 255) << 8) + (int)(joyconManager.j[0].GetStick()[0] * 0.5f * 255 + 0.5f * 255)
                };
                packets[1] = new Data
                {
                    X = (float) X1,
                    Y = (float) Y1,
                    Z = (float) Z1
                };
                YAW2 = (getYaw(0) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftYawL) - DriftYawOffset[0]);
                PITCH2 = (getPitch(0) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftPitchL));
                ROLL2 = (getRoll(0) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftRollL));
                packets[2] = new Data
                {
                    X = (float)X2,
                    Y = (float)Y2,
                    Z = (float)Z2,
                    Yaw = (float) YAW2,
                    Pitch = (float) PITCH2,
                    Roll = (float) ROLL2
                };
                YAW3 = (getYaw(1) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftYawR) - DriftYawOffset[1]);
                PITCH3 = (getPitch(1) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftPitchR));
                ROLL3 = (getRoll(1) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftRollR));
                packets[3] = new Data
                {
                    X = (float) X3,
                    Y = (float) Y3,
                    Z = (float) Z3,
                    Yaw = (float) YAW3,
                    Pitch = (float) PITCH3,
                    Roll = (float) ROLL3
                };

                var result = freepie_io_6dof_write(0, 4, packets);
                if (result != 0)
                    throw new Exception("Could not write to IO slots");
            }
        }

        private static void resetOrientation(int index)
        {
            DriftYawOffset[index] = getYaw(index);
        }

        private static void calibration(int index)
        {
            Task.Run(async() =>
            {
                double yaw = 0;
                double pitch = 0;
                double roll = 0;

                double lastYaw = getYaw(index);
                double lastPitch = getPitch(index);
                double lastRoll = getRoll(index);

                for (int i = 0; i < DriftSamples; i++)
                {
                    await Task.Delay(DriftSampleDelay);
                    yaw += (getYaw(index) - lastYaw);
                    lastYaw = getYaw(index);
                    pitch += (getPitch(index) - lastPitch);
                    lastPitch = getPitch(index);
                    roll += (getRoll(index) - lastRoll);
                    lastRoll = getRoll(index);
                }
                double avgYaw = (yaw / DriftSamples) * (1000 / DriftSampleDelay);
                double avgPitch = (pitch / DriftSamples) * (1000 / DriftSampleDelay);
                double avgRoll = (roll / DriftSamples) * (1000 / DriftSampleDelay);
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == 0 ? "left" : "right") + " Yaw: " + avgYaw + "°/sec");
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == 0 ? "left" : "right") + " Pitch: " + avgPitch + "°/sec");
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == 0 ? "left" : "right") + " Roll: " + avgRoll + "°/sec");
                if(index == 0)
                {
                    DriftYawL = avgYaw;
                    if (DriftFilterOnAllAxis)
                    {
                        DriftPitchL = avgPitch;
                        DriftRollL = avgRoll;
                    }
                    
                }
                else
                {
                    DriftYawR = avgYaw;
                    if (DriftFilterOnAllAxis)
                    {
                        DriftPitchR = avgPitch;
                        DriftRollR = avgRoll;
                    }
                    
                }

            });
            calibFinished[index] = true;

        }

        private static void SetFreePIEDllPath()//Example code by the FreePie developer to load the FreePieIO dll (and the hidapi.dll)
        {
            var path = Registry.GetValue(string.Format("{0}\\Software\\{1}", Registry.CurrentUser, "FreePIE"), "path", null) as string;
            SetDllDirectory(path);
        }

        private static double getRoll(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.X * 180 / Math.PI);
        }
        private static double getPitch(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.Y * 180 / Math.PI - 180);
        }
        private static double getYaw(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.Z * 180 / Math.PI * -1);
        }
    }
}
