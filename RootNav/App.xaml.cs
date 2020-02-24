using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace RootNav
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exc = e.Exception as Exception;

            if (exc.GetType() == typeof(RootNav.Core.LiveWires.LiveWireManager.LiveWireWorkNotCompletedException))
            {
                // Retrieve the inner exception if possible to find the correct stack trace
                this.Dispatcher.Invoke(new Action<Exception>((ex) => { ExceptionHandlerWindow ehw = new ExceptionHandlerWindow(); ehw.SetText(ex); ehw.ShowDialog(); }), exc.InnerException == null ? exc : exc.InnerException);

            }
            else if (exc.GetType() == typeof(RootNav.Core.MixtureModels.EMManager.EMWorkNotCompletedException))
            {
                // Retrieve the inner exception if possible to find the correct stack trace
                this.Dispatcher.Invoke(new Action<Exception>((ex) => { ExceptionHandlerWindow ehw = new ExceptionHandlerWindow(); ehw.SetText(ex); ehw.ShowDialog(); }), exc.InnerException == null ? exc : exc.InnerException);
            }
            else
            {
                // Default exception message
                this.Dispatcher.Invoke(new Action<Exception>((ex) => { ExceptionHandlerWindow ehw = new ExceptionHandlerWindow(); ehw.SetText(ex); ehw.ShowDialog(); }), exc.InnerException == null ? exc : exc.InnerException);
            }

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.Dispatcher.Invoke(new Action<Exception>((ex) => { ExceptionHandlerWindow ehw = new ExceptionHandlerWindow(); ehw.SetText(ex); ehw.ShowDialog(); }),
                (e.ExceptionObject as Exception));

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
