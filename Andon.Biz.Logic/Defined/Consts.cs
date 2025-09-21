using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace iAndon.Biz.Logic
{
    public class Consts
    {

        public static string LOG_CATEGORY = "Biz";


  

        public static DateTime DEFAULT_TIME = new DateTime(2024, 1, 1, 6, 0, 0); //Bổ sung cái này cho đỡ lỗi
        public static DateTime MAX_TIME = new DateTime(2050, 1, 1, 6, 0, 0); //Bổ sung cái này cho đỡ lỗi

        public static short DRAFT_STATUS = 0;
        public static short DONE_STATUS = 3;
        public static short TIMEOUT_STATUS = 4;
        public static short ACCEPT_STATUS = 1;
        public static short REJECT_STATUS = -1;

        public static short ON_STATUS = 1;
        public static short OFF_STATUS = 0;

        public static short DEFAULT_QUANTITY = 1260;
        public static short DEFAULT_CYCLE_TIME = 20;


        public static string Production_Good = "Good";
        public static string Production_Fast = "Fast";
        public static string Production_Delayed = "Delayed";
        public static string Production_Stop = "Stopped";

        public static string PMS_RUNNING = "RUNNING";
        public static string PMS_STOPPED = "STOP";
        public static string PMS_COMPLETED = "COMPLETED";

        //====================================================
        //EVENT_DEF
        public static string EVENTDEF_RUNNING = "1";
        public static string EVENTDEF_STOP = "2";
        public static string EVENTDEF_NOPLAN = "101";
        public static string EVENTDEF_BREAK = "100";
        public static string EVENTDEF_DISCONNECT = "11";
        public static string EVENTDEF_DEFAULT = "0";
        public static string EVENTDEF_DEFAULT_NAME_EN = "REMAINED";
        public static string EVENTDEF_DEFAULT_NAME_VN = "Còn lại";
        public static string EVENTDEF_DEFAULT_COLOR = "#FFF";

        public static string EVENTDEF_PLAN = "0";
        public static string EVENTDEF_PLAN_NAME_EN = "PLAN";
        public static string EVENTDEF_PLAN_NAME_VN = "Kế hoạch";
        public static string EVENTDEF_PLAN_COLOR = "#FFF";
        //====================================================

        public static string CHANGE_ADDNEW = "ADDNEW";
        public static string CHANGE_UPDATE = "UPDATE";
        public static string CHANGE_DELETE = "DELETE";

        public static short CUSTOMER_ID = 3;
        public static short FACTORY_ID = 1;

        // Date 15-02-2022 cập nhật lại format ngày từ dd/MM/yyyy -> yyyy-MM-dd
        public static string MIN_DATE = "2022-01-01";
        //public static string MAX_DATE = "31/12/3000";
        // Date 15-02-2022 cập nhật lại format ngày từ dd/MM/yyyy -> yyyy-MM-dd
        public static string MAX_DATE = "2222-12-31";

        public static short START_TIME_SLOT = 65;

        public static short HOUR_FOR_NEW_DAY = 6;

        public static short DIFFERENCE_PRODUCTION_QUANTITY = 2; //30 giây

        public static short BUFFER_TIME_IN_MINUTE = 0;
        public static short BUFFER_TIME_IN_SECOND = 1;

        public const short MinuteArchive = 1;
        public const short HourArchive = 2;
        public const short DayArchive = 3;
        public const short MonthArchive = 4;
        public const short YearArchive = 5;
    }
}
