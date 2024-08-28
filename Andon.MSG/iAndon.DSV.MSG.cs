using EasyNetQ;
using iAndon;
using System;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;
using Avani.Helper;

namespace iAndon.MSG
{
    [Queue("iAndon.DSV.MSG", ExchangeName = "iAndon.DSV")]
    public class DSV_MSG
    {
        [XmlElement(ElementName = "h")]
        public Header Header { get; set; }
        [XmlElement(ElementName = "b")]
        public BodyMessage Body { get; set; }
        public DSV_MSG()
        {
            this.Header = new Header();
            this.Body = new BodyMessage();
        }
        public DSV_MSG(string from, DateTime time, MessageType type, int nodeId, int in01, int in02, int in03)
        {
            this.Header = new Header()
            {
                From = from,
                Time = time,
                Type = type
            };
            this.Body = new BodyMessage()
            {
                NodeId = nodeId,
                In01 = in01,
                In02 = in02,
                In03 = in03,
            };
        }
        public string Serialize()
        {
            return Utils.Serialize(this);
        }

        public static DSV_MSG Deserialize(string xml)
        {
            return Utils.Deserialize<DSV_MSG>(xml);
        }

        public void StoreRawData(Log _Logger, string _LogPath, string _LogCategory)
        {
            try
            {
                string strMessage = JsonConvert.SerializeObject(this);
                DateTime receivedTime = this.Header.Time;
                string clientIP = this.Header.From;
                strMessage = $"{strMessage}#";
                string path = Path.Combine(_LogPath, "Raws", receivedTime.Year.ToString(), receivedTime.Month.ToString("00"), receivedTime.Day.ToString("00"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string fileName = Path.Combine(path, $"{clientIP}.txt");
                if (File.Exists(fileName))
                {
                    using (StreamWriter writer = File.AppendText(fileName))
                    {
                        writer.WriteLine(strMessage);
                    }
                }
                else
                {
                    using (StreamWriter writer = File.CreateText(fileName))
                    {
                        writer.Write(strMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Error when store message: {ex} ", LogType.Error);
            }
        }


    }

    public class BodyMessage
    {
        [XmlElement(ElementName = "m")]
        public int NodeId { get; set; }
        public int In01 { get; set; }
        public int In02 { get; set; }
        public int In03 { get; set; }
    }


}

