using Microsoft.Extensions.Configuration;
using System;

namespace WinSWLauncher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var conf = builder.Build();
            
            //.AddJsonFile($"appsettings.{Environments.Development}.json", optional: true, reloadOnChange: true);
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(conf));
        }
    }
}