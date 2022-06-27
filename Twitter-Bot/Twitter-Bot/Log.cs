using System.Reflection;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace Twitter_Bot;

public static class Log
{
    private static readonly ILog Logs = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    public static void DebugLog(string debug)
    {
        Logs.Debug(debug);
    }

    public static void InfoLog(string info)
    {
        Logs.Info(info);
    }

    public static void WarnLog(string warn)
    {
        Logs.Warn(warn);
    }

    public static void ErrorLog(string error)
    {
        Logs.Error(error);
    }
}