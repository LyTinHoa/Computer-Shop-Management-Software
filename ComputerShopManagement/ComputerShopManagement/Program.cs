using System;
using System.Windows.Forms;

namespace ComputerShopManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // Starts the app with the default form. We will change this to frmLogin later.
            Application.Run(new Form1());
        }
    }
}