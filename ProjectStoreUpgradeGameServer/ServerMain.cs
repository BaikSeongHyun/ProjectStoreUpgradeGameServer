using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ProjectStoreUpgradeGameServer
{
    public class ServerMain
    {
        // main method
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

    }
}
