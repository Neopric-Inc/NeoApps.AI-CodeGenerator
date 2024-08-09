using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace nkv.MicroService.Utility
{
    public class AppLogManager
    {
        private static AppLogManager _logManager = null;

        private AppLogManager()
        {
        }

        public AppLogManager(string fullConfigFilePath, Assembly assembly)
        {

            var logRepo = log4net.LogManager.GetRepository(assembly);
            log4net.Config.XmlConfigurator.Configure(logRepo, new FileInfo(fullConfigFilePath));

            _logManager = new AppLogManager();

        }
        public static ILog GetLogger<T>()
        {
            return _logManager.GetLogger(typeof(T));
        }

        public ILog GetLogger(Type type)
        {
            var log = log4net.LogManager.GetLogger(type);
            return log;
        }
    }

    public static class GenericLoggingExtensions
    {
        public static ILog Log<T>(this T thing)
        {
            var log = AppLogManager.GetLogger<T>();
            return log;
        }
    }
}
