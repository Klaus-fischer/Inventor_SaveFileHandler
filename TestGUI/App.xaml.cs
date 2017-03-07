using InvAddIn;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TestGUI
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AsmNumberDialog and = new AsmNumberDialog();

            and.ProjectName = "PV001_Projektvorlage";
            and.WorkingDir = new WorkingDir(@"X:\Vorlagen\ProjektordnerVorlage\Konstruktion");
            and.ShowDialog();
            this.Shutdown();

            base.OnStartup(e);
        }
    }
}
