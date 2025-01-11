using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;


namespace PrintingAppQR
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        private static string appGuid = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";
        [STAThread]
        static void Main()
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);

            //Application.Run(new PrintBill());
            // Application.Run(new PrintBill());

            bool mutexCreated = false;
            Mutex appMutex = new Mutex(false, appGuid, out mutexCreated);
            if (!appMutex.WaitOne(0))
            {
                //MessageBox.Show("Only one application at a time, please!");
                Environment.Exit(0);
                return;
            }
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PrintBill());
            }
            finally
            {
                appMutex.ReleaseMutex();
            }

        }
    }
}
