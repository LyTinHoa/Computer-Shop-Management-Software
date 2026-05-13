using System;
using System.Windows.Forms;
using System.Globalization; // Added for regional settings
using System.Threading;     // Added for thread control
using ComputerShopManagement.Views;

namespace ComputerShopManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            CultureInfo usCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = usCulture;
            Thread.CurrentThread.CurrentUICulture = usCulture;

            ApplicationConfiguration.Initialize();

            Application.Run(new frmLogin());
        }
    }
}