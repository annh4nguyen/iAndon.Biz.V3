using Avani.Helper;
using System.Configuration;

namespace iAndon.Biz.Logic
{
    public class Utils
    {
        public static Log GetLog()
        {
            string _LogPath = ConfigurationManager.AppSettings["log_path"];
            LogType logLevel = (LogType)int.Parse(ConfigurationManager.AppSettings["log_level"].ToString());
            return new Log(System.IO.Path.Combine(_LogPath, "Logs")) { Level = logLevel };
        }

        public static Log GetRaws(string _folder)
        {
            string _LogPath = ConfigurationManager.AppSettings["log_path"];
            LogType logLevel = (LogType)int.Parse(ConfigurationManager.AppSettings["log_level"].ToString());
            return new Log(System.IO.Path.Combine(_LogPath, _folder)) { Level = logLevel };
        }
    }
}
