using System;
using System.Windows.Forms;

namespace TowerDefense
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Views.MainMenuForm());
        }
    }
}

