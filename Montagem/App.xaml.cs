using System;
using System.Windows;
using Telerik.Windows.Controls;

namespace Montagem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LocalizationManager.Manager = new LocalizationManager()
            {
                ResourceManager = GridTraducao.ResourceManager
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(e.ExceptionObject.ToString());
            return;
        }
    }
}