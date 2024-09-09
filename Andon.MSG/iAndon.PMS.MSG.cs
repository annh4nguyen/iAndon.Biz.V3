using EasyNetQ;
using iAndon;
using System;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;
using Avani.Helper;

namespace iAndon.MSG
{
    [Queue("iAndon.PMS.MSG", ExchangeName = "iAndon.PMS")]
    public class PMS_MSG
    {
        [XmlElement(ElementName = "h")]
        public Header Header { get; set; }
        [XmlElement(ElementName = "b")]
        public PMS_BodyMessage Body { get; set; }
        public PMS_MSG()
        {
            this.Header = new Header();
            this.Body = new PMS_BodyMessage();
        }
        public PMS_MSG(string from, DateTime time, MessageType type, PMS_BodyMessage bodyMessage)
        {
            this.Header = new Header()
            {
                From = from,
                Time = time,
                Type = type
            };
            this.Body = new PMS_BodyMessage()
            {
                productlineid = bodyMessage.productlineid,
                planid = bodyMessage.planid,
                ponumber = bodyMessage.ponumber,
                productcode = bodyMessage.productcode,
                productname = bodyMessage.productname,
                model = bodyMessage.model,
                planquantity = bodyMessage.planquantity,
                actualquantity = bodyMessage.actualquantity,
                status = bodyMessage.status,
                lastproductiontime = bodyMessage.lastproductiontime
            };
        }
        public string Serialize()
        {
            return Utils.Serialize(this);
        }

        public static PMS_MSG Deserialize(string xml)
        {
            return Utils.Deserialize<PMS_MSG>(xml);
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

    public class PMS_BodyMessage
    {
        [XmlElement(ElementName = "pms")]
        public int productlineid { get; set; }
        public double planid { get; set; }
        public double ponumber { get; set; }
        public string productcode { get; set; }
        public string productname { get; set; }
        public string model { get; set; }
        public int planquantity { get; set; }
        public int actualquantity { get; set; }
        public string status { get; set; }
        public string lastproductiontime { get; set; }
    }


}

