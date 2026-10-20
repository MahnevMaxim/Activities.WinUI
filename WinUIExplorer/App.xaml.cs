using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WinUIExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // DPI awareness обязателен: без него WindowFromPoint/координаты захвата
            // не совпадают с физическими координатами экрана (см. WinAppDriverUIRecorder).
            NativeMethods.SetProcessDPIAware();
            base.OnStartup(e);
        }
    }
}
