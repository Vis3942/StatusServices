using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusServices
{
    internal class TestLED
    {
        private readonly LEDManager ledManager = new LEDManager();
        Task ledTask = null;
        private readonly string disp_str = "(1) Start, (2) Turn off, (3) Turn On, (4) Slow Blink, (5) Fast Blink, (q) Exit App";
        private string cmd_str = "";
        public void PerformTests()
        {
            while (true)
            {
                Console.WriteLine(disp_str);
                Console.Write("Command: ");
                cmd_str = Console.ReadLine();
                switch (cmd_str)
                {
                    case "1":
                        if (ledTask == null)
                        {
                            ledTask = Task.Factory.StartNew(() => ledManager.Manage());
                            Console.WriteLine("Started LED manager");
                        }
                        else
                        {
                            Console.WriteLine("LED manager is already running.");
                        }
                        break;
                    case "2":
                        if (ledTask != null)
                        {
                            ledManager.StatusLEDMode = LEDModes.Off;
                            Console.WriteLine("Turned off");
                        }
                        else
                        {
                            Console.WriteLine("Please start LED control by selecting 1 ");
                        }
                        break;
                    case "3":
                        if (ledTask != null)
                        {
                            ledManager.StatusLEDMode = LEDModes.On;
                            Console.WriteLine("Turned on");
                        }
                        else
                        {
                            Console.WriteLine("Please start LED control by selecting 1 ");
                        }
                        break;
                    case "4":
                        if (ledTask != null)
                        {
                            ledManager.StatusLEDMode = LEDModes.SlowBlink;
                            Console.WriteLine("Slow Blink");
                        }
                        else
                        {
                            Console.WriteLine("Please start LED control by selecting 1 ");
                        }
                        break;
                    case "5":
                        if (ledTask != null)
                        {
                            ledManager.StatusLEDMode = LEDModes.FastBlink;
                            Console.WriteLine("Fast Blink");
                        }
                        else
                        {
                            Console.WriteLine("Please start LED control by selecting 1 ");
                        }
                        break;

                    case "q":
                    case "Q":
                        Console.WriteLine("Exiting Test LED Application");
                        System.Environment.Exit(0);
                        break;

                }
            }

        }
    }
}
