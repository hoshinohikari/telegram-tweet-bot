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
        /*// Log an info level message
        if (Logs.IsInfoEnabled) Logs.Info("Application [ConsoleApp] Start");

        // Log a debug message. Test if debug is enabled before
        // attempting to log the message. This is not required but
        // can make running without logging faster.
        if (Logs.IsDebugEnabled) Logs.Debug("This is a debug message");

        try
        {
            / *Bar();* /
        }
        catch (Exception ex)
        {
            // Log an error with an exception
            Logs.Error("Exception thrown from method Bar", ex);
        }

        Logs.Error("Hey this is an error!");

        // Push a message on to the Nested Diagnostic Context stack
        using (log4net.NDC.Push("NDC_Message"))
        {
            Logs.Warn("This should have an NDC message");

            // Set a Mapped Diagnostic Context value  
            log4net.MDC.Set("auth", "auth-none");
            Logs.Warn("This should have an MDC message for the key 'auth'");

        } // The NDC message is popped off the stack at the end of the using {} block

        Logs.Warn("See the NDC has been popped of! The MDC 'auth' key is still with us.");

        // Log an info level message
        if (Logs.IsInfoEnabled) Logs.Info("Application [ConsoleApp] End");

        Console.Write("Press Enter to exit...");
        Console.ReadLine();*/
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