using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusServices
{
    public class AppOptions
    {
        #region var
        private static readonly string OptionsFileName = "dcstatus_startup_options.json";
        private static bool OptionsLoaded = false;
        private static StartOptions startOptions = null;
        #endregion //var

        public static StartOptions GetAppOptions
        {
            get 
            { 
                if (startOptions == null)
                {
                    LoadOptions();
                }
                return startOptions; 
            }
        }

        public static void LoadOptions()
        {
            if (!OptionsLoaded)
            {
                if (System.IO.File.Exists(OptionsFileName))
                {

                    try
                    {
                        string jsonstr = System.IO.File.ReadAllText(OptionsFileName);
                        startOptions = System.Text.Json.JsonSerializer.Deserialize<StartOptions>(jsonstr);
                        OptionsLoaded = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error loading options {e.Message}");
                        OptionsLoaded = false;
                        startOptions = null;
                    }

                }    
                else
                {
                    Console.WriteLine($"startup options file {OptionsFileName} missing.");
                }
            }
        }

    }
}
