using System;
using System.IO;
using LogUtils;

namespace StatusServices
{
    class Program
    {
        //private static FileStream fileStream = null;
        //private static StreamWriter streamWriter = null;
        static void Main()
        {
            try
            {
                AppOptions.LoadOptions();
                DCLogger.InitLogger(
                    log_to: AppOptions.GetAppOptions.LogTo,
                    log_level: AppOptions.GetAppOptions.LogLevel,
                    max_file_count: AppOptions.GetAppOptions.MaxCountFiles,
                    max_file_size_mb: AppOptions.GetAppOptions.MaxLogFileSizeMB,
                    log_file_name: AppOptions.GetAppOptions.LogFileName);

                if (AppOptions.GetAppOptions.ApplicationMode.ToLower().Equals("test"))
                {
                    TestLED tl = new TestLED();
                    tl.PerformTests();
                }
                else
                {
                    //fileStream = new FileStream("statusctrl.log", FileMode.Append);
                    //streamWriter = new StreamWriter(fileStream);
                    //Console.SetOut(streamWriter);
                    StatusController sc = new StatusController();
                    sc.ControlAction();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("StatusServices Exited.");
            }
        }
    }
}
