using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VMDToBVH.Views;

namespace VMDToBVH
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            if (!e.Name.Contains(".resources,"))
            {
                MessageBox.Show(string.Format("アセンブリ「{0}」の解決に失敗しました。", e.Name));
            }
            return null;   
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var dialog = new ErrorDialog();
            dialog.Message = e.ExceptionObject.ToString();
            dialog.ShowDialog();
        }
    }
}
