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

        private enum AngleType
        {
            Degrees = 0,
            Radiant = 1
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
        static int L = -1;
        static int R = -1;
        static bool[] calibFinished;
        static double DriftYawL = 0;
        static double DriftPitchL = 0;
        static double DriftRollL = 0;
        static double DriftYawR = 0;
        static double DriftPitchR = 0;
        static double DriftRollR = 0;
        static double[] DriftYawOffset;
        static int DriftSamples = 200;
        static int DriftSampleDelay = 50;
        static bool DriftFilterOnAllAxis = false;

        static bool skipCalib = false;

        static double distance = 0.3;
        static double yawOffset = 180;
        static double yawMultiplier = -1;
        static double pitchOffset = 90;
        static double pitchMultiplier = 1;
        static double rollOffset = 0;
        static double rollMultiplier = 1;
        static Vector3[] JoyconPosition;
        static Vector3[] HomePosition;


        //Packet array
        static Data[] packets;
        static Data headset = new Data();
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
            JoyconPosition = new Vector3[2];
            JoyconPosition[0] = new Vector3(0, 0, 0);
            JoyconPosition[1] = new Vector3(0, 0, 0);
            HomePosition = new Vector3[2];
            HomePosition[0] = new Vector3(0, 0, 0);
            HomePosition[1] = new Vector3(0, 0, 0);

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
                if (joyconManager.j[0].isLeft)
                {
                    L = 0;
                    R = 1;
                }
                else
                {
                    L = 1;
                    R = 0;
                }
                if (!skipCalib)
                {
                    calibFinished = new bool[2];
                    calibFinished[0] = false;
                    calibFinished[1] = false;
                    Console.ReadLine();
                    Console.WriteLine("Beginning calibration. This will take " + (DriftSamples * DriftSampleDelay / 1000) + " seconds");
                    calibration(L);
                    calibration(R);
                    Thread.Sleep(DriftSamples * DriftSampleDelay + 500);
                    resetOrientation(L);
                    resetOrientation(R);
                    Console.WriteLine("Calibration complete");
                }
                else
                {
                    Console.WriteLine("!!!!! CALIBRATION SKIPPED !!!!!");
                    calibFinished = new bool[2];
                    calibFinished[0] = true;
                    calibFinished[1] = true;
                }
                


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
                if (joyconManager.j[L].GetButtonDown(Joycon.Button.CAPTURE))
                    resetOrientation(L);
                if (joyconManager.j[R].GetButtonDown(Joycon.Button.HOME))
                    resetOrientation(R);
                packets[0] = new Data
                {
                    Yaw = ((joyconManager.j[R].GetButton(Joycon.Button.PLUS) ? 1:0) << 24) + ((joyconManager.j[L].GetButton(Joycon.Button.MINUS) ? 1 : 0) << 16) + ((joyconManager.j[R].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0) << 8) + (joyconManager.j[L].GetButton(Joycon.Button.SHOULDER_2) ? 255 : 0),
                    Pitch = ((joyconManager.j[R].GetButton(Joycon.Button.SL) ? 1 : 0) << 24) + ((joyconManager.j[L].GetButton(Joycon.Button.SR) ? 1 : 0) << 16) + ((joyconManager.j[R].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0) << 8) + (joyconManager.j[L].GetButton(Joycon.Button.DPAD_LEFT) ? 1 : 0),
                    Roll = ((joyconManager.j[R].GetButton(Joycon.Button.STICK) ? 1 : 0) << 8) + (joyconManager.j[L].GetButton(Joycon.Button.STICK) ? 1 : 0),
                    X = ((int)((joyconManager.j[R].GetStick()[1] + 0.03f)* 0.5f * 255 + 0.5f * 255) << 24) + ((int)(joyconManager.j[L].GetStick()[1] * 0.5f * 255 + 0.5f * 255) << 16) + ((int)((joyconManager.j[R].GetStick()[1] + 0.03f) * 0.5f * 255 + 0.5f * 255) << 8) + (int)(joyconManager.j[L].GetStick()[0] * 0.5f * 255 + 0.5f * 255)
                };
                packets[1] = new Data
                {
                    X = (float) X1,
                    Y = (float) Y1,
                    Z = (float) Z1
                };
                YAW2 = (getYaw(L) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftYawL) - DriftYawOffset[L]);
                PITCH2 = (getPitch(L) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftPitchL));
                ROLL2 = (getRoll(L) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftRollL));
                calculatePosition(L);
                X2 = JoyconPosition[L].X;
                Y2 = JoyconPosition[L].Y;
                Z2 = JoyconPosition[L].Z;
                packets[2] = new Data
                {
                    X = (float)X2,
                    Y = (float)Y2,
                    Z = (float)Z2,
                    Yaw = (float) YAW2,
                    Pitch = (float) PITCH2,
                    Roll = (float) ROLL2
                };
                YAW3 = (getYaw(R) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftYawR) - DriftYawOffset[R]);
                PITCH3 = (getPitch(R) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftPitchR));
                ROLL3 = (getRoll(R) - (DateTime.Now.Subtract(tick).TotalSeconds * DriftRollR));
                calculatePosition(R);
                X3 = JoyconPosition[R].X;
                Y3 = JoyconPosition[R].Y;
                Z3 = JoyconPosition[R].Z;
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
                    throw new Exception("Could not write to IO slots. Try reinstalling FreePie and reboot your PC");
                var res = freepie_io_6dof_read(1, 1, out headset);
            }
        }

        public static void calculatePosition(int index){
            Vector3 tmp = Vector3.Zero;
            
            double b = ((getYaw(index) - yawOffset) * -1) / 360 * 2 * Math.PI;
            double a = (getPitch(index))/ 360 * 2 * Math.PI;
            tmp.X = HomePosition[index].X + distance * Math.Cos(a) * Math.Sin(b);
            tmp.Y = HomePosition[index].Y + distance * Math.Sin(a);
            tmp.Z = HomePosition[index].Z + distance * Math.Cos(a) * Math.Cos(b);
            
            JoyconPosition[index] = tmp;
        }

        private static void resetOrientation(int index)
        {
            DriftYawOffset[index] = getRawYaw(index);
        }

        private static void calibration(int index)
        {
            Task.Run(async() =>
            {
                double yaw = 0;
                double pitch = 0;
                double roll = 0;

                double lastYaw = getRawYaw(index);
                double lastPitch = getPitch(index);
                double lastRoll = getRoll(index);

                for (int i = 0; i < DriftSamples; i++)
                {
                    await Task.Delay(DriftSampleDelay);
                    yaw += (getRawYaw(index) - lastYaw);
                    lastYaw = getRawYaw(index);
                    pitch += (getPitch(index) - lastPitch);
                    lastPitch = getPitch(index);
                    roll += (getRoll(index) - lastRoll);
                    lastRoll = getRoll(index);
                }
                double avgYaw = (yaw / DriftSamples) * (1000 / DriftSampleDelay);
                double avgPitch = (pitch / DriftSamples) * (1000 / DriftSampleDelay);
                double avgRoll = (roll / DriftSamples) * (1000 / DriftSampleDelay);
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == L ? "left" : "right") + " Yaw: " + avgYaw + "°/sec");
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == L ? "left" : "right") + " Pitch: " + avgPitch + "°/sec");
                Console.WriteLine(Environment.NewLine + "AVG Drift on " + (index == L ? "left" : "right") + " Roll: " + avgRoll + "°/sec");
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

        private static double getRawYaw(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.Z * 180 / Math.PI * yawMultiplier);
        }
        private static double getRoll(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.X * 180 / Math.PI);
        }
        private static double getPitch(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.Y * 180 / Math.PI + pitchOffset);
        }
        private static double getYaw(int index)
        {
            return (joyconManager.j[index].GetVector().eulerAngles.Z * 180 / Math.PI * yawMultiplier + yawOffset);
        }

        private static double fixAngle(double input, AngleType type)
        {
            if(type == AngleType.Degrees)
            {
                return input % 360;
            }
            else if(type == AngleType.Radiant)
            {
                return input % (2 * Math.PI);
            }
            return input;
        }
    }
}
