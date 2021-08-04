using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusServices
{
    public class StartOptions
    {
        public int FastSpeedIntervalMs { get; set; }
        public int SlowSpeedIntervalMs { get; set; }
        public int LEDCycleDurationMs { get; set; }
        public int AppStatusGPIO { get; set; }
        public int PowerLineStatusGPIO { get; set; }
        public string ApplicationMode { get; set; }
        public int PingIntervalSecs { get; set; }
        public string LogTo { get; set; }
        public string LogLevel { get; set; }
        public float MaxLogFileSizeMB { get; set; }
        public int MaxCountFiles { get; set; }
        public string LogFileName { get; set; }
        public bool UsesCellularConnection { get; set; }
    }
}
