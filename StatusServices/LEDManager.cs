using LogUtils;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusServices
{

    public enum LEDModes
    {
        Off = 0,
        SlowBlink = 1,
        FastBlink = 2,
        On = 3
    }
    public class LEDManager
    {
        #region var
        private readonly int AppStatusLED;
        private readonly int PowerLineStatusLED;
        private readonly GpioController gpioController = new GpioController(PinNumberingScheme.Logical);
        private LEDModes PreviousAppLEDMode = LEDModes.Off;
        private LEDModes PreviousPowerLineLEDMode = LEDModes.Off;
        private LEDModes AppLEDMode = LEDModes.Off;
        private LEDModes PowerLineLEDMode = LEDModes.Off;
        private TimeEnvelope AppStatusTimer = null;
        private TimeEnvelope PowerLineStatusTimer = null;
        private readonly int CycleDurationMs;
        private readonly int FastBlinkIntervalMs;
        private readonly int SlowBlinkIntervalMs;
        #endregion // var

        #region property
        public LEDModes StatusLEDMode
        {
            get { return AppLEDMode; }
            set 
            {
                if (AppLEDMode != value)
                {
                    PreviousAppLEDMode = AppLEDMode;
                    AppLEDMode = value;
                }
            }
        }

        public LEDModes PLStatusLEDMode
        {
            get { return PowerLineLEDMode; }
            set { PowerLineLEDMode = value; }
        }
        #endregion // property

        public LEDManager()
        {
            AppStatusLED = AppOptions.GetAppOptions.AppStatusGPIO;
            PowerLineStatusLED = AppOptions.GetAppOptions.PowerLineStatusGPIO;
            CycleDurationMs = AppOptions.GetAppOptions.LEDCycleDurationMs;
            FastBlinkIntervalMs = AppOptions.GetAppOptions.FastSpeedIntervalMs; // sleep is based on the fastest blink rate
            SlowBlinkIntervalMs = AppOptions.GetAppOptions.SlowSpeedIntervalMs;
            // init pins
            if (AppStatusLED >= 0)
            {
                gpioController.OpenPin(AppStatusLED, PinMode.Output);
            }
            if (PowerLineStatusLED >= 0)
            {
                gpioController.OpenPin(PowerLineStatusLED, PinMode.Output);
            }
        }

        /// <summary>
        /// function never returns
        /// </summary>
        public void Manage()
        {
            int status_blink_time_ms = 0 ;
            //int status_low_time_ms = 0;
            bool status_on = false;
            int powerline_blink_time_ms = 0;
            //int plc_low_time_ms = 0;
            bool powerline_on = false;
            if (AppStatusLED > 0)
            {
                AppStatusTimer = new TimeEnvelope(CycleDurationMs); // run loop every 250 ms
                gpioController.Write(AppStatusLED, PinValue.Low);
                AppLEDMode = LEDModes.Off;
                status_on = false;
                status_blink_time_ms = 0;
            }
            if (PowerLineStatusLED > 0)
            {
                PowerLineStatusTimer = new TimeEnvelope(CycleDurationMs);
                gpioController.Write(AppStatusLED, PinValue.Low);
                PowerLineLEDMode = LEDModes.Off;
                powerline_on = false;
                powerline_blink_time_ms = 0;
            }

            while (true)
            {
                // status LED
                if (AppStatusLED > 0) 
                {
                    if (AppLEDMode != PreviousAppLEDMode)
                    {
                        switch (AppLEDMode)
                        {
                            case LEDModes.Off:
                                status_blink_time_ms = 0;
                                gpioController.Write(AppStatusLED, PinValue.Low);
                                break;
                            case LEDModes.On:
                                status_blink_time_ms = 0;
                                gpioController.Write(AppStatusLED, PinValue.High);
                                break;
                            case LEDModes.FastBlink:
                                status_blink_time_ms = FastBlinkIntervalMs;
                                break;
                            case LEDModes.SlowBlink:
                                status_blink_time_ms = SlowBlinkIntervalMs;
                                break;
                            default:
                                DCLogger.Error("Unknown App Status LED mode setup.");
                                break;
                        }
                        PreviousAppLEDMode = AppLEDMode;
                    }

                    if ( status_blink_time_ms != 0)
                    {
                        if (AppStatusTimer.IsMultiple(status_blink_time_ms))
                        {
                            if (status_on)
                            {
                                gpioController.Write(AppStatusLED, PinValue.Low);
                                status_on = false;
                            }
                            else
                            {
                                gpioController.Write(AppStatusLED, PinValue.High);
                                status_on = true;
                            }
                        }
                    }

                }
                // PLN status LED
                if (PowerLineStatusLED > 0)
                {
                    if (PowerLineLEDMode != PreviousPowerLineLEDMode)
                    {
                        switch (PowerLineLEDMode)
                        {
                            case LEDModes.Off:
                                powerline_blink_time_ms = 0;
                                gpioController.Write(PowerLineStatusLED, PinValue.Low);
                                break;
                            case LEDModes.On:
                                powerline_blink_time_ms = 0;
                                gpioController.Write(PowerLineStatusLED, PinValue.High);
                                break;
                            case LEDModes.FastBlink:
                                powerline_blink_time_ms = FastBlinkIntervalMs;
                                break;
                            case LEDModes.SlowBlink:
                                powerline_blink_time_ms = SlowBlinkIntervalMs;
                                break;
                            default:
                                DCLogger.Error("Unknown App Status LED mode setup.");
                                break;
                        }
                        PreviousPowerLineLEDMode = PowerLineLEDMode;
                    }

                    if (powerline_blink_time_ms != 0)
                    {
                        if (PowerLineStatusTimer.IsMultiple(powerline_blink_time_ms))
                        {
                            if (powerline_on)
                            {
                                gpioController.Write(PowerLineStatusLED, PinValue.Low);
                                powerline_on = false;
                            }
                            else
                            {
                                gpioController.Write(PowerLineStatusLED, PinValue.High);
                                powerline_on = true;
                            }
                        }
                    }

                }
                System.Threading.Thread.Sleep(FastBlinkIntervalMs);
                if ( (AppStatusLED > 0) && (AppStatusTimer != null))
                {
                    AppStatusTimer.AddTime(FastBlinkIntervalMs);
                }
                if ( (PowerLineStatusLED > 0) && (PowerLineStatusTimer != null) )
                {
                    PowerLineStatusTimer.AddTime(FastBlinkIntervalMs);
                }
            }

        }

    }
}
