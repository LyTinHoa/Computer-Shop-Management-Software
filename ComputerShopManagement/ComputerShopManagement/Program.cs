using System;
using System.Windows.Forms;
using ComputerShopManagement.Views; // Added to access the Views folder

namespace ComputerShopManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // Start the application with the new modern Login Form
            Application.Run(new frmLogin());
        }
    }
}
