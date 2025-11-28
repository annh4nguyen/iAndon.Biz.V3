using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iAndon.Biz.Logic.Models
{
    public class IO_Message
    {
        public DateTime Timestamp { get; set; }
        public int Id { get; set; }
        public bool IsConnect { get; set; }

        public int Input01 { get; set; }
        public int Input02 { get; set; }
        public int Input03 { get; set; }
        public int Input04 { get; set; }

        public int Output01 { get; set; }
        public int Output02 { get; set; }
        public int Output03 { get; set; }
        public int Output04 { get; set; }


    }

    public class DeviceRaw
    {
        public bool isConnect { get; set; }
        public int id { get; set; }
        public string type { get; set; }
        public List<int> outputs { get; set; }
        public List<int> inputs { get; set; }
    }

    public class RootRaw
    {
        public long timestamp { get; set; }
        public List<DeviceRaw> devices { get; set; }
    }


    public class OutputParams
    {
        public int id { get; set; }
        public int output { get; set; }
        public bool state { get; set; }
    }

    public class OutputCommand
    {
        public string method { get; set; }
        public OutputParams @params { get; set; }
    }
}
