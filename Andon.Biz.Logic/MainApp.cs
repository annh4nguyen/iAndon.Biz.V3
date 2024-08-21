﻿using iAndon.MSG;
using iAndon.Biz.Logic.Models;
using Avani.Helper;
using EasyNetQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace iAndon.Biz.Logic
{
    public class MainApp
    {
        #region Configuration

        private string _RabbitMQHost = ConfigurationManager.AppSettings["RabbitMQ.Host"];
        private string _RabbitMQVirtualHost = ConfigurationManager.AppSettings["RabbitMQ.VirtualHost"];
        private string _RabbitMQUser = ConfigurationManager.AppSettings["RabbitMQ.User"];
        private string _RabbitMQPassword = ConfigurationManager.AppSettings["RabbitMQ.Password"];
        private string _CustomerId = ConfigurationManager.AppSettings["CustomerId"];

        private string _SMTP_Host = ConfigurationManager.AppSettings["SMTP.Host"];
        private int _SMTP_Port = int.Parse(ConfigurationManager.AppSettings["SMTP.Port"]);
        private int _SMTP_Timeout = int.Parse(ConfigurationManager.AppSettings["SMTP.Timeout"]);
        private bool _SMTP_SSL = ConfigurationManager.AppSettings["SMTP.SSL"] == "1";
        private string _SMTP_User = ConfigurationManager.AppSettings["SMTP.User"];
        private string _SMTP_Password = ConfigurationManager.AppSettings["SMTP.Password"];
        private string _EmailFrom = ConfigurationManager.AppSettings["email_from"];

        private int _QueueInterval = int.Parse(ConfigurationManager.AppSettings["queue_interval"]);
        private int _MessageInterval = int.Parse(ConfigurationManager.AppSettings["message_interval"]);
        private int _DataInterval = int.Parse(ConfigurationManager.AppSettings["data_interval"]);
        private int _ProcessInterval = int.Parse(ConfigurationManager.AppSettings["process_interval"]);
        private int _SyncInterval = int.Parse(ConfigurationManager.AppSettings["sync_interval"]);
        private string _Sync_Url = ConfigurationManager.AppSettings["sync_url"];
        private int _DataLiveInterval = 1000 * 60 * 60 * int.Parse(ConfigurationManager.AppSettings["data_live_interval"]);
        private int _DisplayInterval = int.Parse(ConfigurationManager.AppSettings["update_display_interval"]);
        private int _ReloadInterval = int.Parse(ConfigurationManager.AppSettings["reload_interval"]);
        private int _ArchiveInterval = 1000 * int.Parse(ConfigurationManager.AppSettings["archive_interval"]);

        private int _ProductionLevel = int.Parse(ConfigurationManager.AppSettings["production_level"]);
        private int _ProductionStop = int.Parse(ConfigurationManager.AppSettings["production_stop"]);
        private int _MinTakttime = int.Parse(ConfigurationManager.AppSettings["min_takttime"]);
        private int _DisconnectedTime = int.Parse(ConfigurationManager.AppSettings["disconnected_time"]);
        private int _DataLiveTime = int.Parse(ConfigurationManager.AppSettings["data_live_time"]);
        private int _DefaultEvent = int.Parse(ConfigurationManager.AppSettings["default_event"]);
        private static string _DefaultProduct = ConfigurationManager.AppSettings["default_product"];
        private static int _DefaultHeadCount = int.Parse(ConfigurationManager.AppSettings["default_head_count"]);
        private static decimal _DefaultCycleTime = decimal.Parse(ConfigurationManager.AppSettings["default_cycle_time"]);

        private bool _ProductionInBreak = (int.Parse(ConfigurationManager.AppSettings["production_in_break"]) == 1);
        private bool _CalculateByPerformance = (int.Parse(ConfigurationManager.AppSettings["calculate_by_performance"]) == 1);
        private bool _AutoSplitWorkPlan2Time = (int.Parse(ConfigurationManager.AppSettings["auto_split_workplan_detail"]) == 1);
        private bool _AutoAddWorkPlan = (int.Parse(ConfigurationManager.AppSettings["auto_add_workplan"]) == 1);
        private bool _UseProductConfig = (int.Parse(ConfigurationManager.AppSettings["use_product_config"]) == 1);
        private bool _UseResponeTime = (int.Parse(ConfigurationManager.AppSettings["use_response_event"]) == 1);
        private bool _AddEventUntilFinish = (int.Parse(ConfigurationManager.AppSettings["add_event_until_finish"]) == 1);
        
        private static int _AutoSwitchWorkPlanInterval = int.Parse(ConfigurationManager.AppSettings["auto_switch_workplan_interval"]);
        

        private int _FixTimeProduction = int.Parse(ConfigurationManager.AppSettings["fix_time_for_production"]);
        private int _TimeProduction2Stop = int.Parse(ConfigurationManager.AppSettings["fix_time_for_stop"]);

        private bool _isProcessMessage = (int.Parse(ConfigurationManager.AppSettings["is_process_message"]) == 1);
        private bool _isProcessSync = (int.Parse(ConfigurationManager.AppSettings["is_process_sync"]) == 1);
        private bool _isSendControlMessage = (int.Parse(ConfigurationManager.AppSettings["is_send_control_message"]) == 1);
        private bool _isProcessArchive = (int.Parse(ConfigurationManager.AppSettings["is_process_archive"]) == 1);
        private bool _isProcessCleanData = (int.Parse(ConfigurationManager.AppSettings["is_process_clean_data"]) == 1);
        private bool _isAutoBreakTime = (int.Parse(ConfigurationManager.AppSettings["is_auto_break"]) == 1);
        private bool _isLineEventByNode = (int.Parse(ConfigurationManager.AppSettings["is_line_event_by_node"]) == 1);


        private int _FixTimeDifference = int.Parse(ConfigurationManager.AppSettings["fix_time_difference"]);

        #endregion

        #region props
        const int SecondArchive = 0;
        const int MinuteArchive = 1;
        const int HourArchive = 2;
        const int DayArchive = 3;
        const int MonthArchive = 4;
        const int YearArchive = 5;

        const int BulkSize = 1000;

        const int INPUT_ON = 1;
        const int INPUT_OFF = 0;

        private Log _Logger;
        private readonly string _LogCategory = "Biz";

        private DateTime START_SERVICE_TIME = Consts.DEFAULT_TIME;
        private static IBus _EventBus = null;

        //Timer
        private System.Timers.Timer _TimerProccessQueue = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessMessage = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessData = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessWork = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessSync = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessDataLive = new System.Timers.Timer();
        private System.Timers.Timer _TimerDisplay = new System.Timers.Timer();
        private System.Timers.Timer _TimerReload = new System.Timers.Timer();
        private System.Timers.Timer _TimerProccessArchive = new System.Timers.Timer();
        

        private List<DM_FACTORY> _Factories = new List<DM_FACTORY>();
        private List<DM_MES_ZONE> _Zones = new List<DM_MES_ZONE>();
        private List<DM_MES_NODE> _Nodes = new List<DM_MES_NODE>();
        private List<DG_DM_SHIFT> _Shifts = new List<DG_DM_SHIFT>();
        private List<DM_MES_BREAK_TIME> _BreakTimes = new List<DM_MES_BREAK_TIME>();
        private List<DM_MES_EVENTDEF> _EventDefs = new List<DM_MES_EVENTDEF>();

        private List<MES_NODE_EVENT> _NodeEvents = new List<MES_NODE_EVENT>();
        private List<MES_LINE_EVENT> _LineEvents = new List<MES_LINE_EVENT>();
        private List<DM_MES_CONFIGURATION> _Configurations = new List<DM_MES_CONFIGURATION>();
        private List<MES_STOP_REASON> _StopReasons = new List<MES_STOP_REASON>();

        private List<DM_MES_PRODUCT> _Products = new List<DM_MES_PRODUCT>();
        private List<DM_MES_PRODUCT_CONFIG> _ProductConfigs = new List<DM_MES_PRODUCT_CONFIG>();
        private List<DM_MES_PRODUCT_CATEGORY> _ProductCategories = new List<DM_MES_PRODUCT_CATEGORY>();

        private List<WorkPlan> _WorkPlans = new List<WorkPlan>();

        private List<Line> _Lines = new List<Line>();

        private List<Andon_MSG> _Messages = new List<Andon_MSG>();

        private DateTime _LastTimeReload = DateTime.Now;

        private static bool IsError = false;//True: Gặp vấn đề cập nhật DB => Cần dừng quá trình lấy message từ Queue
        private static bool IsRunning = false;//True: Đang trong quá trình lấy message từ Queue
        #endregion

        #region public methods
        public MainApp()
        {
            _Logger = Utils.GetLog();
        }
        public void Start()
        {
            try
            {
                _Logger.Write(_LogCategory, $"iAndon Biz Service is Starting!", LogType.Info);

                InitData();
                //StartGetMessage();
                if (_isProcessMessage)
                {
                    _TimerProccessQueue.Interval = _QueueInterval;
                    _TimerProccessQueue.Elapsed += _TimerProccessQueue_Elapsed;
                    _TimerProccessQueue.Start();
                }

                //_TimerProccessMessage.Interval = _MessageInterval;
                //_TimerProccessMessage.Elapsed += _TimerProccessMessage_Elapsed;
                //_TimerProccessMessage.Start();

                _TimerProccessWork.Interval = _ProcessInterval;
                _TimerProccessWork.Elapsed += _TimerProccessWork_Elapsed;
                _TimerProccessWork.Start();

                //_TimerReload.Interval = _ReloadInterval;
                //_TimerReload.Elapsed += _TimerReload_Elapsed;
                //_TimerReload.Start();
                //_TimerProccessSync.Interval = _SyncInterval;
                //_TimerProccessSync.Elapsed += _TimerProccessSync_Elapsed;
                //_TimerProccessSync.Start();

                //_TimerProccessData.Interval = _DataInterval;
                //_TimerProccessData.Elapsed += _TimerProccessData_Elapsed;
                //_TimerProccessData.Start();

                if (_isProcessArchive)
                {
                    _TimerProccessArchive.Interval = _ArchiveInterval;
                    _TimerProccessArchive.Elapsed += _TimerProccessArchive_Elapsed;
                    _TimerProccessArchive.Start();
                }
                if (_isProcessCleanData)
                {
                    _TimerProccessDataLive.Interval = _DataLiveInterval;
                    _TimerProccessDataLive.Elapsed += _TimerProccessDataLive_Elapsed;
                    _TimerProccessDataLive.Start();
                }

                _TimerDisplay.Interval = _DisplayInterval;
                _TimerDisplay.Elapsed += _TimerDisplay_Elapsed;
                _TimerDisplay.Start();


                _Logger.Write(_LogCategory, $"iAndon Biz Service is Start Completed!", LogType.Info);

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Start MainApp Error: {ex}", LogType.Error);
            }
        }
        public void Stop()
        {
            try
            {
                StopGetMessage();
                _TimerProccessQueue.Stop();
                _TimerProccessWork.Stop();
                _TimerProccessArchive.Stop();
                _TimerProccessDataLive.Stop();
                _TimerDisplay.Stop();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Stop MainApp Error: {ex}", LogType.Error);
            }
        }
        #endregion
        #region events
        private void _TimerProccessDataLive_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                ProcessCleanDataLive();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ProccessDataLive Error: {ex}", LogType.Error);
            }
            finally
            {
                timer.Start();
            }
        }
        private void _TimerProccessArchive_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                //Process Archive
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ProccessArchive Error: {ex}", LogType.Error);
            }
            finally
            {
                timer.Start();
            }
        }
        private void _TimerProccessQueue_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                if (IsError)
                {
                    _Logger.Write(_LogCategory, $"Error When Process Data, Ignore and Continue...", LogType.Info);
                    IsError = false;
                    StopGetMessage();
                }
                //else
                //{
                //    StartGetMessage();
                //}
                StartGetMessage();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Queue Error: {ex}", LogType.Error);
            }
            finally
            {
                timer.Start();
            }
        }
        private void _TimerProccessMessage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                //ProcessMessage();
                IsError = false;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Message Error: {ex}", LogType.Error);
                IsError = true;
            }
            finally
            {
                timer.Start();
            }
        }
        private void _TimerProccessWork_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                ProcessReload();
                Thread.Sleep(15);
                if (_isProcessSync)
                {
                    ProccessSync();
                    Thread.Sleep(15);
                }
                ProccessWork();
                Thread.Sleep(15);
                ProccessData();
                IsError = false;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ProccessData Error: {ex}", LogType.Error);
                IsError = true;
            }
            finally
            {
                timer.Start();
                //_TimerProccessData.Start();
            }
        }
        private void _TimerProccessSync_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                ProccessSync();
                IsError = false;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Sync Error: {ex}", LogType.Error);
                IsError = true;
            }
            finally
            {
                timer.Start();
                //_TimerProccessWork.Start();
            }
        }
        private void _TimerProccessData_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                //ProcessMessage();

                ProccessData();
                IsError = false;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ProccessData Error: {ex}", LogType.Error);
                IsError = true;
            }
            finally
            {
                timer.Start();
                //_TimerReload.Start();
            }
        }
        private void _TimerDisplay_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {

                //----------------------------------------------------------------------------------------
                // 2024-05-29: Bổ sung xử lý vào DB thay vì websocket-------------------------------
                //----------------------------------------------------------------------------------------
                ProcessDisplay();
                //Xử lý vào Line
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"TimerDisplay Error: {ex}", LogType.Error);
            }
            finally
            {
                _Logger.Write(_LogCategory, $"TimerDisplay Start Again!", LogType.Debug);
                timer.Start();
            }
        }
        private void _TimerReload_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            try
            {
                ProcessReload();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"TimerReload Error: {ex}", LogType.Error);
            }
            finally
            {
                timer.Start();
                //_TimerProccessSync.Start();
            }
        }
        #endregion

        #region private methods
        private void InitData()
        {
            try
            {
                START_SERVICE_TIME = DateTime.Now.AddSeconds(0);
                using (Entities _dbContext = new Entities())
                {
                    //_Customers = _dbContext.tblCustomers.ToList();
                    _Zones = _dbContext.DM_MES_ZONE.ToList();
                    _Nodes = _dbContext.DM_MES_NODE.ToList();
                    _Factories = _dbContext.DM_FACTORY.ToList();
                    _Shifts = _dbContext.DG_DM_SHIFT.ToList();
                    _BreakTimes = _dbContext.DM_MES_BREAK_TIME.ToList();
                    _EventDefs = _dbContext.DM_MES_EVENTDEF.OrderBy(x => x.NUMBER_ORDER).ToList();
                    _StopReasons = _dbContext.MES_STOP_REASON.OrderBy(x => x.NUMBER_ORDER).ToList();
                    //_NodeEvents = _dbContext.MES_NODE_EVENT.Where(x => !x.FINISH.HasValue).ToList();
                    //_LineEvents = _dbContext.tblLineEvents.Where(x => !x.Finish.HasValue).ToList();
                    _Products = _dbContext.DM_MES_PRODUCT.Where(x => x.ACTIVE).ToList();
                    _ProductConfigs = _dbContext.DM_MES_PRODUCT_CONFIG.ToList();
                    _ProductCategories = _dbContext.DM_MES_PRODUCT_CATEGORY.ToList();
                    _Configurations = _dbContext.DM_MES_CONFIGURATION.ToList();

                    DM_MES_EVENTDEF eventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_NOPLAN);

                    List<DM_MES_LINE> tblLines = _dbContext.DM_MES_LINE.ToList();

                    foreach (DM_MES_LINE tblLine in tblLines)
                    {
                        Line line = new Line().Cast(tblLine);

                        //Khởi đầu luôn gán = NOPLAN
                        line.EventDefId = eventDef.EVENTDEF_ID;
                        line.EventDefName_EN = eventDef.EVENTDEF_NAME_EN;
                        line.EventDefName_VN = eventDef.EVENTDEF_NAME_VN;
                        line.EventDefColor = eventDef.EVENTDEF_COLOR;
                        //Factory
                        string _factoryName = "";
                        DM_FACTORY _factory = _Factories.FirstOrDefault(x => x.FACTORY_ID == line.FACTORY_ID);
                        if (_factory != null)
                        {
                            _factoryName = _factory.FACTORY_NAME;
                        }    
                        line.Factory_Name = _factoryName;
                        //Node
                        List<DM_MES_NODE> _nodes = _Nodes.Where(x => x.LINE_ID == line.LINE_ID).ToList();
                        foreach(DM_MES_NODE node in _nodes)
                        {
                            Node nodeLine = new Node().Cast(node);
                            line.Nodes.Add(nodeLine);
                        }    

                        _Lines.Add(line);
                    }

                    //Tải kế hoạch làm việc ở đây --> Làm bước cuối cùng
                    LoadWorkPlans();

                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"InitData Error: {ex}, try to restart service again.", LogType.Error);
                Stop();
            }
        }
        private void LoadWorkPlans()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                if (eventTime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    eventTime = eventTime.AddDays(-1);
                }
                decimal _day = Time2Num(eventTime, DayArchive);
                _Logger.Write(_LogCategory, $"Check for day: {_day}, load workplan.", LogType.Debug);

                //Lấy kế hoạch từ hôm nay trở về sau
                using (Entities _dbContext = new Entities())
                {

                    //Ban đầu không lấy Draft
                    List<MES_WORK_PLAN> tblWorkPlans = new List<MES_WORK_PLAN>();
                    List<MES_WORK_PLAN_DETAIL> tblWorkPlanDetails = new List<MES_WORK_PLAN_DETAIL>();

                    //Nếu Lần đầu thì cứ lấy hết đến Processing
                    //tblWorkPlans = _dbContext.tblWorkPlans.Where(w => w.Status <= (int)PlanStatus.Proccessing && w.Day >= _day).ToList();
                    tblWorkPlans = _dbContext.MES_WORK_PLAN.Where(w => w.STATUS <= (int)PLAN_STATUS.Proccessing).ToList();

                    foreach (MES_WORK_PLAN tblWorkPlan in tblWorkPlans)
                    {
                        WorkPlan workPlan = new WorkPlan().Cast(tblWorkPlan);

                        if (workPlan.STATUS == (int)PLAN_STATUS.Draft)
                        {
                            workPlan.STATUS = (int)PLAN_STATUS.NotStart;
                        }


                        Shift shift = CheckShift(tblWorkPlan.DAY, tblWorkPlan.SHIFT_ID);

                        workPlan.PlanStart = shift.Start;
                        workPlan.PlanFinish = shift.Finish;

                        //Ban đầu vẫn load Draft
                        workPlan.WorkPlanDetails = _dbContext.MES_WORK_PLAN_DETAIL.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID && x.STATUS >= (int)PLAN_STATUS.Draft).ToList();

                        //workPlan.WorkPlanDetails = _dbContext.tblWorkPlanDetails.Where(x => x.WorkPlanId == workPlan.Id).ToList();
                        //Tại sao không lấy Draft???? ==> để lần sau ReLoad tránh trùng lặp

                        //Kiểm tra xem có phải nó đang bị hết hạn không
                        if (workPlan.PlanFinish < eventTime)
                        {
                            workPlan.STATUS= (int)PLAN_STATUS.Timeout;
                            workPlan.Priority = 1; //Đánh dấu để xóa
                        }
                        //Check trùng lắp
                        WorkPlan _check = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID== workPlan.WORK_PLAN_ID);
                        if (_check != null)
                        {
                            _WorkPlans.Remove(_check);
                        }
                        _WorkPlans.Add(workPlan);
                    }
        
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Load WorkPlan Error: {ex}, try to restart service again.", LogType.Error);
                //Stop();
            }

        }
        private void StartGetMessage()
        {
            if (IsRunning) return;
            IsRunning = true;
            try
            {
                _Logger.Write(_LogCategory, $"Start Get Messages from Rabbit", LogType.Info);

                if (_EventBus == null || !_EventBus.IsConnected || !_EventBus.Advanced.IsConnected)
                {
                    ConnectRabbitMQ();
                }
                _EventBus.Subscribe<Andon_MSG>(_CustomerId, msg => {
                    //QueueMessage(msg);
                    //PreProcessMessage(msg);
                    ProcessMessage(msg);
                });
            }
            catch (Exception ex)
            {
                IsRunning = false;
                if (_EventBus != null) _EventBus.Dispose();
                _EventBus = null;
                _Logger.Write(_LogCategory, $"Start Get Message M3 Error: {ex}", LogType.Error);
            }
        }
        private void StopGetMessage()
        {
            try
            {
                IsRunning = false;
                if (_EventBus != null)
                {
                    _Logger.Write(_LogCategory, $"Disconnect RabbitMQ", LogType.Info);
                    _EventBus.Dispose();
                    _EventBus = null;
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Stop Get Message Error: {ex}", LogType.Error);
            }
        }
        private void QueueMessage(Andon_MSG message)
        {
            try
            {
                if (message == null) return;
                if (message.Header == null) return;
                if (message.Body == null) return;
                if (string.IsNullOrEmpty(message.Body.DeviceId)) return;

                /*
                //Fix time difference between time of PLC and Server
                message.Body.TimeOn = message.Body.TimeOn.AddSeconds(_FixTimeDifference);
                message.Body.TimeOff = message.Body.TimeOff.AddSeconds(_FixTimeDifference);
                */

                //Đơn giản là đưa vào Queue
                _Messages.Add(message);

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Queue Message Error: {ex}", LogType.Error);
            }

        }
        private void PreProcessMessage(Andon_MSG message)
        {
            try
            {
                //Chỗ này check hơi hài nhưng không hiểu sao lại bị lỗi
                if (message == null) return;
/*
                Line line = _Lines.FirstOrDefault(l => l.GatewayId == message.Body.GatewayId);

                //Kiểm tra các điều kiện thỏa mãn cho chạy
                if (line == null) continue;
                if (line.WorkPlan == null) continue;
                if (line.WorkPlan.Status > (int)PlanStatus.Proccessing) continue;


                //_Logger.Write(_LogCategory, $"Message in Line {line.Name}", LogType.Debug);

                DateTime minTime = line.WorkPlan.PlanStart;

                DateTime eventTime = message.Header.Time;

                //Tín hiệu từ lúc chưa vào ca thì bỏ qua
                if (message.Body.TimeOn < minTime && message.Body.TimeOff < minTime)
                {
                    continue;
                }


                int _Input = message.Body.NodeId;

                //_Logger.Write(_LogCategory, $"Check Message at input {_Input}", LogType.Debug);

                foreach (Node node in line.Nodes)
                {
                    try
                    {

                        //_Logger.Write(_LogCategory, $"Start check for Node {node.Id} - Start: {node.StartInputs.Count} - Finish: {node.FinishInputs.Count}", LogType.Debug);

                        NodeInput startInput = node.StartInputs.FirstOrDefault(l => l.Input == _Input);
                        NodeInput finishInput = node.FinishInputs.FirstOrDefault(l => l.Input == _Input);

                        if (startInput == null && finishInput == null)
                        {
                            continue;
                        }

                        //Tức là ít nhất phải có 1 thằng nằm trong ca hiện tại
                        node.Last_Received = eventTime;

                        if (startInput != null && finishInput != null)
                        {
                            _Logger.Write(_LogCategory, JsonConvert.SerializeObject(message), LogType.Debug, $"{node.Id:D2}");


                            //Thông tin 2 bản tin gần nhất
                            startInput.LastTimeOn = finishInput.LastTimeOn;
                            startInput.LastTimeOff = finishInput.LastTimeOff;

                            //Trường hợp bản tin ON trước có sẵn để dành, phòng trường hợp lần sau đè mất ON của lần chưa xử lý
                            if (startInput.LastTimeOff < startInput.TempTimeOff)
                            {
                                startInput.LastTimeOff = startInput.TempTimeOff;
                            }

                            //NodeInput _tempInput = new NodeInput();

                            //Nếu đang ON cho cái mới thì lấy lại cái cũ, không thì lấy cái mới
                            //2023-02-16: Sửa lại logic
                            //Nếu bản tin đi 1 cặp thì gán luôn cho FinishInput
                            //Nếu On > Off --> Lưu tạm ON để chờ lần sau

                            //Bổ sung điều kiện lấy theo trạng thái 0/1 của Input

                            //Thằng Finish luôn luôn chờ để đủ bộ

                            if (message.Body.NodeStatus == INPUT_ON)
                            {


                                //Kiểm tra ON nhưng trước đó vừa OFF xong
                                if (message.Body.TimeOff > startInput.LastTimeOff && message.Body.TimeOff > minTime)
                                {
                                    //Bản tin OFF nhưng có thể nó đã ON trước đó 1 lần mà không đọc kịp
                                    //--> Bỏ qua thằng này và lấy OFF chính là thằng ON
                                    finishInput.TempTimeOff = message.Body.TimeOff;

                                    //Thế thì ON phải để dành lần sau
                                    startInput.TempTimeOn = message.Body.TimeOn;

                                }
                                else
                                {
                                    //Nếu có ON trước đó thì lấy ngay
                                    if (startInput.TempTimeOn != Consts.DEFAULT_TIME)
                                    {
                                        finishInput.TempTimeOn = startInput.TempTimeOn;
                                        //Lấy xong thì clear
                                        startInput.TempTimeOn = Consts.DEFAULT_TIME;

                                    }
                                    else
                                    {
                                        //Nếu không có lưu trước thì lấy của Message đó
                                        finishInput.TempTimeOn = message.Body.TimeOn;
                                    }

                                }

                            }
                            else
                            {
                                //if (message.Body.NodeStatus == INPUT_OFF)

                                //Nếu có cache ON trước đó
                                if (startInput.TempTimeOn != Consts.DEFAULT_TIME)
                                {
                                    finishInput.TempTimeOn = startInput.TempTimeOn;
                                    //Lấy xong thì clear
                                    startInput.TempTimeOn = Consts.DEFAULT_TIME;

                                }

                                finishInput.TempTimeOff = message.Body.TimeOff;

                                if (message.Body.TimeOn > finishInput.TempTimeOn)
                                {
                                    if ((bool)node.CheckPallette)
                                    {
                                        //Đối với trường hợp các node check lỗi Pallette thì sẽ bị trượt qua 1 lượt nên cần bỏ qua bản tin gần nhất

                                        //Bản tin OFF nhưng có thể nó đã ON trước đó 1 lần mà không đọc kịp --> Cất để dành
                                        //--> Bỏ qua thằng này và lấy OFF chính là thằng ON
                                        finishInput.TempTimeOff = message.Body.TimeOn;

                                        //Lưu tạm giá trị lần OFF đó để tính lần kế tiếp
                                        startInput.TempTimeOff = message.Body.TimeOff;
                                    }
                                    else
                                    {
                                        //2023-03-14: Bổ sung phân biệt giữa các node check lỗi Pallette
                                        //Trường hợp ko check lỗi Pallette thì cứ vào là nhận hết
                                        finishInput.TempTimeOn = message.Body.TimeOn;
                                    }

                                }
                            }

                            //Lưu tạm ON mới để lấy cho lần sau

                            //Nếu FinishOn > Finish Off --> Bỏ qua
                            if (finishInput.TempTimeOn > finishInput.TempTimeOff) continue;

                            //Xem đến bản tin kế tiếp chưa, nếu vẫn bản tin cũ thì bỏ qua
                            if (finishInput.TempTimeOn > startInput.LastTimeOn && finishInput.TempTimeOff > startInput.LastTimeOff)
                            {
                                finishInput.LastTimeOn = finishInput.TempTimeOn;
                                finishInput.LastTimeOff = finishInput.TempTimeOff;
                            }
                            else
                            {
                                continue;
                            }


                            _Logger.Write(_LogCategory, $"Before message: Node {node.Name}, [ON:{startInput.LastTimeOn:HH:mm:ss.fff} - OFF:{startInput.LastTimeOff:HH:mm:ss.fff}]", LogType.Debug, $"{node.Id:D2}");
                            _Logger.Write(_LogCategory, $"After  message: Node {node.Name}, [ON:{finishInput.LastTimeOn:HH:mm:ss.fff} - OFF:{finishInput.LastTimeOff:HH:mm:ss.fff}]", LogType.Debug, $"{node.Id:D2}");


                            ProductPair product = new ProductPair();
                            //_Logger.Write(_LogCategory, $"Before Check start for Queue message: Node {node.Id} - Start: Input {startInput.Input} - Flag {startInput.Flag} - Time: {startInput.LastTime}", LogType.Debug);


                            if (startInput.Flag == Consts.ON_STATUS)
                            {
                                product.StartTime = startInput.LastTimeOn;
                                //product.TimeOn = startInput.LastTimeOn;
                            }
                            else
                            {
                                product.StartTime = startInput.LastTimeOff;
                                //product.TimeOn = _tempInput.LastTimeOn;
                            }

                            if (finishInput.Flag == Consts.ON_STATUS)
                            {
                                product.FinishTime = finishInput.LastTimeOn;
                                //product.TimeOff = startInput.LastTimeOff;
                            }
                            else
                            {
                                product.FinishTime = finishInput.LastTimeOff;
                                //product.TimeOff = _tempInput.LastTimeOff;
                            }

                            product.TimeOn = finishInput.LastTimeOn;
                            product.TimeOff = finishInput.LastTimeOff;

                            //Check trường hợp khởi đầu
                            //Input đó chưa có bất cứ 1 sản phẩm nào cả --> Cứ xuất hiện cái mới thì ghi nhận, StartTime mà trước ca thì tính -20s
                            if (finishInput.LastProduct == Consts.DEFAULT_TIME)
                            //if (product.StartTime < line.WorkPlan.PlanStart)
                            {
                                product.StartTime = product.FinishTime.AddSeconds(0 - (double)line.WorkPlan.PlanCycleTime);

                            }

                            //Chỉ kiểm tra có trùng bước trước hay không
                            if (product.FinishTime <= finishInput.LastProduct)
                            {
                                continue;
                            }

                            //Check ok thì mới add vào
                            _Logger.Write(_LogCategory, $"Pre process product: Node {node.Name}, Start: {product.StartTime:HH:mm:ss.fff}, Finish: {product.FinishTime:HH:mm:ss.fff}, On: {product.TimeOn:HH:mm:ss.fff} - Off: {product.TimeOff:HH:mm:ss.fff}", LogType.Debug, $"{node.Id:D2}");
                            product = VerifyProduct(node, product);
                            if (product != null)
                            {
                                _Logger.Write(_LogCategory, $"Post process product: Node {node.Name}, Start: {product.StartTime:HH:mm:ss.fff}, Finish: {product.FinishTime:HH:mm:ss.fff}, On: {product.TimeOn:HH:mm:ss.fff} - Off: {product.TimeOff:HH:mm:ss.fff}", LogType.Debug, $"{node.Id:D2}");

                                //Check lại thêm tình huống giờ giải lao bị thay đổi --> Có thể trùng
                                if (product.FinishTime <= finishInput.LastProduct)
                                {
                                    continue;
                                }


                                node.ProductPairs.Add(product);
                                //Gán giá trị vào cho cặp đầu vào cuối cùng
                                finishInput.LastProduct = product.FinishTime;
                                //Cập nhật lại bộ Temp
                                finishInput.TempTimeOn = finishInput.LastTimeOn;
                                finishInput.TempTimeOff = finishInput.TempTimeOff;


                            }

                        }

                    }
                    catch (Exception ex1)
                    {
                        _Logger.Write(_LogCategory, $"PreProcess Message Error - Node {node.Id} - Msg [{JsonConvert.SerializeObject(message)}]: {ex1}", LogType.Error);
                    }
                }
*/
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"PreProcess Message Error: {ex}", LogType.Error);
            }
        }
        private void ProcessMessage(Andon_MSG message)
        {
            try
            {

                //PreProcessMessage();
                _Logger.Write(_LogCategory, $"Message {JsonConvert.SerializeObject(message)}", LogType.Debug);

                //Chỗ này check hơi hài nhưng không hiểu sao lại bị lỗi
                if (message == null) return;

                _Logger.Write(_LogCategory, $"Gateway: {message.Header.From}", LogType.Debug);
                Line line = _Lines.FirstOrDefault(l => l.GATEWAY_ID == message.Header.From);
                _Logger.Write(_LogCategory, $"Line match with Gateway: {line.LINE_ID}", LogType.Debug);
                //Kiểm tra các điều kiện thỏa mãn cho chạy
                if (line == null) return;
                if (line.EventDefId == Consts.EVENTDEF_NOPLAN) return; //Không có kế hoạch thì bỏ qua

                if (line.WorkPlan == null) return;
                if (line.WorkPlan.STATUS != (int)PLAN_STATUS.Proccessing) return;

                DateTime eventTime = DateTime.Now;

                //_Logger.Write(_LogCategory, $"Message {JsonConvert.SerializeObject(message)} in Line {line.LINE_CODE}", LogType.Debug);

                DateTime minTime = line.WorkPlan.PlanStart;

                DateTime msgTime = message.Header.Time;

                //Tín hiệu từ lúc chưa vào ca thì bỏ qua
                if (msgTime < minTime) return;

                string _deviceId = message.Body.DeviceId;
                Node _node = line.Nodes.FirstOrDefault(x => x.DEVICE_ID == _deviceId);
                _Logger.Write(_LogCategory, $"Node: {JsonConvert.SerializeObject(_node)} - Device: {_deviceId}", LogType.Debug);

                if (_node == null) return;
                List<DM_MES_EVENTDEF> lstEventDefs = _EventDefs.OrderByDescending(x => x.NUMBER_ORDER).ToList(); //Sắp xếp ngược lại, ưu tiên từ cao xuống thấp
                foreach (DM_MES_EVENTDEF eventDef in lstEventDefs)
                {
                    string formula = eventDef.FORMULA;
                    if (string.IsNullOrEmpty(formula)) continue;
                    formula = formula.Replace("In01", $"{message.Body.In01:0}")
                                    .Replace("In02", $"{message.Body.In02:0}")
                                    .Replace("In03", $"{message.Body.In03:0}")
                                    .Replace("In04", $"{message.Body.In04:0}")
                                    .Replace("In05", $"{message.Body.In05:0}")
                                    .Replace("In06", $"{message.Body.In06:0}");

                    DataTable dataTable = new DataTable();
                    bool test = Convert.ToBoolean(dataTable.Compute(formula, ""));
                    if (test)
                    {
                        _Logger.Write(_LogCategory, $"Test Node Event for {_node.NODE_ID}: Event {eventDef.EVENTDEF_ID}", LogType.Debug);
                        ChangeNodeEvent(line.LINE_ID, _node.NODE_ID, msgTime, eventDef.EVENTDEF_ID);
                        break; //Ưu tiên, gặp thằng nào thì dừng luôn
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Message Error: {ex}", LogType.Error);
            }
        }
        private void ProcessReload()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;

                    List<MES_LOG_LAST_UPDATE> lastUpdates = _dbContext.MES_LOG_LAST_UPDATE.Where(x => x.UPDATED > _LastTimeReload).ToList();
                    //Có dữ liệu thì mới làm
                    if (lastUpdates.Count > 0)
                    {
                        DateTime eventTime = DateTime.Now;
                        eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);

                        #region ReportLineDetail
                        List<MES_LOG_LAST_UPDATE> updateReportDetails = lastUpdates.Where(x => x.OBJECT_TYPE == "REPORTLINEDETAIL").ToList();
                     
                        //Cập nhật sản lượng nhập vào
                        ReloadReportDetail();

                        #endregion

                        #region WorkPlan

                        List<MES_LOG_LAST_UPDATE> updateWorkPlans = lastUpdates.Where(x => x.OBJECT_TYPE == "WORKPLAN").ToList();
                        if (updateWorkPlans.Count > 0)
                        {
                            //Cập nhật điều chỉnh kế hoạch
                            foreach (MES_LOG_LAST_UPDATE lastUpdate in updateWorkPlans)
                            {
                                List<MES_REPORT_LINE_DETAIL> removedList = null;

                                //Nếu là xóa thì phải tìm xem thằng nào chứa nó thì xóa đi
                                if (lastUpdate.UPDATE_EVENT == "DELETE")
                                {
                                    _Logger.Write(_LogCategory, $"Delete WorkPlan ID = [{lastUpdate.OBJECT_ID}]", LogType.Info);

                                    //Xiên hết sạch luôn
                                    WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID == lastUpdate.OBJECT_ID);
                                    if (workPlan != null)
                                    {
                                        if (workPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                                        {
                                            //Xóa trong reportLine
                                            removedList = RemoveWorkPlan(workPlan.LINE_ID, workPlan.WORK_PLAN_ID);
                                            //Xoá luôn trong DB
                                            foreach (MES_REPORT_LINE_DETAIL reportDetail in removedList)
                                            {
                                                MES_REPORT_LINE_DETAIL tblReportLineDetail = _dbContext.MES_REPORT_LINE_DETAIL.FirstOrDefault(x => x.REPORT_LINE_DETAIL_ID == reportDetail.REPORT_LINE_DETAIL_ID);
                                                if (tblReportLineDetail != null)
                                                {
                                                    _dbContext.MES_REPORT_LINE_DETAIL.Remove(tblReportLineDetail);
                                                }
                                            }
                                        }
                                        //Xóa trong WorkPlan

                                        lock (_WorkPlans)
                                        {
                                            _WorkPlans.Remove(workPlan);
                                        }
                                    }
                                    continue;
                                }

                                //Giờ thì làm đến thằng thêm
                                //Nếu là xóa thì phải tìm xem thằng nào chứa nó thì xóa đi
                                if (lastUpdate.UPDATE_EVENT == "ADDNEW")
                                {
                                    _Logger.Write(_LogCategory, $"Addnew WorkPlan ID = [{lastUpdate.OBJECT_ID}]", LogType.Info);

                                    MES_WORK_PLAN newItem = _dbContext.MES_WORK_PLAN.FirstOrDefault(x => x.WORK_PLAN_ID == lastUpdate.OBJECT_ID);
                                    if (newItem != null)
                                    {
                                        WorkPlan newWorkPlan = new WorkPlan().Cast(newItem);
                                        //Đặt trạng thái cho WorkPlan --> trạng thái của WorkPlanDetail sẽ ăn theo
                                        if (newWorkPlan.STATUS == (int)PLAN_STATUS.Draft)
                                        {
                                            newWorkPlan.STATUS = (int)PLAN_STATUS.NotStart;
                                        }
                                        //Xử lý Shift
                                        Shift shift = CheckShift(newWorkPlan.DAY, newWorkPlan.SHIFT_ID);
                                        newWorkPlan.PlanStart = shift.Start;
                                        newWorkPlan.PlanFinish = shift.Finish;

                                        //Check trùng lắp
                                        WorkPlan _check = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID == newWorkPlan.WORK_PLAN_ID);
                                        if (_check != null)
                                        {
                                            _WorkPlans.Remove(_check);
                                        }
                                        _WorkPlans.Add(newWorkPlan);
                                    }
                                    continue;
                                }

                            }
                        }
                        #endregion

                        #region WorkPlanDetail
                        List<MES_LOG_LAST_UPDATE> updateWorkPlanDetails = lastUpdates.Where(x => x.OBJECT_TYPE == "WORKPLANDETAIL").ToList();
                        if (updateWorkPlanDetails.Count > 0)
                        {
                            //Cập nhật điều chỉnh kế hoạch
                            foreach (MES_LOG_LAST_UPDATE lastUpdate in updateWorkPlanDetails)
                            {

                                MES_WORK_PLAN_DETAIL updatePlanDetail = null;
                                string _workPlanId = "";

                                MES_WORK_PLAN_DETAIL newItem = _dbContext.MES_WORK_PLAN_DETAIL.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == lastUpdate.OBJECT_ID);
                                int _status = newItem.STATUS;
                                //Lấy NewItem kiểu từ Logs --> Vì Save vào nó lại bị mất
                                List<MES_WORK_PLAN_DETAIL_HISTORY> _historyList = _dbContext.MES_WORK_PLAN_DETAIL_HISTORY.Where(x => x.WORK_PLAN_DETAIL_ID == lastUpdate.OBJECT_ID && x.UPDATED > _LastTimeReload).ToList();
                                MES_WORK_PLAN_DETAIL_HISTORY _history = null;
                                if (_historyList.Count > 0)
                                {
                                    _history = _historyList.LastOrDefault();
                                }
                                if (_history != null)
                                {
                                    newItem = GetWorkPlanDetailByHistory(_history);
                                    newItem.STATUS = _status;
                                }

                                //Nếu là xóa thì phải tìm xem thằng nào chứa nó thì xóa đi
                                if (lastUpdate.UPDATE_EVENT == "DELETE")
                                {
                                    _Logger.Write(_LogCategory, $"Delete WorkPlanDetail ID = [{newItem.WORK_PLAN_DETAIL_ID}] at Line {newItem.LINE_ID}", LogType.Info);
                                    //Xiên hết sạch luôn
                                    WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID == newItem.WORK_PLAN_ID);
                                    if (workPlan != null)
                                    {
                                        if (workPlan.STATUS == (byte)PLAN_STATUS.Proccessing)
                                        {
                                            //Xóa trong reportLine
                                            RemoveWorkPlanDetail(newItem.LINE_ID, newItem.WORK_PLAN_DETAIL_ID);
                                        }
                                    }
                                    continue;
                                }

                                //Giờ thì làm đến thằng thêm/sửa
                                Line updatedLine = null;

                                if (newItem != null)
                                {
                                    _workPlanId = newItem.WORK_PLAN_ID;
                                    WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID== _workPlanId);
                                    string _lineId = newItem.LINE_ID;

                                    if (workPlan == null)
                                    {
                                        //Trường hợp chưa có, chưa chạy
                                        MES_WORK_PLAN tblWorkPlan = _dbContext.MES_WORK_PLAN.FirstOrDefault(x => x.WORK_PLAN_ID == _workPlanId);
                                        if (workPlan == null)
                                        {
                                            if (_AutoAddWorkPlan)
                                            {
                                                workPlan = CreateWorkPlan(_lineId, eventTime);
                                            }
                                        }

                                        if (tblWorkPlan != null)
                                        {
                                            workPlan = new WorkPlan().Cast(tblWorkPlan);

                                            if (workPlan.STATUS == (byte)PLAN_STATUS.Draft)
                                            {
                                                workPlan.STATUS = (byte)PLAN_STATUS.NotStart;
                                            }
                                            Shift shift = CheckShift(tblWorkPlan.DAY, tblWorkPlan.SHIFT_ID);
                                            workPlan.PlanStart = shift.Start;
                                            workPlan.PlanFinish = shift.Finish;

                                            workPlan.WorkPlanDetails.Add(newItem);

                                            _WorkPlans.Add(workPlan);
                                        }

                                        //Lần sau tự tính toán và khởi tạo chạy sau
                                        continue;
                                    }

                                    //Trường hợp đã có rồi nhưng sửa hoặc thêm mới!
                                    updatedLine = _Lines.FirstOrDefault(x => x.LINE_ID == workPlan.LINE_ID);
                                    updatePlanDetail = newItem;

                                    //Chỗ này kiểm tra xem có chưa để thêm vào

                                    MES_WORK_PLAN_DETAIL checkItem = workPlan.WorkPlanDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == updatePlanDetail.WORK_PLAN_DETAIL_ID);
                                    if (checkItem != null)
                                    {
                                        workPlan.WorkPlanDetails.Remove(updatePlanDetail);
                                        _Logger.Write(_LogCategory, $"Update WorkPlanDetail ID = [{updatePlanDetail.WORK_PLAN_DETAIL_ID}] at Line {updatedLine.LINE_ID}", LogType.Info);
                                    }
                                    else
                                    {
                                        _Logger.Write(_LogCategory, $"Add WorkPlanDetail ID = [{updatePlanDetail.WORK_PLAN_DETAIL_ID}] at Line {updatedLine.LINE_ID}", LogType.Info);
                                    }
                                    //Add vào rồi trước đã, rồi xử lý sau
                                    workPlan.WorkPlanDetails.Add(updatePlanDetail);

                                    //Đang chạy thì khởi tạo vào chạy luôn
                                    if (workPlan.STATUS == (byte)PLAN_STATUS.Proccessing)
                                    {
                                        AddWorkPlanDetail2Time(updatedLine.WorkPlan, updatePlanDetail);
                                    }

                                }

                            }
                        }
                        #endregion

                    }

                    //Đọc xong gán lại mới
                    _LastTimeReload = DateTime.Now;

                    _dbContext.SaveChanges();
                    _dbContext.Configuration.AutoDetectChangesEnabled = true;

                    //Reload Product --> trường hợp NOT FOUND thì update
                    ReloadProducts();

                }
                //Reload dữ liệu mới nhập
                ReloadUpdateConfig();

                //Reload dữ liệu sự kiện stop vừa điều chỉnh
                ReloadEvents();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process Reload Error: {ex}", LogType.Error);
            }
        }

        /// <summary>
        /// Định kỳ kiểm tra để khởi chạy kế hoạch, tính toán dữ liệu trong quá trình chạy chuyền, kết thúc chuyền
        /// </summary>
        private void ProccessWork()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);
                //List<decimal> workPlanInLine = new List<decimal>();
                //int _count = 0;
                foreach (Line line in _Lines)
                {
                    //Chưa chạy thì kiểm tra xem có đến giờ chạy rồi hay không
                    if (line.WorkPlan == null)
                    {
                        StartRunningWorkPlan(line.LINE_ID, eventTime);
                    }


                    //Đang chạy thì kiểm tra để tính toán giá trị
                    if (line.WorkPlan != null)
                    {
                        //Nếu DONE rồi thì chờ tính ca mới
                        if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Done)
                        {
                            StartRunningWorkPlan(line.LINE_ID, eventTime);
                        }

                        //workPlanInLine.Add(line.WorkPlan.Id);
                        //_Logger.Write(_LogCategory, $"Have WorkPlan {line.WorkPlan.Id} with status {line.WorkPlan.Status} on Line {line.Name}", LogType.Debug);
                        if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                        {
                            //Cập nhật giá trị của WorkPlan
                            CalculateWorkPlanFactor(line.LINE_ID, eventTime);

                            //Kiểm tra xem đã dừng hay chưa
                            //if (eventTime > line.WorkPlan.FinishETA && line.WorkPlan.ActualQuantity >= line.WorkPlan.PlanQuantity)

                            if (eventTime >= line.WorkPlan.PlanFinish)
                            {
                                //Kết thúc
                                FinishWorkPlan(line.LINE_ID, line.WorkPlan.PlanFinish);
                                //Finish xong thì phải tính toán lại 1 lần nữa. Lần sau ko cần tính toán lại
                                if (line.ReportLine != null)
                                {
                                    CalculateWorkPlanFactor(line.LINE_ID, line.ReportLine.PLAN_FINISH);
                                }

                            }
                        }
                        //Nhỡ Finish xong thì nó lại thành NULL nên phải check
                        if (line.WorkPlan != null)
                        {
                            WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID);
                            workPlan.STATUS = line.WorkPlan.STATUS;
                            workPlan.Priority = line.WorkPlan.Priority;
                        }
                    }

                }

                //Kiểm tra thêm các WorkPlan quá hạn
                foreach(WorkPlan workPlan in _WorkPlans)
                {
                    if (workPlan.PlanFinish < eventTime)
                    {
                        //Chỉ cái nào chưa được chạy mới xem là quá hạn
                        if (workPlan.STATUS == (int)PLAN_STATUS.NotStart)
                        {
                            //_Logger.Write(_LogCategory, $"Check Workplan timeout: {workPlan.Id} - PlanFinish: {workPlan.PlanFinish}", LogType.Debug);
                            workPlan.STATUS = (int)PLAN_STATUS.Timeout;
                            workPlan.Priority = 1;//Đánh dấu để xóa

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Work Error: {ex}", LogType.Error);
            }
       
        }
        private void ProccessSync()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                foreach (Line line in _Lines)
                {
                    if (!line.ACTIVE) continue;

                    //2024-07-15: Bổ sung logic không có WorkPlan thì cũng bỏ qua
                    if (line.WorkPlan == null) continue;
                    if (line.WorkPlan.STATUS != (int)PLAN_STATUS.Proccessing) continue;

                    _Logger.Write(_LogCategory, $"Start Sync PMS for Line {line.LINE_CODE}", LogType.Debug);
                    PMSData linePMS = null;
                    try
                    {
                        linePMS = GetPMSInfo(line.LINE_CODE);

                        if (linePMS == null) continue;
                        //Xử lý dữ liệu PMS tại đây 
                        if (linePMS != null)
                        {
                            //Chỉ lấy những ông Running, thứ khác bỏ qua
                            if (linePMS.status != "Running") continue;

                            //Check thời gian
                            if (linePMS.lastproductiontime == null) continue;
                            if (linePMS.actualquantity == 0) continue;

                            DateTime _lastProductionTime = DateTime.Parse(linePMS.lastproductiontime);
                            if (_lastProductionTime < eventTime.AddSeconds(0 - _FixTimeProduction)) continue;

                            //Vào đây là phải chạy rồi
                            if (line.WorkPlan != null)
                            {
                                //DateTime _lastProductionTime = DateTime.ParseEx(linePMS.lastproductiontime);
                                //Check thời gian
                                if (_lastProductionTime <= line.WorkPlan.PlanStart) continue;

                                if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                                {
                                    bool test = false;
                                    MES_REPORT_LINE_DETAIL detail = line.ReportLineDetails.FirstOrDefault(x => x.STATUS == (int)PLAN_STATUS.Proccessing);

                                    if (detail != null)
                                    {
                                        //Nếu có trong listDetail rồi thì thêm vào
                                        if (detail.WORK_ORDER_CODE == linePMS.ponumber.ToString() && detail.WORK_ORDER_PLAN_CODE == linePMS.planid.ToString())
                                        {
                                            //2024-07-15: Bỏ qua logic chờ lâu thì stop, mà để kệ

                                            //Kiểm tra nếu quá X phút ko có sản phẩm nào thì kết thúc
                                            //if (_lastProductionTime < eventTime.AddSeconds(0 - _TimeProduction2Stop))
                                            //{
                                            //    //Đợi lâu quá thì stop
                                            //    FinishReportLineDetail(line.LINE_ID, detail.REPORT_LINE_DETAIL_ID, eventTime, Consts.EVENTDEF_NOPLAN);
                                            //}
                                            //else
                                            //{
                                                _Logger.Write(_LogCategory, $"UPDATE Actual from PMS for Line {line.LINE_ID}: {linePMS.actualquantity}", LogType.Debug);
                                                //Đúng rồi thằng đang chạy rồi, thêm vào thôi
                                                detail.FINISH_AT = (decimal)linePMS.actualquantity;
                                                detail.ACTUAL_QUANTITY = detail.FINISH_AT - detail.START_AT + 1;
                                                detail.FINISHED = eventTime;

                                                if (detail.ACTUAL_QUANTITY == detail.PLAN_QUANTITY)
                                                {
                                                    FinishReportLineDetail(line.LINE_ID, detail.REPORT_LINE_DETAIL_ID, eventTime, Consts.EVENTDEF_NOPLAN);
                                                }
                                                test = true;
                                            //}
                                        }
                                    }
                                    if (test) continue;

                                    //Chưa đúng --> vào kiểm tra tiếp
                                    foreach (MES_REPORT_LINE_DETAIL reportDetail in line.ReportLineDetails)
                                    {
                                        if (reportDetail.WORK_ORDER_CODE == linePMS.ponumber.ToString() && reportDetail.WORK_ORDER_PLAN_CODE == linePMS.planid.ToString())
                                        {
                                            //Nếu thay đổi số lượng thì đổi sang chạy mã này
                                            if (linePMS.actualquantity == reportDetail.FINISH_AT)
                                            {
                                                _Logger.Write(_LogCategory, $"No More Actual from PMS for Line {line.LINE_ID}: {linePMS.actualquantity}", LogType.Debug);
                                                //Chưa có gì mới, bỏ qua
                                                test = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (test) continue;
                                }
                            }


                            //Đến đây rồi nghĩa là nó phải bắt đầu 1 cái mới --> Cho chạy
                            //Khởi tạo cái mới để chạy
                            WorkPlan workPlan = line.WorkPlan;

                            //Không tạo mới WorkPlan nữa
                            //if (line.WorkPlan == null)
                            //{
                            //    workPlan = CreateWorkPlan(line.LINE_ID, eventTime);
                            //}
                            //else
                            //{
                            //    if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Done)
                            //    {
                            //        workPlan = CreateWorkPlan(line.LINE_ID, eventTime);
                            //    }
                            //}

                            if (workPlan == null) continue;

                            string _productId = "", _productCode = linePMS.productcode;
                            decimal _productCycleTime = _DefaultCycleTime;
                            int _productHeadCount = _DefaultHeadCount;

                            DM_MES_PRODUCT _product = _Products.FirstOrDefault(x => x.PRODUCT_CODE == _productCode);
                            if (_product != null)
                            {
                                _productId = _product.PRODUCT_ID;
                                _productCycleTime = _product.CYCLE_TIME;
                                _productHeadCount = _product.HEADCOUNT;
                            }

                            decimal _startQuantity = linePMS.actualquantity; // - 1;

                            MES_WORK_PLAN_DETAIL newWorkPlanDetail = new MES_WORK_PLAN_DETAIL()
                            {
                                WORK_PLAN_DETAIL_ID = GenID(),
                                WORK_PLAN_ID = workPlan.WORK_PLAN_ID,
                                LINE_ID = line.LINE_ID,
                                DAY = workPlan.DAY,
                                SHIFT_ID = workPlan.SHIFT_ID,
                                PLAN_START = _lastProductionTime.AddSeconds(0 - (double)_productCycleTime), //Lấy thời gian hoàn thành Trừ cái đầu tiên
                                PLAN_FINISH = workPlan.PlanFinish,
                                WORK_ORDER_CODE = linePMS.ponumber.ToString(),
                                WORK_ORDER_PLAN_CODE = linePMS.planid.ToString(),
                                PO_CODE = linePMS.ponumber.ToString(),
                                PRODUCT_ID = _productId,
                                PRODUCT_CODE = _productCode,
                                CONFIG_ID = "",
                                TAKT_TIME = _productCycleTime,
                                STATION_QUANTITY = 1,
                                BATCH = 1,
                                HEAD_COUNT = _productHeadCount,
                                PLAN_QUANTITY = linePMS.planquantity,
                                START_AT = _startQuantity,
                                FINISH_AT = linePMS.actualquantity,
                                DESCRIPTION = "",
                                STATUS = (int)PLAN_STATUS.Proccessing,
                            };
                            workPlan.WorkPlanDetails.Add(newWorkPlanDetail);

                            //Giờ thì kiểm tra để cho chạy
                            //if (line.WorkPlan == null)
                            //{
                            //    StartRunningWorkPlan(line.LINE_ID, eventTime, false);
                            //}
                            //else
                            //{
                            //    AddWorkPlanDetail2Time(line.WorkPlan, newWorkPlanDetail);
                            //}
                            //2024-07-15: Chỉ thêm vào cho chạy thôi, theo WorkPlan đã nhập
                            AddWorkPlanDetail2Time(line.WorkPlan, newWorkPlanDetail);

                            //Kiểm tra trong WorkOrder vào chưa? Cái này từ từ để sau tính

                        }
                    }
                    catch (Exception e)
                    {
                        _Logger.Write(_LogCategory, $"Proccess Sync at Line {line.LINE_ID} Error: {e}", LogType.Error);
                    }

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Sync Error: {ex}", LogType.Error);
            }

        }
        private void ProccessData()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                using (Entities _dbContext = new Entities())
                {
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;
                    //Save LineWorkPlan

                    foreach (Line line in _Lines)
                    {
                        if (line.WorkPlan == null)
                            continue;
                        //Nếu = DONE thì vẫn tính bình thường, sau cùng mới xiên
                        if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Proccessing || line.WorkPlan.STATUS == (int)PLAN_STATUS.Done)
                        {
                            //Cập nhật WorkPlan
                            MES_WORK_PLAN tblWorkPlan = _dbContext.MES_WORK_PLAN.FirstOrDefault(wp => wp.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID);
                            if (tblWorkPlan == null)
                            {
                                tblWorkPlan = line.WorkPlan.Cast();
                                _dbContext.MES_WORK_PLAN.Add(tblWorkPlan);
                            }
                            else
                            {
                                tblWorkPlan.STATUS = line.WorkPlan.STATUS;
                                _dbContext.Entry(tblWorkPlan).State = System.Data.Entity.EntityState.Modified;
                            }
                            _Logger.Write(_LogCategory, $"Process Data: WorkPlan {line.WorkPlan.WORK_PLAN_ID} for Line {line.LINE_ID} - Status: {line.WorkPlan.STATUS}", LogType.Debug);

                            //Check WorkPlanDetail
                            foreach (MES_WORK_PLAN_DETAIL planDetail in line.WorkPlan.WorkPlanDetails)
                            {
                                MES_WORK_PLAN_DETAIL tblWorkPlanDetail = _dbContext.MES_WORK_PLAN_DETAIL.FirstOrDefault(wp => wp.WORK_PLAN_DETAIL_ID == planDetail.WORK_PLAN_DETAIL_ID);
                                if (tblWorkPlanDetail == null)
                                {
                                    _dbContext.MES_WORK_PLAN_DETAIL.Add(planDetail);
                                }
                                else
                                {
                                    MES_REPORT_LINE_DETAIL reportDetail = line.ReportLineDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == planDetail.WORK_PLAN_DETAIL_ID);
                                    tblWorkPlanDetail.STATUS = planDetail.STATUS;
                                    if (reportDetail != null)
                                    {
                                        tblWorkPlanDetail.FINISH_AT = reportDetail.FINISH_AT;
                                    }
                                    _dbContext.Entry(tblWorkPlanDetail).State = System.Data.Entity.EntityState.Modified;
                                }
                                _Logger.Write(_LogCategory, $"Process Data: WorkPlan {line.WorkPlan.WORK_PLAN_ID} - WorkPlanDetail: {planDetail.WORK_PLAN_DETAIL_ID} - Status: {planDetail.STATUS}", LogType.Debug);
                            }
                            #region SaveReportLine

                            if (line.ReportLine != null)
                            {
                                MES_REPORT_LINE reportLine = _dbContext.MES_REPORT_LINE.FirstOrDefault(l => l.REPORT_LINE_ID == line.ReportLine.REPORT_LINE_ID);
                                if (reportLine == null)
                                {
                                    _dbContext.MES_REPORT_LINE.Add(line.ReportLine);
                                }
                                else
                                {
                                    //Đang trong quá trình thực thi
                                    reportLine.STARTED = line.ReportLine.STARTED;
                                    reportLine.FINISHED = line.ReportLine.FINISHED;

                                    reportLine.ACTUAL_DURATION = line.ReportLine.ACTUAL_DURATION;
                                    reportLine.ACTUAL_BREAK_DURATION = line.ReportLine.ACTUAL_BREAK_DURATION;
                                    reportLine.ACTUAL_STOP_DURATION = line.ReportLine.ACTUAL_STOP_DURATION;
                                    reportLine.ACTUAL_WORKING_DURATION = line.ReportLine.ACTUAL_WORKING_DURATION;

                                    reportLine.PLAN_QUANTITY = line.ReportLine.PLAN_QUANTITY;
                                    reportLine.TARGET_QUANTITY = line.ReportLine.TARGET_QUANTITY;
                                    reportLine.ACTUAL_QUANTITY = line.ReportLine.ACTUAL_QUANTITY;
                                    reportLine.ACTUAL_NG_QUANTITY = line.ReportLine.ACTUAL_NG_QUANTITY;
                                    reportLine.ACTUAL_TAKT_TIME = line.ReportLine.ACTUAL_TAKT_TIME;
                                    reportLine.ACTUAL_UPH = line.ReportLine.ACTUAL_UPH;

                                    reportLine.TIME_RATE = line.ReportLine.TIME_RATE;
                                    reportLine.PLAN_RATE = line.ReportLine.PLAN_RATE;
                                    reportLine.TARGET_RATE = line.ReportLine.TARGET_RATE;
                                    reportLine.QUALITY_RATE = line.ReportLine.QUALITY_RATE;
                                    reportLine.OEE = line.ReportLine.OEE;
                                    reportLine.RESULT = line.ReportLine.RESULT;
                                    reportLine.STATUS = line.ReportLine.STATUS;

                                    _dbContext.Entry(reportLine).State = System.Data.Entity.EntityState.Modified;

                                }
                                _Logger.Write(_LogCategory, $"Process Data: Save ReportLine - Line {line.LINE_ID} - WorkPlan {line.WorkPlan.WORK_PLAN_ID} - ReportLine {line.ReportLine.REPORT_LINE_ID}", LogType.Debug);
                            }

                            #endregion
                            #region SaveReportLineDetail

                            _Logger.Write(_LogCategory, $"Process Data: Start save report line detail: Total Detail: Line {line.LINE_ID} - Total: {line.ReportLineDetails.Count}", LogType.Debug);
                            foreach (MES_REPORT_LINE_DETAIL reportLineDetail in line.ReportLineDetails)
                            {
                                MES_REPORT_LINE_DETAIL detail = _dbContext.MES_REPORT_LINE_DETAIL.FirstOrDefault(l => l.REPORT_LINE_DETAIL_ID == reportLineDetail.REPORT_LINE_DETAIL_ID);
                                if (detail == null)
                                {
                                    _dbContext.MES_REPORT_LINE_DETAIL.Add(reportLineDetail);
                                }
                                else
                                {
                                    //Đang trong quá trình thực thi
                                    if (reportLineDetail.STATUS == (int)PLAN_STATUS.Ready2Cancel)
                                    {
                                        _Logger.Write(_LogCategory, $"Remove Detail: Line {line.LINE_ID} - ReportLineDetail: {reportLineDetail.REPORT_LINE_DETAIL_ID} - Time: {reportLineDetail.TIME_NAME}", LogType.Debug);
                                        //Xóa
                                        //_dbContext.Entry(detail).State = System.Data.Entity.EntityState.Deleted;
                                        _dbContext.MES_REPORT_LINE_DETAIL.Remove(detail);
                                    }
                                    else
                                    {
                                        detail.PLAN_UPH = reportLineDetail.PLAN_UPH;
                                        detail.PLAN_UPPH = reportLineDetail.PLAN_UPPH;
                                        //Thực thi
                                        detail.PRODUCT_ID = reportLineDetail.PRODUCT_ID;
                                        detail.PRODUCT_NAME = reportLineDetail.PRODUCT_NAME;
                                        detail.STARTED = reportLineDetail.STARTED;
                                        detail.FINISHED = reportLineDetail.FINISHED;
                                        detail.PLAN_QUANTITY = reportLineDetail.PLAN_QUANTITY;
                                        detail.ACTUAL_DURATION = reportLineDetail.ACTUAL_DURATION;
                                        detail.BREAK_DURATION = reportLineDetail.BREAK_DURATION;
                                        detail.STOP_DURATION = reportLineDetail.STOP_DURATION;
                                        detail.NUMBER_OF_STOP = reportLineDetail.NUMBER_OF_STOP;
                                        detail.TARGET_QUANTITY = reportLineDetail.TARGET_QUANTITY;
                                        detail.ACTUAL_QUANTITY = reportLineDetail.ACTUAL_QUANTITY;
                                        detail.ACTUAL_NG_QUANTITY = reportLineDetail.ACTUAL_NG_QUANTITY;
                                        detail.ACTUAL_TAKT_TIME = reportLineDetail.ACTUAL_TAKT_TIME;
                                        detail.ACTUAL_UPH = reportLineDetail.ACTUAL_UPH;
                                        detail.ACTUAL_UPPH = reportLineDetail.ACTUAL_UPPH;

                                        //2 giá trị sửa online từ web
                                        detail.RUNNING_HEAD_COUNT = reportLineDetail.RUNNING_HEAD_COUNT;
                                        detail.RUNNING_TAKT_TIME = reportLineDetail.RUNNING_TAKT_TIME;
                                        detail.RUNNING_UPH = reportLineDetail.RUNNING_UPH;
                                        detail.RUNNING_UPPH = reportLineDetail.RUNNING_UPPH;

                                        detail.START_AT = reportLineDetail.START_AT;
                                        detail.FINISH_AT = reportLineDetail.FINISH_AT;
                                        detail.PLAN_RATE = reportLineDetail.PLAN_RATE;
                                        detail.TARGET_RATE = reportLineDetail.TARGET_RATE;
                                        detail.TIME_RATE = reportLineDetail.TIME_RATE;
                                        detail.QUALITY_RATE = reportLineDetail.QUALITY_RATE;
                                        detail.OEE = reportLineDetail.OEE;
                                        detail.RESULT = reportLineDetail.RESULT;
                                        detail.STATUS = reportLineDetail.STATUS;

                                        _dbContext.Entry(detail).State = System.Data.Entity.EntityState.Modified;

                                    }

                                }

                            }

                            #endregion
  
                        }
                        #region SaveLineEvent
                        _Logger.Write(_LogCategory, $"Process Data: Save Event Line {line.LINE_ID} - Total: {line.LineEvents.Count}", LogType.Debug);
                        //Line Event
                        foreach (MES_LINE_EVENT lineEvent in line.LineEvents)
                        {
                            //tblLineEvent lineEvent = line.LineEvents.OrderBy(x => x.Start).Last(); //Bản tin sau chót
                            //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.Finish.HasValue && x.LineId == line.Id);

                            //Kiểm tra theo ID
                            MES_LINE_EVENT tblLineEvent = _dbContext.MES_LINE_EVENT.FirstOrDefault(x => x.EVENT_ID == lineEvent.EVENT_ID);
                            //Kiểm tra theo LineId, WorkPlan và EventDefId
                            //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.LineId == lineEvent.LineId && x.WorkPlanId == lineEvent.WorkPlanId && x.EventDefId == lineEvent.EventDefId && x.Start == lineEvent.Start);

                            if (tblLineEvent == null)
                            {
                                _dbContext.MES_LINE_EVENT.Add(lineEvent);
                            }
                            else
                            {
                                //Đang trong quá trình thực thi
                                //Thực thi
                                DateTime _finish = eventTime;
                                if (lineEvent.FINISH.HasValue)
                                {
                                    _finish = (DateTime)lineEvent.FINISH;
                                    tblLineEvent.FINISH = lineEvent.FINISH;
                                }

                                tblLineEvent.EVENTDEF_ID = lineEvent.EVENTDEF_ID;
                                tblLineEvent.EVENTDEF_NAME_EN = lineEvent.EVENTDEF_NAME_EN;
                                tblLineEvent.EVENTDEF_NAME_VN = lineEvent.EVENTDEF_NAME_VN;
                                tblLineEvent.EVENTDEF_COLOR = lineEvent.EVENTDEF_COLOR;

                                tblLineEvent.REPORT_LINE_DETAIL_ID = lineEvent.REPORT_LINE_DETAIL_ID;
                                tblLineEvent.PRODUCT_ID = lineEvent.PRODUCT_ID;
                                tblLineEvent.PRODUCT_CODE = lineEvent.PRODUCT_CODE;
                                tblLineEvent.PRODUCT_NAME = lineEvent.PRODUCT_NAME;
                                tblLineEvent.TOTAL_DURATION = (decimal)(_finish - lineEvent.START).TotalSeconds;

                                _dbContext.Entry(tblLineEvent).State = System.Data.Entity.EntityState.Modified;

                                //_Logger.Write(_LogCategory, $"Save event {tblLineEvent.Id} for Line {line.Id}", LogType.Debug);
                            }
                        }

                        //Node Event
                        foreach (Node node in line.Nodes)
                        {
                            foreach (MES_NODE_EVENT nodeEvent in node.NodeEvents)
                            {
                                //tblLineEvent lineEvent = line.LineEvents.OrderBy(x => x.Start).Last(); //Bản tin sau chót
                                //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.Finish.HasValue && x.LineId == line.Id);

                                //Kiểm tra theo ID
                                MES_NODE_EVENT tblNodeEvent = _dbContext.MES_NODE_EVENT.FirstOrDefault(x => x.EVENT_ID == nodeEvent.EVENT_ID);
                                //Kiểm tra theo LineId, WorkPlan và EventDefId
                                //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.LineId == lineEvent.LineId && x.WorkPlanId == lineEvent.WorkPlanId && x.EventDefId == lineEvent.EventDefId && x.Start == lineEvent.Start);

                                if (tblNodeEvent == null)
                                {
                                    _dbContext.MES_NODE_EVENT.Add(nodeEvent);
                                }
                                else
                                {
                                    //Đang trong quá trình thực thi
                                    //Thực thi
                                    DateTime _finish = eventTime;
                                    if (nodeEvent.FINISH.HasValue)
                                    {
                                        _finish = (DateTime)nodeEvent.FINISH;
                                        tblNodeEvent.FINISH = nodeEvent.FINISH;
                                    }

                                    tblNodeEvent.EVENTDEF_ID = nodeEvent.EVENTDEF_ID;
                                    tblNodeEvent.EVENTDEF_NAME_EN = nodeEvent.EVENTDEF_NAME_EN;
                                    tblNodeEvent.EVENTDEF_NAME_VN = nodeEvent.EVENTDEF_NAME_VN;
                                    tblNodeEvent.EVENTDEF_COLOR = nodeEvent.EVENTDEF_COLOR;

                                    tblNodeEvent.REPORT_LINE_DETAIL_ID = nodeEvent.REPORT_LINE_DETAIL_ID;
                                    tblNodeEvent.PRODUCT_ID = nodeEvent.PRODUCT_ID;
                                    tblNodeEvent.PRODUCT_CODE = nodeEvent.PRODUCT_CODE;
                                    tblNodeEvent.PRODUCT_NAME = nodeEvent.PRODUCT_NAME;
                                    tblNodeEvent.TOTAL_DURATION = (decimal)(_finish - nodeEvent.START).TotalSeconds;

                                    _dbContext.Entry(tblNodeEvent).State = System.Data.Entity.EntityState.Modified;

                                    //_Logger.Write(_LogCategory, $"Save event {tblLineEvent.Id} for Line {line.Id}", LogType.Debug);
                                }
                            }
                        }

                        //LineWorking
                        foreach (MES_LINE_WORKING lineWorking in line.LineWorkings)
                        {
                            //Kiểm tra theo ID
                            MES_LINE_WORKING tblLineWorking = _dbContext.MES_LINE_WORKING.FirstOrDefault(x => x.LINE_WORKING_ID == lineWorking.LINE_WORKING_ID);
                            //Kiểm tra theo LineId, WorkPlan và EventDefId
                            //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.LineId == lineEvent.LineId && x.WorkPlanId == lineEvent.WorkPlanId && x.EventDefId == lineEvent.EventDefId && x.Start == lineEvent.Start);

                            if (tblLineWorking == null)
                            {
                                _dbContext.MES_LINE_WORKING.Add(lineWorking);
                            }
                            else
                            {
                                //Đang trong quá trình thực thi
                                tblLineWorking.DURATION = lineWorking.DURATION;

                                _dbContext.Entry(tblLineWorking).State = System.Data.Entity.EntityState.Modified;
                                //_Logger.Write(_LogCategory, $"Save event {tblLineEvent.Id} for Line {line.Id}", LogType.Debug);
                            }
                        }

                        //LineSTOP
                        foreach (MES_LINE_STOP lineStop in line.LineStops)
                        {
                            //Kiểm tra theo ID
                            MES_LINE_STOP tblLineStop = _dbContext.MES_LINE_STOP.FirstOrDefault(x => x.LINE_STOP_ID == lineStop.LINE_STOP_ID);
                            //Kiểm tra theo LineId, WorkPlan và EventDefId
                            //tblLineEvent tblLineEvent = _dbContext.tblLineEvents.FirstOrDefault(x => x.LineId == lineEvent.LineId && x.WorkPlanId == lineEvent.WorkPlanId && x.EventDefId == lineEvent.EventDefId && x.Start == lineEvent.Start);

                            if (tblLineStop == null)
                            {
                                _dbContext.MES_LINE_STOP.Add(lineStop);
                            }
                            else
                            {
                                //Đang trong quá trình thực thi
                                tblLineStop.DURATION = lineStop.DURATION;

                                _dbContext.Entry(tblLineStop).State = System.Data.Entity.EntityState.Modified;
                                //_Logger.Write(_LogCategory, $"Save event {tblLineEvent.Id} for Line {line.Id}", LogType.Debug);
                            }
                        }

                        #endregion

                        _dbContext.SaveChanges();
                        _Logger.Write(_LogCategory, $"Process Data - Save Line: {line.LINE_ID} completed!", LogType.Debug);

                        //Save xong hoàn thành thì xóa bỏ
                        if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Done)
                        {
                            //Kết thúc toàn bộ những gì đang chạy tại Line này
                            WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID);
                            if (workPlan != null)
                            {
                                workPlan.Priority = 1; //Đánh dấu để xóa
                            }
                            ResetRunningLine(line.LINE_ID);
                        }
                    }
                    #region SaveWorkPlan
                    //Xử lý các kế hoạch đã quá hạn mà không được chạy
                    _Logger.Write(_LogCategory, $"Process Data: Save All WorkPlan - Total: {_WorkPlans.Count}", LogType.Debug);

                    lock (_WorkPlans)
                    {
                        foreach (WorkPlan workPlan in _WorkPlans)
                        {
                            if (workPlan.STATUS == (int)PLAN_STATUS.Proccessing || workPlan.STATUS == (int)PLAN_STATUS.Done) continue;

                            //Chỉ update những thằng mới
                            //_Logger.Write(_LogCategory, $"Save WorkPlan {workPlan.Id} with Status {workPlan.Status}", LogType.Debug);
                            MES_WORK_PLAN updateWorkPlan = _dbContext.MES_WORK_PLAN.FirstOrDefault(wp => wp.WORK_PLAN_ID == workPlan.WORK_PLAN_ID);
                            if (updateWorkPlan == null)
                            {
                                updateWorkPlan = workPlan.Cast();
                                _dbContext.MES_WORK_PLAN.Add(updateWorkPlan);
                            }
                            else
                            {
                                updateWorkPlan.STATUS = workPlan.STATUS;
                                _dbContext.Entry(updateWorkPlan).State = System.Data.Entity.EntityState.Modified;
                            }
                            _Logger.Write(_LogCategory, $"Process Data: Save WorkPlan {workPlan.WORK_PLAN_ID} - Status {workPlan.STATUS}", LogType.Debug);

                            //Cập nhật Detail
                            foreach (MES_WORK_PLAN_DETAIL tblWorkPlanDetail in workPlan.WorkPlanDetails)
                            {
                                _Logger.Write(_LogCategory, $"Process Data: Save WorkPlanDetail {tblWorkPlanDetail.WORK_PLAN_DETAIL_ID} - Status {tblWorkPlanDetail.STATUS}", LogType.Debug);

                                if (tblWorkPlanDetail.STATUS != (int)PLAN_STATUS.Ready2Cancel)
                                {
                                    tblWorkPlanDetail.STATUS = (int)workPlan.STATUS;
                                }

                                //Update Status
                                MES_WORK_PLAN_DETAIL updateWorkPlanDetail = _dbContext.MES_WORK_PLAN_DETAIL.FirstOrDefault(wp => wp.WORK_PLAN_DETAIL_ID == tblWorkPlanDetail.WORK_PLAN_DETAIL_ID);

                                if (updateWorkPlanDetail != null)
                                {
                                    if (tblWorkPlanDetail.STATUS == (int)PLAN_STATUS.Ready2Cancel)
                                    {
                                        //_dbContext.Entry(updateWorkPlanDetail).State = System.Data.Entity.EntityState.Deleted;
                                        _dbContext.MES_WORK_PLAN_DETAIL.Remove(updateWorkPlanDetail);
                                    }
                                    else
                                    {
                                        //_Logger.Write(_LogCategory, $"Save WorkPlan {workPlan.Id} with Status {workPlan.Status}", LogType.Debug);
                                        updateWorkPlanDetail.STATUS = tblWorkPlanDetail.STATUS;
                                        _dbContext.Entry(updateWorkPlanDetail).State = System.Data.Entity.EntityState.Modified;
                                    }
                                }

                            }
                        }
                    }
                    //_Logger.Write(_LogCategory, $"Process Data - Save WorkPlan", LogType.Debug);
                    _dbContext.SaveChanges();
                    _dbContext.Configuration.AutoDetectChangesEnabled = true;

                    //Remove WorkPlan
                    lock (_WorkPlans)
                    {
                        for (int i = _WorkPlans.Count - 1; i >= 0; i--)
                        {
                            WorkPlan workPlan = _WorkPlans[i];
                            //if (workPlanTimeOut.Contains(workPlan.Id))
                            if (workPlan.Priority == 1)
                            {
                                _Logger.Write(_LogCategory, $"Remove WorkPlan timeout or is deleted - Total: {_WorkPlans.Count}, Index: {i}: {workPlan.WORK_PLAN_ID}", LogType.Debug);
                                _WorkPlans.Remove(workPlan);
                            }
                        }
                    }

                    //Remove WorkPlanDetail --> Chỉ áp dụng cho những trường hợp đang chạy
                    List<WorkPlan> runningWorkPlans = _WorkPlans.Where(x => x.STATUS == (int)PLAN_STATUS.Proccessing).ToList();
                    foreach (WorkPlan plan in runningWorkPlans)
                    {
                        List<MES_WORK_PLAN_DETAIL> planDetails = plan.WorkPlanDetails;
                        for (int i = planDetails.Count - 1; i >= 0; i--)
                        {
                            MES_WORK_PLAN_DETAIL planDetail = planDetails[i];

                            if (planDetail.STATUS == (int)PLAN_STATUS.Ready2Cancel)
                            {
                                _Logger.Write(_LogCategory, $"Remove WorkPlanDetail update or cancel - Line {plan.LINE_ID} - WorkPlan {plan.WORK_PLAN_ID} - Detail {planDetail.WORK_PLAN_DETAIL_ID}", LogType.Debug);
                                lock (plan)
                                {
                                    plan.WorkPlanDetails.Remove(planDetail);
                                }
                            }
                        }
                    }

                    //Remove ReportLineDetail
                    foreach (Line line in _Lines)
                    {
                        for (int i = line.ReportLineDetails.Count - 1; i >= 0; i--)
                        {
                            MES_REPORT_LINE_DETAIL lineDetail = line.ReportLineDetails[i];
                            if (lineDetail.STATUS == (int)PLAN_STATUS.Ready2Cancel)
                            {
                                lock (line)
                                {
                                    line.ReportLineDetails.Remove(lineDetail);
                                }
                            }
                        }
                    }

                    #endregion
                }
            }
            catch (Exception exM3)
            {
                _Logger.Write(_LogCategory, $"ProccessData Error: {exM3}", LogType.Error);
            }
        }
        private void ProcessDisconnect()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                DM_MES_EVENTDEF eventDef = _EventDefs.LastOrDefault(); //Lấy thằng Disconnect
                using (Entities _dbContext = new Entities())
                {
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;

                    foreach (Line line in _Lines)
                    {

                        //foreach (Node node in line.Nodes)
                        //{
                        //    double timeDuration = (eventTime - (DateTime)node.Last_Received).TotalSeconds;

                        //    //Đúng là Disconnect thật
                        //    if (timeDuration > _DisconnectedTime)
                        //    {

                        //        tblEvent nodeEvent = node.Events.FirstOrDefault(x => !x.T1.HasValue);
                        //        //Chưa có sự kiện thì thêm mới
                        //        if (nodeEvent == null)
                        //        {
                        //            tblEvent newEvent = new tblEvent()
                        //            {
                        //                NodeId = node.Id,
                        //                EventDefId = eventDef.Id,
                        //                T3 = node.Last_Received,
                        //            };
                        //            _dbContext.tblEvents.Add(newEvent);
                        //            node.Events.Add(newEvent);
                        //        }
                        //        else
                        //        {
                        //            //Nếu đã có sự kiện đó
                        //            if (nodeEvent.EventDefId == eventDef.Id)
                        //            {
                        //                //Vẫn tiếp diễn --> Bỏ qua
                        //            }
                        //            else
                        //            {
                        //                //Đã đổi sự kiện
                        //                //Kết thúc thằng cũ và tạo thằng mới

                        //                nodeEvent.T1 = node.Last_Received;
                        //                tblEvent oldEvent = _dbContext.tblEvents.FirstOrDefault(x => x.NodeId == nodeEvent.NodeId && !x.T1.HasValue);
                        //                oldEvent.T1 = node.Last_Received;
                        //                _dbContext.Entry(oldEvent).State = System.Data.Entity.EntityState.Modified;

                        //                tblEvent newEvent = new tblEvent()
                        //                {
                        //                    NodeId = node.Id,
                        //                    EventDefId = eventDef.Id,
                        //                    T3 = node.Last_Received,
                        //                };
                        //                _dbContext.tblEvents.Add(newEvent);
                        //                node.Events.Add(newEvent);
                        //            }
                        //        }

                        //    }
                        //}

                    }
                    _dbContext.SaveChanges();
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(Consts.LOG_CATEGORY, $"Check Disconnect Error: {ex}", LogType.Error);
            }

        }
        private void ProcessCleanDataLive()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    //_dbContext.Database.CommandTimeout = 120;
                    ////Clean Raw Data
                    //DateTime timeToClear = DateTime.Now.AddDays(0 - _DataLiveTime);
                    //decimal _dayToClear = Time2Num(timeToClear, DayArchive);
                    //List<tblRawData> rawData = _dbContext.tblRawDatas.Where(d => d.Day < _dayToClear).ToList();

                    //_dbContext.tblRawDatas.RemoveRange(rawData);

                    //_dbContext.SaveChanges();

                    //_Logger.Write(_LogCategory, $"Cleaned Live Data before {_DataLiveTime} days!", LogType.Info);
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Clean Live Data Error: {ex}", LogType.Error);
            }

        }
        private void ProcessDisplay()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;
                    foreach (Line line in _Lines)
                    {
                        //if (line.WorkPlan == null) continue;
                        //if (line.ReportLine == null) continue;
                        //if (line.ReportLineDetails.Count == 0) continue;

                        DateTime eventTime = DateTime.Now;
                        //Lấy dữ liệu tổng để bắn vào

                        //Lấy trước 1 thằng thôi
                        MES_REPORT_LINE_DETAIL detail = null;
                        if (line.ReportLineDetails.Count > 0)
                        {
                            detail = line.ReportLineDetails.FirstOrDefault(x => x.STATUS == (int)PLAN_STATUS.Proccessing);
                            if (detail == null)
                            {
                                detail = line.ReportLineDetails.LastOrDefault();
                            }
                        }
                        if (detail == null)
                        {
                            detail = new MES_REPORT_LINE_DETAIL();
                        }
                        string _productId = "", _productCode = "", _productName = "", _productCategoryCode = "", _productCategoryName = "";
                        string _productCategoryId = "";

                        //Line
                        #region Line
                        if (detail.PRODUCT_ID != "")
                        {
                            DM_MES_PRODUCT _product = _Products.FirstOrDefault(x => x.PRODUCT_ID == detail.PRODUCT_ID);
                            if (_product != null)
                            {
                                _productId = _product.PRODUCT_ID;
                                _productCode = _product.PRODUCT_CODE;
                                _productName = _product.PRODUCT_NAME;
                                _productCategoryId = _product.PRODUCT_CATEGORY_ID;
                                DM_MES_PRODUCT_CATEGORY _productCategory = _ProductCategories.FirstOrDefault(x => x.CATEGORY_ID == _product.PRODUCT_CATEGORY_ID);
                                if (_productCategory != null)
                                {
                                    _productCategoryId = _productCategory.CATEGORY_ID;
                                    _productCategoryCode = _productCategory.CATEGORY_CODE;
                                    _productCategoryName = _productCategory.CATEGORY_NAME;
                                }
                            }
                        }

                        decimal _actualStopDuration = 0;
                        int _numberOfStop = 0;
                        if (line.ReportLine != null)
                        {
                            _actualStopDuration = line.ReportLine.ACTUAL_STOP_DURATION;
                            _numberOfStop = line.ReportLine.NUMBER_OF_STOP;
                        }

                        //Tính thời gian dịch chuyển
                        if (line.WorkPlan != null)
                        {
                            double _duration = (eventTime - line.Changed).TotalSeconds;
                            if (_duration > _AutoSwitchWorkPlanInterval)
                            {
                                line.CurrentDetail++;
                                if (line.CurrentDetail > line.WorkPlan.WorkPlanDetails.Count)
                                {
                                    line.CurrentDetail = 1;
                                }
                                line.Changed = eventTime;
                            }
                        }
                        else
                        {
                            line.CurrentDetail = 0;
                        }

                        MES_MSG_LINE msgLine = _dbContext.MES_MSG_LINE.FirstOrDefault(l => l.LINE_ID == line.LINE_ID);
                        if (msgLine == null)
                        {
                            //Get Total Plan of Detail
                            //decimal _totalPlanQuantity = line.ReportLine.PLAN_QUANTITY;
                            //MES_WORK_PLAN_DETAIL planDetail = line.WorkPlan.WorkPlanDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == detail.WORK_PLAN_DETAIL_ID);
                            //if (planDetail != null)
                            //{
                            //    _totalPlanQuantity = planDetail.PLAN_QUANTITY;
                            //}
                            msgLine = new MES_MSG_LINE()
                            {
                                LINE_ID = line.LINE_ID,
                                LINE_NAME = line.LINE_NAME,
                                FACTORY_ID = line.FACTORY_ID,
                                FACTORY_NAME = line.Factory_Name,
                                EVENTDEF_ID = line.EventDefId,
                                EVENTDEF_NAME_VN = line.EventDefName_VN,
                                EVENTDEF_NAME_EN = line.EventDefName_EN,
                                EVENTDEF_COLOR = line.EventDefColor,
                                PRODUCT_ID = _productId,
                                PRODUCT_CODE = _productCode,
                                PRODUCT_NAME = _productName,
                                PRODUCT_CATEGORY_ID = _productCategoryId,
                                PRODUCT_CATEGORY_CODE = _productCategoryCode,
                                PRODUCT_CATEGORY_NAME = _productCategoryName,
                                HEAD_COUNT = detail.RUNNING_HEAD_COUNT,
                                TAKT_TIME = detail.RUNNING_TAKT_TIME,
                                TOTAL_PLAN_QUANTITY = detail.TOTAL_PLAN_QUANTITY,
                                PLAN_QUANTITY = detail.PLAN_QUANTITY,
                                TARGET_QUANTITY = detail.TARGET_QUANTITY,
                                ACTUAL_QUANTITY = detail.ACTUAL_QUANTITY,
                                ACTUAL_NG_QUANTITY = detail.ACTUAL_NG_QUANTITY,
                                STOP_DURATION = detail.STOP_DURATION,
                                TOTAL_STOP_DURATION = _actualStopDuration,
                                NUMBER_OF_STOP = _numberOfStop,
                                PLAN_RATE = detail.PLAN_RATE,
                                TARGET_RATE = detail.TARGET_RATE,
                                TIME_RATE = detail.TIME_RATE,
                                QUALITY_RATE = detail.QUALITY_RATE,
                                OEE = detail.OEE,
                                CURRENT_DETAIL = line.CurrentDetail,
                                TIME_UPDATED = eventTime,
                            };
                            _dbContext.MES_MSG_LINE.Add(msgLine);
                        }
                        else
                        {

                            msgLine.EVENTDEF_ID = line.EventDefId;
                            msgLine.EVENTDEF_NAME_VN = line.EventDefName_VN;
                            msgLine.EVENTDEF_NAME_EN = line.EventDefName_EN;
                            msgLine.EVENTDEF_COLOR = line.EventDefColor;
                            msgLine.PRODUCT_ID = _productId;
                            msgLine.PRODUCT_CODE = _productCode;
                            msgLine.PRODUCT_NAME = _productName;
                            msgLine.PRODUCT_CATEGORY_ID = _productCategoryId;
                            msgLine.PRODUCT_CATEGORY_CODE = _productCategoryCode;
                            msgLine.PRODUCT_CATEGORY_NAME = _productCategoryName;
                            msgLine.HEAD_COUNT = detail.RUNNING_HEAD_COUNT;
                            msgLine.TAKT_TIME = detail.RUNNING_TAKT_TIME;
                            msgLine.TOTAL_PLAN_QUANTITY = detail.TOTAL_PLAN_QUANTITY;
                            msgLine.PLAN_QUANTITY = detail.PLAN_QUANTITY;
                            msgLine.TARGET_QUANTITY = detail.TARGET_QUANTITY;
                            msgLine.ACTUAL_QUANTITY = detail.ACTUAL_QUANTITY;
                            msgLine.ACTUAL_NG_QUANTITY = detail.ACTUAL_NG_QUANTITY;
                            msgLine.STOP_DURATION = detail.STOP_DURATION;
                            msgLine.TOTAL_STOP_DURATION = _actualStopDuration;
                            msgLine.NUMBER_OF_STOP = _numberOfStop;
                            msgLine.PLAN_RATE = detail.PLAN_RATE;
                            msgLine.TARGET_RATE = detail.TARGET_RATE;
                            msgLine.TIME_RATE = detail.TIME_RATE;
                            msgLine.QUALITY_RATE = detail.QUALITY_RATE;
                            msgLine.OEE = detail.OEE;
                            msgLine.CURRENT_DETAIL = line.CurrentDetail;
                            msgLine.TIME_UPDATED = eventTime;
                            _dbContext.Entry(msgLine).State = System.Data.Entity.EntityState.Modified;
                        }
                        #endregion

                        //LineRunning
                        #region LineWorking
                        List<MES_MSG_LINE_WORKING> msgLineWorkings = _dbContext.MES_MSG_LINE_WORKING.Where(l => l.LINE_ID == line.LINE_ID).ToList();
                        if (msgLineWorkings.Count > 0)
                        {
                            _dbContext.MES_MSG_LINE_WORKING.RemoveRange(msgLineWorkings);
                        }
                        foreach (MES_LINE_WORKING lineRunningWSS in line.LineWorkings)
                        {
                            MES_MSG_LINE_WORKING msgLineRunning = new MES_MSG_LINE_WORKING()
                            {
                                ID = lineRunningWSS.LINE_WORKING_ID,
                                LINE_ID = lineRunningWSS.LINE_ID,
                                EVENTDEF_ID = lineRunningWSS.EVENTDEF_ID,
                                EVENTDEF_NAME_EN = lineRunningWSS.EVENTDEF_NAME_EN,
                                EVENTDEF_NAME_VN = lineRunningWSS.EVENTDEF_NAME_VN,
                                EVENTDEF_COLOR = lineRunningWSS.EVENTDEF_COLOR,
                                DURATION = lineRunningWSS.DURATION,

                            };
                            _dbContext.MES_MSG_LINE_WORKING.Add(msgLineRunning);
               
                        }
                        #endregion

                        //LineSTOP
                        #region LineStop
                        List<MES_MSG_LINE_STOP> msgLineStops = _dbContext.MES_MSG_LINE_STOP.Where(l => l.LINE_ID == line.LINE_ID).ToList();
                        if (msgLineStops.Count > 0)
                        {
                            _dbContext.MES_MSG_LINE_STOP.RemoveRange(msgLineStops);
                        }

                        foreach (MES_LINE_STOP lineStop in line.LineStops)
                        {
                            MES_MSG_LINE_STOP msgLineStop = new MES_MSG_LINE_STOP()
                            {
                                ID = lineStop.LINE_STOP_ID,
                                LINE_ID = lineStop.LINE_ID,
                                EVENTDEF_ID = lineStop.EVENTDEF_ID,
                                EVENTDEF_NAME_EN = lineStop.EVENTDEF_NAME_EN,
                                EVENTDEF_NAME_VN = lineStop.EVENTDEF_NAME_VN,
                                EVENTDEF_COLOR = lineStop.EVENTDEF_COLOR,
                                DURATION = lineStop.DURATION,
                                REASON_ID = lineStop.REASON_ID,
                                REASON_NAME_EN = lineStop.REASON_NAME_EN,
                                REASON_NAME_VN = lineStop.REASON_NAME_VN,

                            };
                            _dbContext.MES_MSG_LINE_STOP.Add(msgLineStop);
                        }
                        #endregion

                        //LineEvent
                        #region LineEvent
                        List<MES_MSG_LINE_EVENT> msgLineEvents = _dbContext.MES_MSG_LINE_EVENT.Where(l => l.LINE_ID == line.LINE_ID).ToList();
                        if (msgLineEvents.Count > 0)
                        {
                            _dbContext.MES_MSG_LINE_EVENT.RemoveRange(msgLineEvents);
                        }

                        foreach (MES_LINE_EVENT lineEventWSS in line.LineEvents)
                        {
                            DateTime _finishEvent = eventTime;
                            if (lineEventWSS.FINISH.HasValue) _finishEvent = (DateTime)lineEventWSS.FINISH;

                            MES_MSG_LINE_EVENT msgLineEvent = new MES_MSG_LINE_EVENT()
                            {
                                LINE_ID = line.LINE_ID,
                                EVENT_ID = lineEventWSS.EVENT_ID,
                                EVENTDEF_ID = lineEventWSS.EVENTDEF_ID,
                                EVENTDEF_NAME_EN = lineEventWSS.EVENTDEF_NAME_EN,
                                EVENTDEF_NAME_VN = lineEventWSS.EVENTDEF_NAME_VN,
                                EVENTDEF_COLOR = lineEventWSS.EVENTDEF_COLOR,
                                START = lineEventWSS.START,
                                RESPONSE = lineEventWSS.RESPONSE,
                                FINISH = _finishEvent,
                            };
                            _dbContext.MES_MSG_LINE_EVENT.Add(msgLineEvent);

                        }

                        //Thêm đoạn cuối cùng ????
                        if (_AddEventUntilFinish)
                        {
                            if (line.Shift != null)
                            {
                                DateTime _finish = line.Shift.Finish;
                                if (line.WorkPlan != null) _finish = line.WorkPlan.PlanFinish;

                                MES_MSG_LINE_EVENT msgLineEvent = new MES_MSG_LINE_EVENT()
                                {
                                    LINE_ID = line.LINE_ID,
                                    EVENT_ID = GenID(),
                                    EVENTDEF_ID = Consts.EVENTDEF_DEFAULT,
                                    EVENTDEF_NAME_EN = Consts.EVENTDEF_DEFAULT_NAME_EN,
                                    EVENTDEF_NAME_VN = Consts.EVENTDEF_DEFAULT_NAME_VN,
                                    EVENTDEF_COLOR = "#000",
                                    START = eventTime.AddSeconds(1),
                                    RESPONSE = eventTime.AddSeconds(1),
                                    FINISH = _finish,
                                };
                                _dbContext.MES_MSG_LINE_EVENT.Add(msgLineEvent);
                            }
                        }    

                        #endregion

                        //LineDetail
                        #region LineDetail
                        //Lấy danh sách thuộc Line này về đã
                        List<MES_MSG_LINE_DETAIL> msgLineDetails = _dbContext.MES_MSG_LINE_DETAIL.Where(l => l.LINE_ID == line.LINE_ID).ToList();
                        if (msgLineDetails.Count > 0)
                        {
                            _dbContext.MES_MSG_LINE_DETAIL.RemoveRange(msgLineDetails);
                        }

                        foreach (MES_REPORT_LINE_DETAIL lineDetailWSS in line.ReportLineDetails)
                        {
                            MES_MSG_LINE_DETAIL msgLineDetail = new MES_MSG_LINE_DETAIL()
                            {
                                REPORT_LINE_DETAIL_ID = lineDetailWSS.REPORT_LINE_DETAIL_ID,
                                LINE_ID = line.LINE_ID,
                                STARTED = lineDetailWSS.STARTED,
                                FINISHED = lineDetailWSS.FINISHED,
                                TIME_NAME = lineDetailWSS.TIME_NAME,
                                PRODUCT_ID = lineDetailWSS.PRODUCT_ID,
                                PRODUCT_CODE = lineDetailWSS.PRODUCT_CODE,
                                PRODUCT_NAME = lineDetailWSS.PRODUCT_NAME,
                                PLAN_QUANTITY = lineDetailWSS.PLAN_QUANTITY,
                                HEAD_COUNT = lineDetailWSS.RUNNING_HEAD_COUNT,
                                TAKT_TIME = lineDetailWSS.RUNNING_TAKT_TIME,
                                TARGET_QUANTITY = lineDetailWSS.TARGET_QUANTITY,
                                ACTUAL_QUANTITY = lineDetailWSS.ACTUAL_QUANTITY,
                                ACTUAL_NG_QUANTITY = lineDetailWSS.ACTUAL_NG_QUANTITY,
                                PLAN_RATE = lineDetailWSS.PLAN_RATE,
                                TARGET_RATE = lineDetailWSS.TARGET_RATE,
                                QUALITY_RATE = lineDetailWSS.QUALITY_RATE,
                                TIME_RATE = lineDetailWSS.TIME_RATE,
                                OEE = lineDetailWSS.OEE,
                                INDEX_DETAIL = lineDetailWSS.DETAIL_INDEX,
                                STATUS = lineDetailWSS.STATUS,
                                STATUS_NAME = lineDetailWSS.RESULT,
                                TIME_UPDATED = eventTime,
                            };
                            _dbContext.MES_MSG_LINE_DETAIL.Add(msgLineDetail);

                        }
                        #endregion

                        //LineProduct
                        #region LineProduct
                        List<MES_MSG_LINE_PRODUCT> msgLineProducts = _dbContext.MES_MSG_LINE_PRODUCT.Where(x => x.LINE_ID == line.LINE_ID).ToList();
                        if (msgLineProducts.Count > 0)
                        {
                            _dbContext.MES_MSG_LINE_PRODUCT.RemoveRange(msgLineProducts);
                        }
                        List<MES_REPORT_LINE_DETAIL> reportDetails = new List<MES_REPORT_LINE_DETAIL>(line.ReportLineDetails);
                        while (reportDetails.Count > 0)
                        {
                            MES_REPORT_LINE_DETAIL item = reportDetails.FirstOrDefault();
                            string productId = item.PRODUCT_ID;

                            List<MES_REPORT_LINE_DETAIL> productDetails = reportDetails.Where(x => x.PRODUCT_ID == productId).ToList();

                            MES_MSG_LINE_PRODUCT lineProduct = new MES_MSG_LINE_PRODUCT()
                            {
                                ID = GenID(),
                                LINE_ID = line.LINE_ID,
                                PRODUCT_ID = productId,
                                PRODUCT_CODE = item.PRODUCT_CODE,
                                PRODUCT_NAME = item.PRODUCT_NAME,
                                PLAN_QUANTITY = productDetails.Sum(x => x.PLAN_QUANTITY),
                                HEAD_COUNT = (int)productDetails.Average(x => x.RUNNING_HEAD_COUNT),
                                TAKT_TIME = Math.Round(productDetails.Average(x => x.RUNNING_TAKT_TIME), 1),
                                TARGET_QUANTITY = productDetails.Sum(x => x.TARGET_QUANTITY),
                                ACTUAL_QUANTITY = productDetails.Sum(x => x.ACTUAL_QUANTITY),
                                ACTUAL_NG_QUANTITY = productDetails.Sum(x => x.ACTUAL_NG_QUANTITY),
                                PLAN_RATE = Math.Round(productDetails.Average(x => x.PLAN_RATE), 1),
                                TARGET_RATE = Math.Round(productDetails.Average(x => x.TARGET_RATE), 1),
                                QUALITY_RATE = Math.Round(productDetails.Average(x => x.QUALITY_RATE), 1),
                                TIME_RATE = Math.Round(productDetails.Average(x => x.TIME_RATE), 1),
                                OEE = Math.Round(productDetails.Average(x => x.OEE),1),
                                TIME_UPDATED = eventTime,
                            };
                            _dbContext.MES_MSG_LINE_PRODUCT.Add(lineProduct);

                            reportDetails.RemoveAll(x => x.PRODUCT_ID == productId);
                        }
                        #endregion


                    }
                    _dbContext.SaveChanges();
                    _dbContext.Configuration.AutoDetectChangesEnabled = true;
                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Save Current Status to Database Error: {ex}", LogType.Error);
            }
        }
        private void ProcessSendControlMessage()
        {
            try
            {
                if (!_isSendControlMessage) return;

                //PreProcessMessage();

                DateTime eventTime = DateTime.Now;

                foreach (Line line in _Lines)
                {
                    //Kiểm tra xem tồn tại kế hoạch không
                    if (line.WorkPlan == null) continue;

                    //Nếu chuyền không chạy thì cũng thôi không tính
                    if (line.WorkPlan.STATUS != (byte)PLAN_STATUS.Proccessing) continue;

                    //Giờ mới xem trạng thái và gửi lệnh điều khiển đèn
                    int in01 = 0, in02 = 0, in03 = 0;
                    if (line.EventDefId != Consts.EVENTDEF_DEFAULT && line.EventDefId != Consts.EVENTDEF_NOPLAN && line.EventDefId != Consts.EVENTDEF_BREAK)
                    {
                        if (line.EventDefId == Consts.EVENTDEF_RUNNING)
                        {
                            in01 = 1;
                        }
                        else
                        {
                            if (line.EventDefId == Consts.EVENTDEF_STOP)
                            {
                                in02 = 1;
                            }
                            else
                            {
                                in03 = 1;
                            }
                        }
                    }
                    int NodeId = 1;
                    try
                    {
                        NodeId = int.Parse(line.LINE_ID);
                    }
                    catch(Exception x) { }

                    iAndon.MSG.DSV_MSG message = new DSV_MSG("SERVER", DateTime.Now, MessageType.Andon, NodeId, in01, in02, in03);

                    //Send to Rabbit

                    if (_EventBus == null)
                    {
                        // try connect to rabbitmq
                        ConnectRabbitMQ();
                    }

                    if (!_EventBus.IsConnected)
                    {
                        // try connect to rabbitmq
                        ConnectRabbitMQ();
                    }

                    if (_EventBus != null && _EventBus.IsConnected)
                    {
                        _EventBus.Publish<DSV_MSG>(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Message Error: {ex}", LogType.Error);
            }
        }

        #endregion

        #region WorkPlanProcess
        private void StartRunningWorkPlan(string LineId, DateTime eventTime)
        {
            try
            {
                ReloadConfigurations(); //Tải lại cấu hình (Shift/Break/Product)

                //_Logger.Write(_LogCategory, $"No WorkPlan --> Check for running workplan!", LogType.Debug);

                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);

                DateTime checktime = eventTime;

                if (checktime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    checktime = checktime.AddDays(-1);
                }

                decimal _day = Time2Num(checktime, DayArchive);
                List<Shift> lstShift = CheckShiftList(eventTime);

                //Shift shift = CheckShift(eventTime);
                if (lstShift == null)
                {
                    _Logger.Write(_LogCategory, $"NO Shift match for {line.LINE_ID} - Day {_day}!", LogType.Debug);
                    return;
                }
                //Loại bỏ cái cũ
                if (line.WorkPlan != null)
                {
                    //Kiểm tra nếu nó còn dính cái cũ thì cho DONE và xóa bỏ đi
                    WorkPlan workPlanOld = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID);
                    if (workPlanOld != null)
                    {
                        workPlanOld.STATUS = (int)PLAN_STATUS.Done;
                        //Xóa bỏ cái cũ nếu có
                        workPlanOld.Priority = 1;//Đánh dấu xóa
                    }
                }

                WorkPlan workPlan = null;
                Shift _currentShift = null;
                foreach (Shift shift in lstShift)
                {
                    _Logger.Write(_LogCategory, $"Check for WorkPlan at {line.LINE_ID} - Day {_day} - Shift {shift.SHIFT_ID}!", LogType.Debug);
                    workPlan = _WorkPlans.FirstOrDefault(wp => wp.LINE_ID == line.LINE_ID && wp.DAY == _day && wp.SHIFT_ID == shift.SHIFT_ID);
                    _currentShift = shift;
                    if (workPlan != null) break;
                }
                //Chưa có thì khởi tạo luôn --> Cho vào dạng kế hoạch luôn
                //Đây là dạng không có chi tiết thì khởi động chạy
                if (workPlan == null)
                {
                    if (_AutoAddWorkPlan)
                    {
                        workPlan = CreateWorkPlan(LineId, eventTime);
                    }
                }

                //Vào đây là reset đã
                ResetMessageLine(LineId);

                line.WorkPlan = null;
                line.Shift = _currentShift;
                if (workPlan != null)
                {
                    line.WorkPlan = null;

                    //Gán kế hoạch mới
                    workPlan.STATUS = (int)PLAN_STATUS.Proccessing;
                    line.WorkPlan = workPlan;

                    _Logger.Write(_LogCategory, $"Starting Line {LineId} - WorkPlan: {workPlan.WORK_PLAN_ID}", LogType.Debug);

                    //Lấy bộ BreakTimes
                    if (line.BreakTimes == null)
                    {
                        line.BreakTimes = new List<BreakTime>();
                    }
                    line.BreakTimes.Clear();

                    line.BreakTimes = GetBreakTimes(line.LINE_ID, line.WorkPlan.DAY, _currentShift.SHIFT_ID);
    
                    //Khởi tạo giá trị cho Node
                    ResetNode(line.LINE_ID);

                    //Khởi tạo tính toán bộ Report với khung thời gian
                    ResetLineReport(line.LINE_ID, workPlan.WORK_PLAN_ID);
                }
                BuildLineEvent(line.LINE_ID);
            }

            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Start Running WorkPlan Error: {ex}", LogType.Error);
            }

        }
        private WorkPlan CreateWorkPlan(string LineId, DateTime eventTime)
        {
            DateTime checktime = eventTime;

            if (checktime.Hour < Consts.HOUR_FOR_NEW_DAY)
            {
                checktime = checktime.AddDays(-1);
            }
            decimal _day = Time2Num(checktime, DayArchive);
            Shift shift = CheckShift(eventTime);
            if (shift == null)
            {
                _Logger.Write(_LogCategory, $"NO Shift match for {LineId} - Day {_day}!", LogType.Debug);
                return null;
            }

            MES_WORK_PLAN tblWorkPlan = new MES_WORK_PLAN()
            {
                WORK_PLAN_ID = GenID(),
                DAY = _day,
                LINE_ID = LineId,
                SHIFT_ID = shift.SHIFT_ID,
                STATUS = (int)PLAN_STATUS.NotStart, //Đặt trạng thái kế hoạch để không reload nữa
            };
            WorkPlan workPlan = new WorkPlan().Cast(tblWorkPlan);
            workPlan.PlanStart = shift.Start;
            workPlan.PlanFinish = shift.Finish;
            _WorkPlans.Add(workPlan);

            if (workPlan != null)
            {
                _Logger.Write(_LogCategory, $"Create WorkPlan for Line {workPlan.LINE_ID}: Start {workPlan.PlanStart:yyyy-MM-dd HH:mm:ss} - Finish {workPlan.PlanFinish:yyyy-MM-dd HH:mm:ss}", LogType.Debug);
            }

            return workPlan;
        }
        private void CalculateWorkPlanFactor(string LineId, DateTime eventTime)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                _Logger.Write(_LogCategory, $"Start calculate workplan factor for Line : {line.LINE_ID}", LogType.Debug);

                //Tính toán cho WorkPlan
                foreach (MES_WORK_PLAN_DETAIL workPlanDetail in line.WorkPlan.WorkPlanDetails)
                {
                    if (workPlanDetail.STATUS <= (int)PLAN_STATUS.Proccessing)
                    {
                        DateTime _start = workPlanDetail.PLAN_START;
                        if (_start <= eventTime)
                        {
                            workPlanDetail.STATUS = (int)PLAN_STATUS.Proccessing;
                        }
                    }
                }

                //Xử lý các thông số cho LINE
                MES_REPORT_LINE reportLine = line.ReportLine;

                MES_REPORT_LINE_DETAIL detailRunning = null;

                //Tính các thằng chi tiết
                if (line.ReportLineDetails.Count > 0)
                {
                    foreach (MES_REPORT_LINE_DETAIL detail in line.ReportLineDetails)
                    {
                        if (detail.STATUS == (int)PLAN_STATUS.NotStart)
                        {
                            _Logger.Write(_LogCategory, $"Calculate Report Detail to Run - Line : {line.LINE_ID}", LogType.Debug);
                            if (detail.STARTED <= eventTime && detail.STATUS< (int)PLAN_STATUS.Proccessing)
                            {
                                _Logger.Write(_LogCategory, $"Calculate Report Detail: Process [{detail.REPORT_LINE_DETAIL_ID}] to Run - Line : {line.LINE_ID}", LogType.Debug);

                                detail.STATUS = (int)PLAN_STATUS.Proccessing;

                                //Kiểm tra luôn thằng vỏ chạy chưa
                                if (reportLine != null)
                                {
                                    if (reportLine.STATUS < (int)PLAN_STATUS.Proccessing)
                                    {
                                        reportLine.STATUS = (int)PLAN_STATUS.Proccessing;
                                        reportLine.STARTED = detail.STARTED;
                                    }
                                }
                            }

                        }
                        //Ở đây có thằng DONE cần phải tính
                        if (detail.STATUS >= (int)PLAN_STATUS.Proccessing)
                        {
                            
                            if (detail.STATUS == (int)PLAN_STATUS.Proccessing)
                            {
                                //Cứ có thằng chạy là trạng thái chạy
                                detailRunning = detail;

                                if (eventTime > detail.PLAN_START)
                                {
                                    detail.FINISHED = (eventTime < detail.PLAN_FINISH) ? eventTime : detail.PLAN_FINISH;
                                }
                                if (eventTime >= detail.PLAN_FINISH)
                                {
                                    //Kết thúc rồi
                                    detail.STATUS = (int)PLAN_STATUS.Done;
                                }
                            }
                            decimal _detailDuration = (decimal)(detail.FINISHED - detail.STARTED).TotalSeconds;

                            //Kiểm tra Product nếu chưa có
                            if (detail.PRODUCT_ID == _DefaultProduct) //NOT FOUND!
                            {
                                DM_MES_PRODUCT product = _Products.FirstOrDefault(x => x.PRODUCT_CODE == detail.PRODUCT_CODE);
                                if (product != null)
                                {
                                    detail.PRODUCT_ID = product.PRODUCT_ID;
                                    detail.PRODUCT_NAME = product.PRODUCT_NAME;
                                }
                            }

                            int _numberOfStop = 0;
                            decimal _breakDurationDetail = 0;
                            decimal _stopDurationDetail = GetLineStopDuration(line.LINE_ID, detail.STARTED, detail.FINISHED, eventTime, out _numberOfStop, out _breakDurationDetail);

                            foreach(MES_LINE_EVENT lineEvent in line.LineEvents)
                            {
                                if (string.IsNullOrEmpty(lineEvent.REPORT_LINE_DETAIL_ID))
                                {
                                    lineEvent.REPORT_LINE_DETAIL_ID = detail.REPORT_LINE_DETAIL_ID;
                                }
                                if (lineEvent.REPORT_LINE_DETAIL_ID == detail.REPORT_LINE_DETAIL_ID)
                                {
                                    lineEvent.PRODUCT_ID = detail.PRODUCT_ID;
                                    lineEvent.PRODUCT_NAME = detail.PRODUCT_NAME;
                                    lineEvent.PRODUCT_CODE = detail.PRODUCT_CODE;
                                }

                            }
                            decimal _plan_takttime = detail.PLAN_TAKT_TIME;
                            if (_plan_takttime == 0) _plan_takttime = 1;
                            if (_plan_takttime != 0)
                            {
                                detail.PLAN_UPH = Math.Round(3600 / _plan_takttime, 2);
                            }
                            if (detail.HEAD_COUNT != 0)
                            {
                                detail.PLAN_UPPH = Math.Round(detail.PLAN_UPH / detail.HEAD_COUNT, 2);
                            }
                            decimal _takttime = detail.RUNNING_TAKT_TIME;
                            if (_takttime == 0) _takttime = 1;
                            if (_takttime != 0)
                            {
                                detail.RUNNING_UPH = Math.Round(3600 / _takttime, 2);
                            }
                            if (detail.RUNNING_HEAD_COUNT != 0)
                            {
                                detail.RUNNING_UPPH = Math.Round(detail.RUNNING_UPH / detail.RUNNING_HEAD_COUNT, 2);
                            }

                            detail.PLAN_DURATION = _detailDuration;
                            detail.BREAK_DURATION = _breakDurationDetail;
                            detail.STOP_DURATION = _stopDurationDetail;
                            detail.ACTUAL_DURATION = _detailDuration - _breakDurationDetail - _stopDurationDetail;
                            if (detail.ACTUAL_DURATION < 0) detail.ACTUAL_DURATION = 0;
                            detail.NUMBER_OF_STOP = _numberOfStop;
                            decimal _performance = GetPerformance(detail.PRODUCT_ID);

                            if (detail.ACTUAL_DURATION != 0)
                            {
                                detail.TARGET_QUANTITY = Math.Floor(_performance * (detail.ACTUAL_DURATION / _takttime) * detail.BATCH * detail.STATION_QUANTITY); //Nếu làm nhiều máy hoặc 1 lần ra nhiều hàng
                            }
                            detail.PLAN_RATE = 0;
                            if (detail.PLAN_QUANTITY != 0)
                            {
                                detail.PLAN_RATE = Math.Round(100 * detail.ACTUAL_QUANTITY / detail.PLAN_QUANTITY, 1);
                            }
                            detail.TARGET_RATE = 0;
                            if (detail.TARGET_QUANTITY != 0)
                            {
                                detail.TARGET_RATE = Math.Round(100 * detail.ACTUAL_QUANTITY / detail.TARGET_QUANTITY, 1);
                            }
                            detail.QUALITY_RATE = 100;
                            if (detail.ACTUAL_QUANTITY != 0)
                            {
                                detail.QUALITY_RATE = Math.Round(100 * (detail.ACTUAL_QUANTITY - detail.ACTUAL_NG_QUANTITY) / detail.ACTUAL_QUANTITY, 1);
                                detail.ACTUAL_TAKT_TIME = Math.Round(detail.ACTUAL_DURATION / detail.ACTUAL_QUANTITY, 2);
                            }
                            if (detail.ACTUAL_TAKT_TIME != 0)
                            {
                                detail.ACTUAL_UPH = Math.Round(3600 / detail.ACTUAL_TAKT_TIME, 2);
                            }
                            detail.ACTUAL_HEAD_COUNT = detail.RUNNING_HEAD_COUNT;
                            if (detail.ACTUAL_HEAD_COUNT != 0)
                            {
                                detail.ACTUAL_UPPH = Math.Round(detail.ACTUAL_UPH / detail.ACTUAL_HEAD_COUNT, 2);
                            }

                            detail.TIME_RATE = 100;
                            if (detail.ACTUAL_DURATION != 0)
                            {
                                detail.TIME_RATE = Math.Round(100 * detail.ACTUAL_DURATION / (detail.PLAN_DURATION - detail.BREAK_DURATION), 1);
                            }

                            detail.OEE = Math.Round(100*(detail.TIME_RATE * detail.TARGET_RATE * detail.QUALITY_RATE)/(100*100*100), 1);

                            detail.RESULT = CalculateResult(detail.STATUS);
                        }
                    }
                }
                
                if (reportLine != null)
                {
                    if (reportLine.STATUS >= (int)PLAN_STATUS.Proccessing)
                    {
                        #region ProcessEvent
                        if (reportLine.STATUS == (int)PLAN_STATUS.Proccessing)
                        {
                            reportLine.FINISHED = (eventTime < reportLine.PLAN_FINISH) ? eventTime : reportLine.PLAN_FINISH;
                        }
                        
                        //Kiểm tra xem hết kế hoạch hay chưa
                        if (eventTime > reportLine.PLAN_FINISH)
                        {
                            if (line.EventDefId != Consts.EVENTDEF_NOPLAN)
                            {
                                _Logger.Write(_LogCategory, $"Finish Report Line {LineId} - PlanFinish: {reportLine.PLAN_FINISH:HH:mm:ss}", LogType.Debug);
                                //Cho kết thúc để chuyển qua NOPLAN
                                ChangeLineEvent(line.LINE_ID, (DateTime)reportLine.PLAN_FINISH, Consts.EVENTDEF_NOPLAN);
                            }
                        }
                        string _eventDefId = Consts.EVENTDEF_RUNNING;

                        if (detailRunning != null)
                        {
                            if (line.EventDefId == Consts.EVENTDEF_NOPLAN)
                            {
                                //Trường hợp chưa có kế hoạch chạy thì cho chạy
                                ChangeLineEvent(line.LINE_ID, detailRunning.STARTED, Consts.EVENTDEF_RUNNING);
                            }
                        }    
                        else
                        {
                            //Trường hợp không còn thằng nào chạy mà ko ở trong Break thì có nghĩa là không kế hoạch
                            //Đoạn này xử lý tình huống giữa ca mà không có cái nào chạy.
                            if (line.EventDefId != Consts.EVENTDEF_BREAK)
                            {
                                ChangeLineEvent(line.LINE_ID, eventTime, Consts.EVENTDEF_NOPLAN);
                            }
                        }

                        //Xử lý phần LineEvent Break hoặc các sự kiện khác -> Chỉ xử lý khi đang có kế hoạch chạy
                        if (line.EventDefId != Consts.EVENTDEF_NOPLAN)
                        {
                            if (_isAutoBreakTime)
                            {
                                CheckBreakTimeBySchdule(line.LINE_ID, eventTime);
                            }

                            if (_isLineEventByNode)
                            {
                                CheckLineEventByNodes(line.LINE_ID, eventTime);
                            }
                        }
                        #endregion

                        //Tính toán cho ReportLine
                        #region Process ReportLine
                        _Logger.Write(_LogCategory, $"Process Report Line - Line : {line.LINE_ID}", LogType.Debug);
                        //Tính toán thời lượng chạy/dừng
                        int _numberOfStop = 0;
                        decimal _breakDuration = 0;
                        decimal _stopDuration = GetLineStopDuration(line.LINE_ID, reportLine.STARTED, reportLine.FINISHED, eventTime, out _numberOfStop, out _breakDuration);

                        line.ReportLine.ACTUAL_STOP_DURATION = _stopDuration;
                        line.ReportLine.NUMBER_OF_STOP = _numberOfStop;
                        line.ReportLine.ACTUAL_BREAK_DURATION = _breakDuration;
                        line.ReportLine.ACTUAL_DURATION = (decimal)(reportLine.FINISHED - reportLine.STARTED).TotalSeconds - _breakDuration;
                        line.ReportLine.ACTUAL_WORKING_DURATION = line.ReportLine.ACTUAL_DURATION - line.ReportLine.ACTUAL_STOP_DURATION;

                        if (line.ReportLineDetails.Count > 0)
                        {
                            //Tính đến thằng Line
                            reportLine.PLAN_QUANTITY = line.ReportLineDetails.Sum(x => x.PLAN_QUANTITY);
                            reportLine.TARGET_QUANTITY = line.ReportLineDetails.Sum(x => x.TARGET_QUANTITY);
                            reportLine.ACTUAL_QUANTITY = line.ReportLineDetails.Sum(x => x.ACTUAL_QUANTITY);
                            reportLine.ACTUAL_NG_QUANTITY = line.ReportLineDetails.Sum(x => x.ACTUAL_NG_QUANTITY);
                            reportLine.ACTUAL_TAKT_TIME = Math.Round(line.ReportLineDetails.Average(x => x.ACTUAL_TAKT_TIME),1);

                            reportLine.TIME_RATE = 100;
                            if (reportLine.ACTUAL_DURATION != 0)
                            {
                                reportLine.TIME_RATE = Math.Round(100 * reportLine.ACTUAL_WORKING_DURATION / reportLine.ACTUAL_DURATION, 1);
                            }

                            //Tính PlanRate theo kết quả hiện tại
                            decimal _planQuantity = reportLine.PLAN_QUANTITY;
                            decimal _actualQuantity = reportLine.ACTUAL_QUANTITY;
                            List<MES_REPORT_LINE_DETAIL> lineDetails = line.ReportLineDetails.Where(x => x.STATUS >= (int)PLAN_STATUS.NotStart).ToList();
                            if (lineDetails.Count > 0)
                            {
                                _planQuantity = lineDetails.Sum(x => x.PLAN_QUANTITY);
                                _actualQuantity = lineDetails.Sum(x => x.PLAN_QUANTITY);
                            }
                            reportLine.PLAN_RATE = 0;
                            if (_planQuantity != 0)
                            {
                                reportLine.PLAN_RATE = Math.Round(100 * _actualQuantity / _planQuantity, 1);
                            }
                            reportLine.TARGET_RATE = 0;
                            if (reportLine.TARGET_QUANTITY != 0)
                            {
                                reportLine.TARGET_RATE = Math.Round(100 * reportLine.ACTUAL_QUANTITY / reportLine.TARGET_QUANTITY, 1);
                            }
                            reportLine.QUALITY_RATE = 100;
                            if (reportLine.ACTUAL_QUANTITY != 0)
                            {
                                reportLine.QUALITY_RATE = Math.Round(100 * (reportLine.ACTUAL_QUANTITY - reportLine.ACTUAL_NG_QUANTITY) / reportLine.ACTUAL_QUANTITY, 1);
                            }
                            reportLine.OEE = Math.Round(100 * (reportLine.TIME_RATE * reportLine.TARGET_RATE * reportLine.QUALITY_RATE) / (100 * 100 * 100), 1);
                            reportLine.RESULT = CalculateResult(reportLine.STATUS);

                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Calculate Work Factor for Line {LineId} Error: {ex}", LogType.Error);
            }

        }

        private void FinishWorkPlan(string LineId, DateTime eventTime)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                if (line == null) return;

                WorkPlan workPlan = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID);
                if (workPlan == null) return;
                if (workPlan.STATUS == (int)PLAN_STATUS.Done) return;

                _Logger.Write(_LogCategory, $"Finish WorkPlan {workPlan.WORK_PLAN_ID} at Line {LineId}", LogType.Debug);
                workPlan.STATUS = (int)PLAN_STATUS.Done;
                //workPlan.Priority = 1; --> Chưa cho xóa, lúc nào loại bỏ hẳn mới xóa

                //Các kế hoạch chi tiết cũng được cập nhật kết thúc
                foreach (MES_WORK_PLAN_DETAIL planDetail in workPlan.WorkPlanDetails)
                {
                    planDetail.STATUS = (int)PLAN_STATUS.Done;
                }

                line.WorkPlan.STATUS = (int)PLAN_STATUS.Done;
               
                MES_REPORT_LINE tblReportLine = line.ReportLine;
                if (tblReportLine != null)
                {
                    tblReportLine.STATUS = (int)PLAN_STATUS.Done;

                    foreach (MES_REPORT_LINE_DETAIL reportLineDetail in line.ReportLineDetails)
                    {
                        //Chỉ cần kết thúc thằng đang chạy dở
                        if (reportLineDetail.STATUS == (int)PLAN_STATUS.Proccessing)
                        {
                            reportLineDetail.STATUS = (int)PLAN_STATUS.Done;
                            reportLineDetail.FINISHED = eventTime;
                        }
                    }
                }
                //Xử lý LineEvent
                ChangeLineEvent(line.LINE_ID, eventTime);
                _Logger.Write(_LogCategory, $"Finish Event {line.LINE_ID} - {eventTime} at Line {LineId}", LogType.Debug);

                //Xóa hết dữ liệu hiển thị
                //ResetMessageLine(LineId);
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Finish WorkPlan at Line {LineId} Error: {ex}", LogType.Error);
            }
        }
        private void ProcessWorkPlanDetails(string LineId, out DateTime WorkPlanStarted)
        {
            WorkPlanStarted = DateTime.Now;
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                WorkPlan workPlan = line.WorkPlan;
                //Xử lý các WorkPlanDetail
                WorkPlanStarted = line.WorkPlan.PlanStart;

                BuildTimeData(LineId);

                _Logger.Write(_LogCategory, $"Process Start at Line {LineId} - WorkPlan {workPlan.WORK_PLAN_ID} - WorkPlanDetail: {line.WorkPlan.WorkPlanDetails.Count} ", LogType.Debug);

                //DateTime _startPlanned = line.WorkPlan.PlanStart;
                int _id = 0;
                foreach (MES_WORK_PLAN_DETAIL workPlanDetail in line.WorkPlan.WorkPlanDetails)
                {
                    _id += 1;
                    //Xử lý phân bổ ra từng TIME
                    _Logger.Write(_LogCategory, $"Start Process for Plan of Line: Line {line.LINE_ID} - WorkPlan {workPlan.WORK_PLAN_ID} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - Total: {workPlanDetail.PLAN_QUANTITY}", LogType.Debug);
                    AddWorkPlanDetail2Time(workPlan, workPlanDetail, _id);
                }

                if (line.ReportLine != null)
                {
                    //line.ReportLine.PLAN_BREAK_DURATION = _planBreakDuration;
                    //line.ReportLine.PLAN_WORKING_DURATION = line.ReportLine.PLAN_TOTAL_DURATION - _planBreakDuration;
                    WorkPlanStarted = line.ReportLine.STARTED;
                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Add All Plan to Line {LineId} Error: {ex}", LogType.Error);
            }

        }
        private void AddWorkPlanDetail2Time(WorkPlan workPlan, MES_WORK_PLAN_DETAIL workPlanDetail, int _PlanIndex = 0)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == workPlan.LINE_ID);
                DateTime eventTime = DateTime.Now;

                if (_PlanIndex == 0) { _PlanIndex = line.WorkPlan.WorkPlanDetails.Count; }

                DateTime _startWorkPlanDetail = workPlanDetail.PLAN_START; //line.WorkPlan.PlanStart; 
                DateTime _finishWorkPlanDetail = workPlanDetail.PLAN_FINISH; //line.WorkPlan.PlanFinish; 
                decimal _planDuration = (decimal)(line.WorkPlan.PlanFinish - line.WorkPlan.PlanStart).TotalSeconds;
                decimal _planBreakDuration = GetBreakDuration(line.LINE_ID, line.WorkPlan.PlanStart, line.WorkPlan.PlanFinish);
                //Vào 1 WorkPlanDetail thì phải check ReportLine ngay
                if (line.ReportLine == null)
                {
                    MES_REPORT_LINE reportLine = new MES_REPORT_LINE()
                    {
                        REPORT_LINE_ID = GenID(),
                        LINE_ID = line.LINE_ID,
                        WORK_PLAN_ID = workPlan.WORK_PLAN_ID,
                        SHIFT_ID = workPlan.SHIFT_ID,
                        DAY = workPlan.DAY,
                        PLAN_START = _startWorkPlanDetail, //line.WorkPlan.PlanStart, //
                        PLAN_FINISH = _finishWorkPlanDetail, //line.WorkPlan.PlanFinish, 
                        PLAN_TOTAL_DURATION = _planDuration,
                        PLAN_WORKING_DURATION = _planDuration - _planBreakDuration,
                        PLAN_BREAK_DURATION = _planBreakDuration,
                        PLAN_QUANTITY = line.WorkPlan.WorkPlanDetails.Sum(x => x.PLAN_QUANTITY),
                        STARTED = line.WorkPlan.PlanStart, //workPlanDetail.PLAN_START,
                        FINISHED = eventTime,
                        PLAN_TAKT_TIME = 0,
                        PLAN_UPH = 0,
                        ACTUAL_DURATION = 0,
                        ACTUAL_BREAK_DURATION = 0,
                        ACTUAL_STOP_DURATION = 0,
                        ACTUAL_WORKING_DURATION = 0,
                        NUMBER_OF_STOP = 0,
                        TARGET_QUANTITY = 0,
                        ACTUAL_QUANTITY = 0,
                        ACTUAL_NG_QUANTITY = 0,
                        PLAN_RATE = 0,
                        TARGET_RATE = 0,
                        TIME_RATE = 0,
                        QUALITY_RATE = 0,
                        OEE = 0,
                        ACTUAL_TAKT_TIME = 0,
                        ACTUAL_UPH = 0,
                        RESULT= "",
                        STATUS = (int)PLAN_STATUS.Proccessing, //Tạo ra vỏ này là cho chạy luôn
                    };

                    line.ReportLine = reportLine;
                }
  
                //Tính toán sản phẩm ????
                string _ProductCode = workPlanDetail.PRODUCT_CODE;
                DM_MES_PRODUCT product = _Products.FirstOrDefault(x => x.PRODUCT_CODE == workPlanDetail.PRODUCT_CODE);
                string _ProductId = _DefaultProduct;
                string _ProductName = _DefaultProduct;
                if (product != null)
                {
                    _ProductId = product.PRODUCT_ID;
                    _ProductName = product.PRODUCT_NAME;
                }

                string _ProductConfigName = "";
                double _taktTime = 1;
                if (_UseProductConfig)
                {
                    if (workPlanDetail.CONFIG_ID != "0")
                    {
                        _ProductConfigName = _ProductConfigs.FirstOrDefault(x => x.PRODUCT_CONFIG_ID == workPlanDetail.CONFIG_ID).PRODUCT_CONFIG_NAME;
                    }
                }
                if (workPlanDetail.TAKT_TIME > 0) { _taktTime = (double)workPlanDetail.TAKT_TIME; }

                decimal _planQuantity = workPlanDetail.PLAN_QUANTITY - workPlanDetail.START_AT + 1;
                _Logger.Write(_LogCategory, $"Process Start Detail: Line {line.LINE_CODE} - WorkPlan {workPlan.WORK_PLAN_ID} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - ProductId {workPlanDetail.PRODUCT_ID} - Total: {_planQuantity}", LogType.Debug);

                //Nếu tự động chia theo TIME thì thực hiện
                //Nếu không thì cứ add thẳng vào là xong
                if (_AutoSplitWorkPlan2Time)
                {
                    decimal _totalTimeDuration = 0;

                    List<MES_REPORT_LINE_DETAIL> updateReportLineDetails = new List<MES_REPORT_LINE_DETAIL>();

                    foreach (TimeData timeData in line.TimeDatas)
                    {
                        //Phân bổ tổng sản lượng theo khung thời gian
                        if (_startWorkPlanDetail < timeData.Finish && _finishWorkPlanDetail > timeData.Start)
                        {
                            DateTime _startDetail = (timeData.Start > _startWorkPlanDetail) ? timeData.Start : _startWorkPlanDetail;
                            DateTime _finishDetail = (timeData.Finish < _finishWorkPlanDetail) ? timeData.Finish : _finishWorkPlanDetail;
                            decimal _detailDuration = (decimal)(_finishDetail - _startDetail).TotalSeconds;

                            MES_REPORT_LINE_DETAIL reportLineDetail = new MES_REPORT_LINE_DETAIL()
                            {
                                REPORT_LINE_DETAIL_ID = GenID(),
                                TIME_NAME = timeData.TimeName,
                                LINE_ID = line.LINE_ID,
                                WORK_PLAN_ID = workPlan.WORK_PLAN_ID,
                                DAY = workPlan.DAY,
                                SHIFT_ID = workPlan.SHIFT_ID,
                                WORK_PLAN_DETAIL_ID = workPlanDetail.WORK_PLAN_DETAIL_ID,
                                WORK_ORDER_CODE = workPlanDetail.WORK_ORDER_CODE + "",
                                WORK_ORDER_PLAN_CODE = workPlanDetail.WORK_ORDER_PLAN_CODE + "",
                                PO_CODE = workPlanDetail.PO_CODE + "",

                                REPORT_LINE_ID = line.ReportLine.REPORT_LINE_ID,
                                PLAN_START = _startDetail,
                                PLAN_FINISH = _finishDetail,
                                STARTED = _startDetail,
                                FINISHED = _startDetail,

                                PRODUCT_ID = _ProductId,
                                PRODUCT_CODE = _ProductCode,
                                PRODUCT_NAME = _ProductName,
                                STATION_QUANTITY = workPlanDetail.STATION_QUANTITY,
                                BATCH = workPlanDetail.BATCH,
                                CONFIG_ID = workPlanDetail.CONFIG_ID,
                                CONFIG_NAME = _ProductConfigName,
                                PLAN_TAKT_TIME = workPlanDetail.TAKT_TIME,
                                PLAN_UPH = 0,
                                PLAN_UPPH = 0,
                                HEAD_COUNT = workPlanDetail.HEAD_COUNT,
                                //Running
                                RUNNING_TAKT_TIME = workPlanDetail.TAKT_TIME,
                                RUNNING_UPH = 0,
                                RUNNING_UPPH = 0,
                                RUNNING_HEAD_COUNT = workPlanDetail.HEAD_COUNT,

                                PLAN_DURATION = _detailDuration,
                                PLAN_QUANTITY = 0,
                                ACTUAL_DURATION = 0,
                                BREAK_DURATION = 0,
                                STOP_DURATION = 0,
                                NUMBER_OF_STOP = 0,
                                ACTUAL_TAKT_TIME = workPlanDetail.TAKT_TIME,
                                TARGET_QUANTITY = 0,
                                ACTUAL_QUANTITY = 0,
                                ACTUAL_NG_QUANTITY = 0,
                                ACTUAL_UPH = 0,
                                ACTUAL_UPPH = 0,
                                ACTUAL_HEAD_COUNT = workPlanDetail.HEAD_COUNT,
                                START_AT = 0,
                                FINISH_AT = 0,
                                PLAN_RATE = 0,
                                TARGET_RATE = 0,
                                TIME_RATE = 0,
                                QUALITY_RATE = 0,
                                OEE = 0,
                                RESULT = "",
                                STATUS = (int)PLAN_STATUS.NotStart,
                                DETAIL_INDEX = _PlanIndex,
                            };

                            _totalTimeDuration += _detailDuration;

                            updateReportLineDetails.Add(reportLineDetail);
                        }
                    }

                    //Phân bổ sản lượng kế hoạch theo % thời gian.
                    decimal _remainQuantity = _planQuantity;
                    int _id = 0, _iCount = updateReportLineDetails.Count;
                    foreach (MES_REPORT_LINE_DETAIL tblReport in updateReportLineDetails)
                    {
                        decimal _quantity = (decimal)Math.Floor((_planQuantity * tblReport.PLAN_DURATION) / _totalTimeDuration);
                        //Nếu vượt quá thì gán = luôn
                        if (_quantity > _remainQuantity) { _quantity = _remainQuantity; }

                        //Nếu thằng cuối cùng thì cũng gán = luôn
                        if (_id == (_iCount - 1)) { _quantity = _remainQuantity; }

                        //Tính toán xong thì gán
                        tblReport.PLAN_QUANTITY = _quantity;

                        _Logger.Write(_LogCategory, $"Process Start Detail: Line {line.LINE_CODE} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - Time: {tblReport.TIME_NAME} - Product {tblReport.PRODUCT_CODE} - PlanQuantity: {_quantity}", LogType.Debug);

                        _remainQuantity -= _quantity;
                        _id++;
                    }

                    //Kiểm tra sự tồn tại của WorkPlanDetail đó hay chưa
                    //Kiểm tra kế hoạch đó trong TIME đó
                    //Nếu có tồn tại thì move giá trị Actual sang cái mới --> Đánh dấu Ready2Cancel
                    //List<tblReportLineDetail> existReportDetail = line.ReportLineDetails.Where(x => x.WorkPlanDetailId == workPlanDetail.Id).ToList();

                    _Logger.Write(_LogCategory, $"Total Detail: Line {line.LINE_CODE} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - Total: {line.ReportLineDetails.Count}", LogType.Debug);
                    foreach (MES_REPORT_LINE_DETAIL reportLineDetail in line.ReportLineDetails)
                    {
                        if (reportLineDetail.WORK_PLAN_DETAIL_ID != workPlanDetail.WORK_PLAN_DETAIL_ID) continue;

                        _Logger.Write(_LogCategory, $"Set Detail to Cancel: Line {line.LINE_CODE} - ReportLineDetail: {reportLineDetail.WORK_PLAN_DETAIL_ID} - Time: {reportLineDetail.TIME_NAME}", LogType.Debug);
                        TimeData timeData = line.TimeDatas.FirstOrDefault(x => x.TimeName == reportLineDetail.TIME_NAME);
                        //Move giá trị hiện có sang cái mới
                        MES_REPORT_LINE_DETAIL update = updateReportLineDetails.FirstOrDefault(x => x.TIME_NAME == reportLineDetail.TIME_NAME);
                        if (update != null)
                        {
                            update.ACTUAL_QUANTITY = reportLineDetail.ACTUAL_QUANTITY;
                            update.ACTUAL_NG_QUANTITY = reportLineDetail.ACTUAL_NG_QUANTITY;
                        }
                        reportLineDetail.STATUS= (int)PLAN_STATUS.Ready2Cancel; //Đặt đây để xóa đi
                    }
                    //Kiểm tra và chuyển giá trị xong thì add cái mới vào
                    if (updateReportLineDetails.Count > 0)
                    {
                        //Thêm mới các Detail
                        line.ReportLineDetails.AddRange(updateReportLineDetails);
                    }

                    //decimal _planWorkingDuration = 0;
                    //foreach (TimeData timeData in line.TimeDatas)
                    //{
                    //    if (line.ReportLineDetails.Where(x => x.TIME_NAME == timeData.TimeName).ToList().Count > 0)
                    //    {
                    //        _planWorkingDuration += timeData.Duration;
                    //    }
                    //}
                }
                else
                {
                    //add thẳng vào xử lý

                    //Nếu còn cái nào đang chạy thì dừng luôn
                    MES_REPORT_LINE_DETAIL detail = line.ReportLineDetails.FirstOrDefault(x => x.STATUS == (int)PLAN_STATUS.Proccessing);
                    if (detail != null)
                    {
                        FinishReportLineDetail(line.LINE_ID, detail.REPORT_LINE_DETAIL_ID, eventTime, Consts.EVENTDEF_RUNNING);
                    }

                    _Logger.Write(_LogCategory, $"Add Detail to Run: Line {line.LINE_CODE} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - Total: {workPlanDetail.PLAN_QUANTITY}", LogType.Debug);
                    decimal _actualQuantity = workPlanDetail.FINISH_AT - workPlanDetail.START_AT + 1;
                    MES_REPORT_LINE_DETAIL reportLineDetail = new MES_REPORT_LINE_DETAIL()
                    {
                        REPORT_LINE_DETAIL_ID = GenID(),
                        TIME_NAME = "",
                        LINE_ID = line.LINE_ID,
                        WORK_PLAN_ID = workPlan.WORK_PLAN_ID,
                        DAY = workPlan.DAY,
                        SHIFT_ID = workPlan.SHIFT_ID,
                        WORK_PLAN_DETAIL_ID = workPlanDetail.WORK_PLAN_DETAIL_ID,
                        WORK_ORDER_CODE = workPlanDetail.WORK_ORDER_CODE,
                        WORK_ORDER_PLAN_CODE = workPlanDetail.WORK_ORDER_PLAN_CODE,
                        PO_CODE = workPlanDetail.PO_CODE,

                        REPORT_LINE_ID = line.ReportLine.REPORT_LINE_ID,
                        PLAN_START = workPlanDetail.PLAN_START,
                        PLAN_FINISH = workPlanDetail.PLAN_FINISH,
                        STARTED = workPlanDetail.PLAN_START,
                        FINISHED = eventTime, //workPlanDetail.PLAN_START,

                        PRODUCT_ID = _ProductId,
                        PRODUCT_CODE = _ProductCode,
                        PRODUCT_NAME = _ProductName,
                        STATION_QUANTITY = workPlanDetail.STATION_QUANTITY,
                        BATCH = workPlanDetail.BATCH,
                        CONFIG_ID = workPlanDetail.CONFIG_ID,
                        CONFIG_NAME = _ProductConfigName,
                        PLAN_TAKT_TIME = workPlanDetail.TAKT_TIME,
                        PLAN_UPH = 0,
                        HEAD_COUNT = workPlanDetail.HEAD_COUNT,
                        //Running
                        RUNNING_TAKT_TIME = workPlanDetail.TAKT_TIME,
                        RUNNING_UPH = 0,
                        RUNNING_HEAD_COUNT = workPlanDetail.HEAD_COUNT,

                        PLAN_DURATION = _planDuration,
                        TOTAL_PLAN_QUANTITY = workPlanDetail.PLAN_QUANTITY,
                        PLAN_QUANTITY = _planQuantity,
                        ACTUAL_DURATION = 0,
                        BREAK_DURATION = 0,
                        STOP_DURATION = 0,
                        NUMBER_OF_STOP = 0,
                        ACTUAL_TAKT_TIME = workPlanDetail.TAKT_TIME,
                        TARGET_QUANTITY = 0,
                        ACTUAL_QUANTITY = _actualQuantity,
                        ACTUAL_NG_QUANTITY = 0,
                        ACTUAL_UPH = 0,
                        ACTUAL_HEAD_COUNT = workPlanDetail.HEAD_COUNT,
                        START_AT = workPlanDetail.START_AT,
                        FINISH_AT = workPlanDetail.START_AT,
                        PLAN_RATE = 0,
                        TARGET_RATE = 0,
                        TIME_RATE = 0,
                        QUALITY_RATE = 0,
                        OEE = 0,
                        DETAIL_INDEX = line.WorkPlan.WorkPlanDetails.Count,
                        RESULT = "",
                        STATUS = (int)PLAN_STATUS.Proccessing,
                        
                    };
                    line.ReportLineDetails.Add(reportLineDetail);

                    if (line.ReportLineDetails.Count > 0)
                    {
                        DateTime _startDetail = line.ReportLineDetails.Min(x => (DateTime)x.PLAN_START);
                        DateTime _finishDetail = line.ReportLineDetails.Max(x => (DateTime)x.PLAN_FINISH);
                        if (line.ReportLine.PLAN_START > _startDetail) line.ReportLine.PLAN_START = _startDetail;
                        if (line.ReportLine.PLAN_FINISH < _finishDetail) line.ReportLine.PLAN_FINISH = _finishDetail;
                        decimal _breakDuration = 0;
                        foreach (BreakTime breakTime in line.BreakTimes)
                        {
                            if (line.ReportLine.PLAN_START <= breakTime.StartTime && breakTime.StartTime < line.ReportLine.PLAN_FINISH)
                            {
                                DateTime _finish = (line.ReportLine.PLAN_FINISH > breakTime.FinishTime) ? breakTime.FinishTime : line.ReportLine.PLAN_FINISH;
                                _breakDuration += (decimal)(_finish - breakTime.StartTime).TotalSeconds;
                            }
                        }
                        line.ReportLine.PLAN_TOTAL_DURATION = (decimal)((DateTime)line.ReportLine.PLAN_FINISH - (DateTime)line.ReportLine.PLAN_START).TotalSeconds;
                        line.ReportLine.PLAN_WORKING_DURATION = line.ReportLine.PLAN_TOTAL_DURATION - _breakDuration;
                        line.ReportLine.PLAN_BREAK_DURATION = _breakDuration;
                    }
                }

                //Tính toán thời gian bắt đầu và Kết thúc cho ca chạy đó
                UpdateBackLineEvent(line.LINE_ID, eventTime);

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Add PlanDetail {workPlanDetail.WORK_PLAN_DETAIL_ID} - WorkPlan {workPlan.WORK_PLAN_ID} to Line {workPlan.LINE_ID} Error: {ex}", LogType.Error);
            }
        }

        private void RemoveWorkPlanDetail(string LineId, string workPlanDetailId)
        {
            //Lúc nào xử lý kế hoạch thì tạm dừng phần bắn thông tin đã
            //_TimerWS.Stop();
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                DateTime eventTime = DateTime.Now;

                MES_WORK_PLAN_DETAIL workPlanDetail = line.WorkPlan.WorkPlanDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == workPlanDetailId);

                if (workPlanDetail == null) return;

                _Logger.Write(_LogCategory, $"Remove Detail: Line [{line.LINE_ID}] - WorkPlanDetail: [{workPlanDetailId}] - Total: {line.ReportLineDetails.Count}", LogType.Debug);

                //Loại bỏ những thằng thuộc WorkPlanDetail này
                List<MES_REPORT_LINE_DETAIL> removedList = line.ReportLineDetails.Where(x => x.WORK_PLAN_DETAIL_ID == workPlanDetailId).ToList();

                foreach (MES_REPORT_LINE_DETAIL reportLineDetail in line.ReportLineDetails)
                {
                    if (reportLineDetail.WORK_PLAN_DETAIL_ID != workPlanDetail.WORK_PLAN_DETAIL_ID) continue;

                    _Logger.Write(_LogCategory, $"Set Detail to Cancel: Line {line.LINE_CODE} - ReportLineDetail: {reportLineDetail.WORK_PLAN_DETAIL_ID} - Time: {reportLineDetail.TIME_NAME}", LogType.Debug);

                    reportLineDetail.STATUS = (int)PLAN_STATUS.Ready2Cancel; //Đặt đây để xóa đi
                }

                //Tính toán thời gian bắt đầu và kết thúc cho thằng ReportLine 
                UpdateBackLineEvent(line.LINE_ID, eventTime);

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Remove PlanDetail {workPlanDetailId} - to Line {LineId} Error: {ex}", LogType.Error);
            }
        }

        private List<MES_REPORT_LINE_DETAIL> RemoveWorkPlan(string LineId, string workPlanId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                DateTime eventTime = DateTime.Now;

                _Logger.Write(_LogCategory, $"Remove WorkPlan: Line {line.LINE_ID} - WorkPlan: {workPlanId} - Total Report Details: {line.ReportLineDetails.Count}", LogType.Debug);

                //Loại bỏ những thằng thuộc WorkPlanDetail này
                List<MES_REPORT_LINE_DETAIL> removedList = line.ReportLineDetails.Where(x => x.WORK_PLAN_ID== workPlanId).ToList();
                line.ReportLineDetails.RemoveAll(x => x.WORK_PLAN_ID == workPlanId);

                line.WorkPlan = null;

                return removedList;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Remote All WorkPlan [{workPlanId}] - Line [{LineId}] Error: {ex}", LogType.Error);
            }
            return null;
        }
        private void FinishReportLineDetail(string LineId, string ReportDetailId, DateTime eventTime, string newEventDefId = "0")
        {
            try
            {
                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);
                if (line != null)
                {
                    MES_REPORT_LINE_DETAIL detail = line.ReportLineDetails.FirstOrDefault(x => x.REPORT_LINE_DETAIL_ID == ReportDetailId);
                    if (detail != null)
                    {
                        detail.STATUS = (int)PLAN_STATUS.Done; //Cho về Done hết, chỉ có thằng mới chạy mới là Processing
                        detail.FINISHED = eventTime;
                        //
                        if (newEventDefId == Consts.EVENTDEF_DEFAULT)
                        {
                            newEventDefId = Consts.EVENTDEF_NOPLAN;
                        }

                        ChangeLineEvent(line.LINE_ID, eventTime, newEventDefId);
                    }
                }    
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Finish ReportLineDetail {ReportDetailId} of Line {LineId} Error: {ex}", LogType.Error);
            }
}
        private string CalculateResult(double performance, DateTime _lastCount)
        {

            try
            {
                int _top_level = 100 + _ProductionLevel;
                int _bottom_level = 100 - _ProductionLevel;
                DateTime eventTime = DateTime.Now;
                if ((eventTime - _lastCount).TotalMinutes > _ProductionStop)
                {
                    return Consts.Production_Stop;
                }

                if (performance >= _bottom_level && performance <= _top_level)
                {
                    return Consts.Production_Good;
                }

                if (performance < _bottom_level)
                {
                    return Consts.Production_Delayed;
                }
                if (performance > _top_level)
                {
                    return Consts.Production_Fast;
                }

                return Consts.Production_Good;

                //for (int i = 0; i< _Levels.Count; i++)
                //{
                //    ProductionLevel level = _Levels[i];
                //    if (performance > level.Level)
                //    {
                //        return level.Name;
                //    }
                //}

                //return _Levels[0].Name;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Calculate Result Error: {ex}", LogType.Error);
            }
            return "";
        }
        private string CalculateResult(int _status)
        {
            string ret = "";
            try
            {
                switch (_status)
                {
                    case (int)PLAN_STATUS.NotStart:
                        ret = "Plan";
                        break;
                    case (int)PLAN_STATUS.Proccessing:
                        ret = "Running";
                        break;
                    case (int)PLAN_STATUS.Done:
                        ret = "Finished";
                        break;
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Calculate Result Error: {ex}", LogType.Error);
            }
            return ret;
        }
        private decimal GetPerformance(string _productId = "")
        {
            decimal _performance = 1;
            if (!_CalculateByPerformance) return _performance;

            try
            {
                if (_productId != "")
                {
                    using (Entities _dbContext = new Entities())
                    {
                        DM_MES_PRODUCT product = _dbContext.DM_MES_PRODUCT.FirstOrDefault(x => x.PRODUCT_ID == _productId);
                        if (product != null)
                        {
                            _performance = product.PERFORMANCE;
                        }
                    }
                }
                return _performance / 100;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Get Performance of Product {_productId} Error: {ex}", LogType.Error);
            }
            return _performance;

        }
        private MES_WORK_PLAN_DETAIL GetWorkPlanDetailByHistory(MES_WORK_PLAN_DETAIL_HISTORY history)
        {
            return new MES_WORK_PLAN_DETAIL()
            {
                WORK_PLAN_DETAIL_ID = history.WORK_PLAN_DETAIL_ID,
                PO_CODE = history.PO_CODE,
                WORK_ORDER_PLAN_CODE = history.WORK_ORDER_PLAN_CODE,
                WORK_ORDER_CODE = history.WORK_ORDER_CODE,
                WORK_PLAN_ID = history.WORK_PLAN_ID,
                PLAN_START = history.PLAN_START,
                PLAN_FINISH = history.PLAN_FINISH,
                LINE_ID = history.LINE_ID,
                DAY = history.DAY,
                PRODUCT_ID = history.PRODUCT_ID,
                PRODUCT_CODE = history.PRODUCT_CODE,
                CONFIG_ID = history.CONFIG_ID,
                STATION_QUANTITY = history.STATION_QUANTITY,
                BATCH = history.BATCH,
                TAKT_TIME = history.TAKT_TIME,
                PLAN_QUANTITY = history.PLAN_QUANTITY,
                HEAD_COUNT = history.HEAD_COUNT,
                DESCRIPTION = history.DESCRIPTION
            };
        }

        #endregion

        #region EventProcess
        private void ChangeLineEvent(string LineId, DateTime eventTime, string newEventDefId = "0")
        {
            try
            {
                //Loại bỏ phần sau GIÂY
                eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);

                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);

                DM_MES_EVENTDEF tblEventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == newEventDefId);

                bool isFinishOldEvent = false, isAddNewEvent = false;

                MES_LINE_EVENT oldEvent = null;
                if (line.LineEvents.Count > 0)
                {
                    oldEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);

                }
                else
                {
                    isAddNewEvent = true;
                }

                if (oldEvent != null)
                {
                    if (newEventDefId == Consts.EVENTDEF_DEFAULT)
                    {
                        isFinishOldEvent = true;
                    }
                    else
                    {
                        if (newEventDefId != oldEvent.EVENTDEF_ID)
                        {
                            isFinishOldEvent = true;
                            isAddNewEvent = true;
                        }
                    }

                    if (isFinishOldEvent)
                    {
                        //Kết thúc cái cũ
                        oldEvent.FINISH = eventTime;
                        if (eventTime < oldEvent.START) oldEvent.FINISH = oldEvent.START;
                        oldEvent.TOTAL_DURATION = (decimal)(eventTime - oldEvent.START).TotalSeconds;
                        oldEvent.WAIT_DURATION = 0;
                        oldEvent.FIX_DURATION = oldEvent.TOTAL_DURATION;

                        if (oldEvent.RESPONSE.HasValue)
                        {
                            oldEvent.WAIT_DURATION = (decimal)((DateTime)oldEvent.RESPONSE - oldEvent.START).TotalSeconds;
                            oldEvent.FIX_DURATION = (decimal)((DateTime)oldEvent.FINISH - (DateTime)oldEvent.RESPONSE).TotalSeconds;
                        }
                        _Logger.Write(_LogCategory, $"Finish Line {LineId} - Finish Event {oldEvent.EVENT_ID} - {oldEvent.FINISH}", LogType.Debug);
                    }
                }

                if (isAddNewEvent)
                {
                    _Logger.Write(_LogCategory, $"Process Add Event: Line {line.LINE_ID} - Event: {line.EventDefId} - Time: {eventTime:yyyy-MM-dd HH:mm:ss}", LogType.Debug);

                    line.LastEventDefId = line.EventDefId;

                    line.EventDefId = tblEventDef.EVENTDEF_ID;
                    line.EventDefName_EN = tblEventDef.EVENTDEF_NAME_EN;
                    line.EventDefName_VN = tblEventDef.EVENTDEF_NAME_VN;
                    line.EventDefColor = tblEventDef.EVENTDEF_COLOR;

                    _Logger.Write(_LogCategory, $"New Event at Line {line.LINE_CODE} - Event: {line.EventDefId}", LogType.Debug);


                    string _detailId = "", _productId = "", _productCode = "", _productName = "";

                    List<MES_REPORT_LINE_DETAIL> details = line.ReportLineDetails.Where(x => x.STATUS == (int)PLAN_STATUS.Proccessing).ToList();
                    if (details.Count > 0)
                    {
                        foreach (MES_REPORT_LINE_DETAIL detail in details)
                        {
                            _detailId += detail.REPORT_LINE_DETAIL_ID + ",";
                            _productId += detail.PRODUCT_ID + ",";
                            _productCode += detail.PRODUCT_CODE + ",";
                            _productName += detail.PRODUCT_NAME + ",";
                        }
                        _detailId = _detailId.Substring(0, _detailId.Length - 1);
                        _productId = _productId.Substring(0, _productId.Length - 1);
                        _productCode = _productCode.Substring(0, _productCode.Length - 1);
                        _productName = _productName.Substring(0, _productName.Length - 1);
                    }


                    MES_LINE_EVENT lineEvent = CreateLineEvent(line, tblEventDef, eventTime, _detailId, _productId, _productCode, _productName);

                    //Check trong listEvent có chưa đã
                    bool isCheckOk = true;
                    if (line.LineEvents.Count > 0)
                    {
                        MES_LINE_EVENT checkEvent = line.LineEvents.FirstOrDefault(x => x.EVENTDEF_ID == lineEvent.EVENTDEF_ID && x.START == lineEvent.START);
                        if (checkEvent != null) isCheckOk = false;
                    }    

                    if (isCheckOk) line.LineEvents.Add(lineEvent);
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process Change LineEvent Error: {ex}", LogType.Error);
            }
        }
        private MES_LINE_EVENT CreateLineEvent(Line line, DM_MES_EVENTDEF tblEventDef, DateTime eventTime, string _detailId = "", string _productId = "", string _productCode = "", string _productName = "")
        {
            //Loại bỏ phần sau GIÂY
            eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);

            WorkPlan workPlan = line.WorkPlan;
            string _workPlanId = "", _shiftId = line.Shift.SHIFT_ID;
            decimal _day = line.Shift.FullDay;
            DateTime _start = line.Shift.Start, _finish = line.Shift.Finish;

            if (workPlan != null)
            {
                _workPlanId = workPlan.WORK_PLAN_ID;
                _day = workPlan.DAY;
                _shiftId = workPlan.SHIFT_ID;
                _start = workPlan.PlanStart;
                _finish = workPlan.PlanFinish;
            }

            MES_LINE_EVENT lineEvent = new MES_LINE_EVENT()
            {
                EVENT_ID = GenID(),
                LINE_ID = line.LINE_ID,
                LINE_CODE = line.LINE_CODE,
                LINE_NAME = line.LINE_NAME,
                EVENTDEF_ID = tblEventDef.EVENTDEF_ID,
                EVENTDEF_NAME_EN = tblEventDef.EVENTDEF_NAME_EN,
                EVENTDEF_NAME_VN = tblEventDef.EVENTDEF_NAME_VN,
                EVENTDEF_COLOR = tblEventDef.EVENTDEF_COLOR,
                START = eventTime,
                //Reposne & Finish = NULL,
                WAIT_DURATION = 0,
                FIX_DURATION = 0,
                TOTAL_DURATION = 0,
                WORK_PLAN_ID = _workPlanId,
                SHIFT_ID = _shiftId,
                DAY = _day,
                
                REPORT_LINE_DETAIL_ID = _detailId,
                PRODUCT_ID = _productId,
                PRODUCT_CODE = _productCode,
                PRODUCT_NAME = _productName,

                REASON_ID = "",
                RESPONSIBILITY_ID = "",
                DESCRIPTION = "",
                COMMENT = "",

            };

            return lineEvent;
        }
        private void ChangeNodeEvent(string LineId, string NodeId, DateTime eventTime, string newEventDefId = "0")
        {
            try
            {
                //Loại bỏ phần sau GIÂY
                eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);

                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);
                if (line == null) return;
                if (line.WorkPlan == null) return;
                if (line.ReportLine == null) return;
                if (line.EventDefId == Consts.EVENTDEF_NOPLAN) return;

                Node node = line.Nodes.FirstOrDefault(x => x.NODE_ID == NodeId);

                DM_MES_EVENTDEF tblEventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == newEventDefId);

                string oldEventId = Consts.EVENTDEF_DEFAULT;
                MES_NODE_EVENT oldEvent = null;
                if (node.NodeEvents.Count > 0)
                {
                    oldEvent = node.NodeEvents.Last(x => !x.FINISH.HasValue);
                    oldEventId = oldEvent.EVENTDEF_ID;
                }

                bool isFinishOldEvent = false, isAddNewEvent = false;
                if (newEventDefId == Consts.EVENTDEF_DEFAULT)
                {
                    isFinishOldEvent = true;
                }
                else
                {
                    if (newEventDefId != oldEventId)
                    {
                        isFinishOldEvent = true;
                        isAddNewEvent = true;
                    }
                }

                if (isFinishOldEvent)
                {
                    if (oldEvent != null)
                    {
                        //Kết thúc cái cũ
                        oldEvent.FINISH = eventTime;
                        oldEvent.TOTAL_DURATION = (decimal)(eventTime - oldEvent.START).TotalSeconds;
                        oldEvent.WAIT_DURATION = 0;
                        oldEvent.FIX_DURATION = oldEvent.TOTAL_DURATION;

                        if (oldEvent.RESPONSE.HasValue)
                        {
                            oldEvent.WAIT_DURATION = (decimal)((DateTime)oldEvent.RESPONSE - oldEvent.START).TotalSeconds;
                            oldEvent.FIX_DURATION = (decimal)((DateTime)oldEvent.FINISH - (DateTime)oldEvent.RESPONSE).TotalSeconds;
                        }
                        _Logger.Write(_LogCategory, $"Node {NodeId} - Finish Event {oldEvent.EVENT_ID} - {oldEvent.FINISH}", LogType.Debug);
                    }
                }

                if (isAddNewEvent)
                {
                    _Logger.Write(_LogCategory, $"Process Add Event for Node {node.NODE_ID} - Current Event: {oldEventId} - Time: {eventTime:yyyy-MM-dd HH:mm:ss}", LogType.Debug);

                    //Thêm cái mới

                    _Logger.Write(_LogCategory, $"New Event at Node {NodeId} - Event: {line.EventDefId}", LogType.Debug);

                    string _detailId = "", _productId = "", _productCode = "", _productName = "";

                    List<MES_REPORT_LINE_DETAIL> details = line.ReportLineDetails.Where(x => x.STATUS == (int)PLAN_STATUS.Proccessing).ToList();
                    if (details.Count > 0)
                    {
                        foreach (MES_REPORT_LINE_DETAIL detail in details)
                        {
                            _detailId += detail.REPORT_LINE_DETAIL_ID + ",";
                            _productId += detail.PRODUCT_ID + ",";
                            _productCode += detail.PRODUCT_CODE + ",";
                            _productName += detail.PRODUCT_NAME + ",";
                        }
                        _detailId = _detailId.Substring(0, _detailId.Length - 1);
                        _productId = _productId.Substring(0, _productId.Length - 1);
                        _productCode = _productCode.Substring(0, _productCode.Length - 1);
                        _productName = _productName.Substring(0, _productName.Length - 1);
                    }
                    MES_NODE_EVENT nodeEvent = new MES_NODE_EVENT()
                    {
                        EVENT_ID = GenID(),
                        NODE_ID = NodeId,
                        NODE_CODE = node.NODE_NAME,
                        NODE_NAME = node.NODE_NAME,
                        EVENTDEF_ID = newEventDefId,
                        EVENTDEF_NAME_EN = tblEventDef.EVENTDEF_NAME_EN,
                        EVENTDEF_NAME_VN = tblEventDef.EVENTDEF_NAME_VN,
                        EVENTDEF_COLOR = tblEventDef.EVENTDEF_COLOR,
                        START = eventTime,
                        //Reposne & Finish = NULL,
                        WAIT_DURATION = 0,
                        FIX_DURATION = 0,
                        TOTAL_DURATION = 0,
                        WORK_PLAN_ID = line.WorkPlan.WORK_PLAN_ID,
                        SHIFT_ID = line.WorkPlan.SHIFT_ID,
                        DAY = line.WorkPlan.DAY,
                        REPORT_LINE_DETAIL_ID = _detailId,
                        PRODUCT_ID = _productId,
                        PRODUCT_CODE = _productCode,
                        PRODUCT_NAME = _productName,

                        REASON_ID = "",
                        RESPONSIBILITY_ID = "",
                        DESCRIPTION = ""
                    };
                    node.NodeEvents.Add(nodeEvent);

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process Change Node Event Error: {ex}", LogType.Error);
            }
        }
        private void BuildLineEvent(string LineId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                WorkPlan workPlan = line.WorkPlan;
                string _workPlanId = "", _shiftId = line.Shift.SHIFT_ID;
                decimal _day = line.Shift.FullDay;
                DateTime _start = line.Shift.Start, _finish = line.Shift.Finish;

                if (workPlan != null)
                {
                    _workPlanId = workPlan.WORK_PLAN_ID;
                    _day = workPlan.DAY;
                    _shiftId = workPlan.SHIFT_ID;
                    _start = workPlan.PlanStart;
                    _finish = workPlan.PlanFinish;
                }
                DateTime eventTime = DateTime.Now;
                //Loại bỏ phần sau GIÂY
                eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);

                //Khởi tạo Event ban đầu


                ////Chỗ này fix tạm để xử lý
                DateTime actualStartPlan = _start;
                string _EventDefId = Consts.EVENTDEF_NOPLAN;

                if (line.ReportLine != null)
                {
                    actualStartPlan = line.ReportLine.PLAN_START;
                    //Khớp nhau thì Running
                    if (actualStartPlan == _start)
                    {
                        _EventDefId = Consts.EVENTDEF_RUNNING;
                    }
                }

                ChangeLineEvent(line.LINE_ID, _start, _EventDefId);
         

                //Line Working
                if (line.LineWorkings == null)
                {
                    line.LineWorkings = new List<MES_LINE_WORKING>();
                }
                if (line.LineWorkings.Count == 0)
                {
                    foreach (DM_MES_EVENTDEF eventDef in _EventDefs)
                    {
                        MES_LINE_WORKING lineRunning = new MES_LINE_WORKING()
                        {
                            LINE_WORKING_ID = GenID(),
                            LINE_ID = line.LINE_ID,
                            LINE_CODE = line.LINE_CODE,
                            LINE_NAME = line.LINE_NAME,
                            WORK_PLAN_ID = _workPlanId,
                            DAY = _day,
                            SHIFT_ID = _shiftId,
                            START = _start,
                            FINISH = _finish,
                            EVENTDEF_ID = eventDef.EVENTDEF_ID,
                            EVENTDEF_NAME_VN = eventDef.EVENTDEF_NAME_VN,
                            EVENTDEF_NAME_EN = eventDef.EVENTDEF_NAME_EN,
                            EVENTDEF_COLOR = eventDef.EVENTDEF_COLOR,
                            DURATION = 0,
                        };
                        line.LineWorkings.Add(lineRunning);
                    }
                }
                //Line STOP
                if (line.LineStops == null)
                {
                    line.LineStops = new List<MES_LINE_STOP>();
                }
                if (line.LineStops.Count == 0)
                {
                    DM_MES_EVENTDEF eventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_STOP);
                    foreach (MES_STOP_REASON stop in _StopReasons)
                    {
                        MES_LINE_STOP lineStop = new MES_LINE_STOP()
                        {
                            LINE_STOP_ID = GenID(),
                            LINE_ID = line.LINE_ID,
                            LINE_CODE = line.LINE_CODE,
                            LINE_NAME = line.LINE_NAME,
                            WORK_PLAN_ID = _workPlanId,
                            DAY = _day,
                            SHIFT_ID = _shiftId,
                            START = _start,
                            FINISH = _finish,

                            EVENTDEF_ID = eventDef.EVENTDEF_ID,
                            EVENTDEF_NAME_VN = eventDef.EVENTDEF_NAME_VN,
                            EVENTDEF_NAME_EN = eventDef.EVENTDEF_NAME_EN,
                            EVENTDEF_COLOR = eventDef.EVENTDEF_COLOR,

                            REASON_ID = stop.REASON_ID,
                            REASON_NAME_EN = stop.REASON_NAME_EN,
                            REASON_NAME_VN = stop.REASON_NAME_VN,

                            DURATION = 0,
                        };
                        line.LineStops.Add(lineStop);
                    }
                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Build Line Events Error: {ex}", LogType.Error);
            }
        }
        private void UpdateBackLineEvent(string LineId, DateTime eventTime)
        {
            try
            {
                //Loại bỏ phần sau GIÂY
                eventTime = eventTime.AddMilliseconds(0 - eventTime.Millisecond);
                //Trường hợp đang chạy nhưng tình huống thằng thêm vào mới lại trước thằng hiện tại --> Xử lý lại đoạn NOPLAN
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                WorkPlan workPlan = line.WorkPlan;
                MES_REPORT_LINE reportLine = line.ReportLine;
                if (reportLine == null) return;

                List<MES_REPORT_LINE_DETAIL> lstReportLineDetails = line.ReportLineDetails.Where(x => x.STATUS != (int)PLAN_STATUS.Ready2Cancel).ToList();

                //Update thằng ReportLine
                if (lstReportLineDetails.Count > 0)
                {
                    //Tính toán lại cho thằng ReportLine
                    DateTime _startDetail = lstReportLineDetails.Min(x => (DateTime)x.PLAN_START);
                    DateTime _finishDetail = lstReportLineDetails.Max(x => (DateTime)x.PLAN_FINISH);

                    line.ReportLine.PLAN_START = _startDetail;
                    line.ReportLine.PLAN_FINISH = _finishDetail;

                    //Check thêm update lại Line
                    line.ReportLine.STARTED = line.ReportLine.PLAN_START;

                    line.ReportLine.PLAN_QUANTITY = lstReportLineDetails.Sum(x => x.PLAN_QUANTITY);
                    line.ReportLine.PLAN_TAKT_TIME = Math.Round(lstReportLineDetails.Average(x => x.PLAN_TAKT_TIME), 2);
                    line.ReportLine.PLAN_UPH = Math.Round(lstReportLineDetails.Average(x => x.PLAN_UPH), 2);

                    line.ReportLine.PLAN_TOTAL_DURATION = (decimal)((DateTime)line.ReportLine.PLAN_FINISH - (DateTime)line.ReportLine.PLAN_START).TotalSeconds;
                    line.ReportLine.PLAN_BREAK_DURATION = GetBreakDuration(line.LINE_ID, line.ReportLine.PLAN_START, line.ReportLine.PLAN_FINISH);
                    line.ReportLine.PLAN_WORKING_DURATION = line.ReportLine.PLAN_TOTAL_DURATION - line.ReportLine.PLAN_BREAK_DURATION;

                    _Logger.Write(_LogCategory, $"Update ReportLine: Line [{line.LINE_ID}] - Start [{line.ReportLine.PLAN_START}] - Finish [{line.ReportLine.PLAN_FINISH}]", LogType.Debug);
                }

                //Bắt đầu xử lý phần sự kiện
                DM_MES_EVENTDEF _running = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_RUNNING);
                DM_MES_EVENTDEF _noplan = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_NOPLAN);
                //actualStartPlan là thời gian thực tế chạy
                DateTime actualStartPlan = line.ReportLine.STARTED;

                if (line.LineEvents.Count > 0)
                {
                    _Logger.Write(_LogCategory, $"Start Update Back at Line [{LineId}]", LogType.Debug);

                    ///========================================================================================
                    ///Nếu ông nào trước khi thực tế chạy: Cho thành No-Plan
                    ///Nếu ông nào mà vắt ngang ra: bổ ra thành 2 cái: 1 thành NOPLAN -> 1 thành RUNNING

                    List<MES_LINE_EVENT> lstEvents = line.LineEvents;
                    //Lấy các event bắt đầu trước thời điểm cập nhật
                    //Nếu kết thúc trước đó ==> Update thành NOPLAN
                    //Cái cuối cùng thì bổ ra thành 2 cái: 1 thành NOPLAN -> 1 thành RUNNING
                    foreach (MES_LINE_EVENT lstEvent in lstEvents)
                    {
                        if (lstEvent.FINISH <= actualStartPlan)
                        {
                            _Logger.Write(_LogCategory, $"Change Event {lstEvent.EVENT_ID} FROM {lstEvent.EVENTDEF_ID} to NoPlan at Line {LineId}", LogType.Debug);
                            //Cập nhật thành NOPLAN những thằng trước khi bắt đầu thực tế
                            lstEvent.EVENTDEF_ID = _noplan.EVENTDEF_ID;
                            lstEvent.EVENTDEF_NAME_EN = _noplan.EVENTDEF_NAME_EN;
                            lstEvent.EVENTDEF_NAME_VN = _noplan.EVENTDEF_NAME_VN;
                            lstEvent.EVENTDEF_COLOR = _noplan.EVENTDEF_COLOR;
                        }
                        else
                        {
                            if (lstEvent.START <= actualStartPlan)
                            {
                                //Tách ra 2 cái: 1 cái NoPlan từ Start đến actualStartPlan, 1 cái là Running từ actualStartPlan

                                //Thêm thằng mới là thằng NoPlan từ bắt đầu Start của nó đến thời điểm actualStartPlan

                                string _detailId = "", _productId = "", _productCode = "", _productName = "";
                                long _duration = (long)(actualStartPlan - lstEvent.START).TotalSeconds;
                                if (_duration > 0)
                                {
                                    foreach (MES_REPORT_LINE_DETAIL detail in lstReportLineDetails)
                                    {
                                        //Nếu nó chạy trong khoảng đó
                                        if (detail.STARTED <= actualStartPlan && detail.FINISHED > actualStartPlan)
                                        {
                                            _detailId += detail.REPORT_LINE_DETAIL_ID + ",";
                                            _productId += detail.PRODUCT_ID + ",";
                                            _productCode += detail.PRODUCT_CODE + ",";
                                            _productName += detail.PRODUCT_NAME + ",";
                                        }
                                    }
                                    //Tạo thằng NO-PLAN mới
                                    MES_LINE_EVENT lineEvent = CreateLineEvent(line, _noplan, lstEvent.START, _detailId, _productId, _productCode, _productName);
                                    lineEvent.FINISH = actualStartPlan;
                                    lineEvent.TOTAL_DURATION = _duration;

                                    line.LineEvents.Add(lineEvent);
                                    _Logger.Write(_LogCategory, $"Add NoPlan Event Start [{lstEvent.START:HH:mm:ss}] - Finish [{actualStartPlan:HH:mm:ss}]  at Line {LineId}", LogType.Debug);

                                    //Cập nhật thằng Running cũ cho bắt đầu tại thời điểm actualStartPlan
                                    lstEvent.EVENTDEF_ID = _running.EVENTDEF_ID;
                                    lstEvent.EVENTDEF_NAME_EN = _running.EVENTDEF_NAME_EN;
                                    lstEvent.EVENTDEF_NAME_VN = _running.EVENTDEF_NAME_VN;
                                    lstEvent.EVENTDEF_COLOR = _running.EVENTDEF_COLOR;
                                    lstEvent.START = actualStartPlan;
                                    _Logger.Write(_LogCategory, $"Change Event {lstEvent.EVENT_ID} FROM {lstEvent.EVENTDEF_ID} to Running: Start [{actualStartPlan:HH:mm:ss}] at Line {LineId}", LogType.Debug);

                                }
                            }
                            //else
                            //{
                            //    //Những thằng còn lại
                            //    //Chỉ xét nếu = NO-PLAN 
                            //    //Chưa xảy ra thì chỉ đổi lại thằng ban đầu thành NoPlan mà thôi
                            //    lstEvent.EVENTDEF_ID = _noplan.EVENTDEF_ID;
                            //    lstEvent.EVENTDEF_NAME_EN = _noplan.EVENTDEF_NAME_EN;
                            //    lstEvent.EVENTDEF_NAME_VN = _noplan.EVENTDEF_NAME_VN;
                            //    lstEvent.EVENTDEF_COLOR = _noplan.EVENTDEF_COLOR;

                            //}
                        }
                    }

                    //B - ĐOẠN SAU: KỆ NÓ CHẠY = PHẦN KIỂM TRA THÔI

                    //C - CUỐI CÙNG: Sắp xếp lại phát
                    line.LineEvents = line.LineEvents.OrderBy(x => x.START).ToList();

                    //Update lại trạng thái của LINE
                    MES_LINE_EVENT lastEvent = line.LineEvents.LastOrDefault(x => !x.FINISH.HasValue);
                    DM_MES_EVENTDEF tblEventDef = _noplan;
                    if (lastEvent != null)
                    {
                        tblEventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == lastEvent.EVENTDEF_ID);
                    }
                    line.LastEventDefId = line.EventDefId;
                    line.EventDefId = tblEventDef.EVENTDEF_ID;
                    line.EventDefName_EN = tblEventDef.EVENTDEF_NAME_EN;
                    line.EventDefName_VN = tblEventDef.EVENTDEF_NAME_VN;
                    line.EventDefColor = tblEventDef.EVENTDEF_COLOR;
                }
                else
                {
                    //Trường hợp chưa có thì phải thêm vào
                    //Khởi tạo thằng đầu tiên
                    string _detailId = "", _productId = "", _productCode = "", _productName = "";

                    string _eventDef = Consts.EVENTDEF_RUNNING;
                    if (actualStartPlan > workPlan.PlanStart)
                    {
                        _eventDef = Consts.EVENTDEF_NOPLAN;
                    }
                    //Chạy sau khi bắt đầu ca thì thêm NOPLAN vào đoạn đầu
                    ChangeLineEvent(LineId, workPlan.PlanStart, _eventDef);

                    if (actualStartPlan > workPlan.PlanStart)
                    {
                        if ((eventTime > actualStartPlan) && line.EventDefId == Consts.EVENTDEF_NOPLAN)
                            //Chạy sau khi bắt đầu ca thì thêm NOPLAN vào đoạn đầu
                            ChangeLineEvent(LineId, workPlan.PlanStart, Consts.EVENTDEF_RUNNING);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Update Back LineEvent for line {LineId} Error: {ex}", LogType.Error);
            }

        }
        private void CheckLineEventByNodes(string LineId, DateTime eventTime)
        {

            Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
            //Lấy thằng cuối cùng
            string _eventDefId = line.EventDefId;
            //Test for Line Break/Stop
            bool testForNodebreak = false, testForNodeStop = false;
            foreach (Node node in line.Nodes)
            {
                MES_NODE_EVENT nodeEvent = node.NodeEvents.FirstOrDefault(x => !x.FINISH.HasValue);
                if (nodeEvent == null) continue;
                if (nodeEvent.EVENTDEF_ID == Consts.EVENTDEF_BREAK)
                {
                    testForNodebreak = true;
                }
                if (nodeEvent.EVENTDEF_ID == Consts.EVENTDEF_STOP)
                {
                    testForNodeStop = true;
                }

            }
            //Nếu vào Break
            if (testForNodebreak)
            {
                _eventDefId = Consts.EVENTDEF_BREAK;
            }
            else
            {
                if (testForNodeStop)
                {
                    _eventDefId = Consts.EVENTDEF_STOP;
                }
                else
                {
                    _eventDefId = Consts.EVENTDEF_RUNNING;
                }
            }

            ChangeLineEvent(line.LINE_ID, eventTime, _eventDefId);
            _Logger.Write(_LogCategory, $"Process Event - Status is [{line.EventDefId}] - Change to [{_eventDefId}] - Line : {line.LINE_ID}", LogType.Debug);
        }
        private void CheckBreakTimeBySchdule(string LineId, DateTime eventTime)
        {
            Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
            //if (line.ReportLine == null) return;
            //if (line.LineEvents.Count == 0) return;

            //if (line.EventDefId == Consts.EVENTDEF_NOPLAN) return;

            MES_LINE_EVENT tblLineEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);
            //Test for BreakTime
            foreach (BreakTime breakTime in line.BreakTimes)
            {
                tblLineEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);

                if (breakTime.StartTime < eventTime)
                {
                    //Giờ này đúng là rơi vào BreakTime rồi
                    _Logger.Write(_LogCategory, $"Line {LineId} - LastEvent Started: {tblLineEvent.START:HH:mm:ss} - Break Start: {breakTime.StartTime:HH:mm:ss}", LogType.Debug);
                    if (tblLineEvent.START < breakTime.StartTime)
                    {
                        //Chưa vào nghỉ thì cho vào nghỉ
                        if (tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_BREAK && tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_NOPLAN)
                        {
                            _Logger.Write(_LogCategory, $"Line {LineId} - Add Break: Start: {breakTime.StartTime:HH:mm:ss}", LogType.Debug);
                            ChangeLineEvent(line.LINE_ID, breakTime.StartTime, Consts.EVENTDEF_BREAK);
                        }
                    }
                }

                tblLineEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);

                if (breakTime.FinishTime <= eventTime)
                {
                    if (tblLineEvent.START < breakTime.FinishTime && tblLineEvent.EVENTDEF_ID == Consts.EVENTDEF_BREAK)
                    {
                        //Đang nghỉ thì cho kết thúc nghỉ và trở lại trạng thái trước đó
                        _Logger.Write(_LogCategory, $"Line {LineId} - Finish Break: Start: {breakTime.StartTime:HH:mm:ss} Finish: {breakTime.FinishTime:HH:mm:ss}", LogType.Debug);
                        ChangeLineEvent(line.LINE_ID, breakTime.FinishTime, line.LastEventDefId);
                    }
                }
            }
        }
        #endregion

        #region Reset
        private void ResetLineReport(string LineId, string WorkPlanId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                ////Xóa bỏ hết cũ
                //ResetRunningLine(LineId);
                //Khởi tạo lại dữ liệu
                DateTime _start = line.WorkPlan.PlanStart;
                DateTime _finish = line.WorkPlan.PlanFinish;
                DateTime eventTime = DateTime.Now;
                decimal _planDuration = (decimal)(_finish - _start).TotalSeconds;
                WorkPlan workPlan = line.WorkPlan;
                using (Entities _dbContext = new Entities())
                {
                    MES_REPORT_LINE reportLine = _dbContext.MES_REPORT_LINE.FirstOrDefault(x => x.WORK_PLAN_ID == WorkPlanId);
                    List<MES_REPORT_LINE_DETAIL> reportLineDetails = _dbContext.MES_REPORT_LINE_DETAIL.Where(x => x.WORK_PLAN_ID == WorkPlanId).ToList();
                    //Tồn tại - Tức là đang chạy dở dang rồi thì đã có tính toán rồi, Load lại
                    if (reportLine != null)
                    {
                        _Logger.Write(_LogCategory, $"Running Start at Line {LineId} - WorkPlan {workPlan.WORK_PLAN_ID} - WorkPlanDetail: {line.WorkPlan.WorkPlanDetails.Count} ", LogType.Debug);

                        line.ReportLine = reportLine;
                        if (reportLineDetails.Count > 0)
                        {
                            foreach (MES_REPORT_LINE_DETAIL reportLineDetail in reportLineDetails)
                            {
                                //reportLineDetail.Status = (int)PlanStatus.Proccessing;
                                line.ReportLineDetails.Add(reportLineDetail);
                            }
                        }
  
                        //Build TimeData cho nó nữa
                        BuildTimeData(LineId);

                    }
                    else
                    {
                        //Chưa có ==> Xử lý từ thằng Plan đi
                        DateTime actualStartPlan = (DateTime)workPlan.PlanStart;

                        //Cho các kế hoạch vào để chạy
                        ProcessWorkPlanDetails(line.LINE_ID, out actualStartPlan);
                    }
                }
              
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reset Line Report for Line {LineId} Error: {ex}", LogType.Error);
            }
        }
        private void ResetNode(string LineId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                foreach (Node node in line.Nodes)
                {
                    node.NodeEvents.Clear();
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reset Node for Line {LineId} Error: {ex}", LogType.Error);
            }
        }
        private void ResetMessageLine(string LineId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);

                using (Entities _dbContext = new Entities())
                {
                    //Khởi tạo thì phải xóa đi đống thông báo là line đó đi
                    _dbContext.Configuration.AutoDetectChangesEnabled = false;
                    MES_MSG_LINE msgLine = _dbContext.MES_MSG_LINE.FirstOrDefault(x => x.LINE_ID == LineId);
                    DM_MES_EVENTDEF noPlan = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_NOPLAN);
                    if (msgLine != null)
                    {
                          msgLine.EVENTDEF_ID = noPlan.EVENTDEF_ID;
                          msgLine.EVENTDEF_NAME_VN = noPlan.EVENTDEF_NAME_VN;
                          msgLine.EVENTDEF_NAME_EN = noPlan.EVENTDEF_NAME_EN;
                          msgLine.EVENTDEF_COLOR = noPlan.EVENTDEF_COLOR;
                          msgLine.PRODUCT_ID = "";
                          msgLine.PRODUCT_CODE = "";
                          msgLine.PRODUCT_NAME = "";
                          msgLine.PRODUCT_CATEGORY_ID = "";
                          msgLine.PRODUCT_CATEGORY_CODE = "";
                          msgLine.PRODUCT_CATEGORY_NAME = "";
                          msgLine.HEAD_COUNT = 0;
                          msgLine.TAKT_TIME = 0;
                          msgLine.TOTAL_PLAN_QUANTITY = 0;
                          msgLine.PLAN_QUANTITY = 0;
                          msgLine.TARGET_QUANTITY = 0;
                          msgLine.ACTUAL_QUANTITY = 0;
                          msgLine.ACTUAL_NG_QUANTITY = 0;
                          msgLine.STOP_DURATION = 0;
                          msgLine.TOTAL_STOP_DURATION = 0;
                          msgLine.NUMBER_OF_STOP = 0;
                          msgLine.PLAN_RATE = 0;
                          msgLine.TARGET_RATE = 0;
                          msgLine.TIME_RATE = 0;
                          msgLine.QUALITY_RATE = 0;
                          msgLine.OEE = 0;
                          msgLine.CURRENT_DETAIL = 0;
                          msgLine.TIME_UPDATED = DateTime.Now;

                        _dbContext.Entry(msgLine).State = System.Data.Entity.EntityState.Modified;
                    }

                    List<MES_MSG_LINE_WORKING> msgLineWorkings = _dbContext.MES_MSG_LINE_WORKING.Where(x => x.LINE_ID == LineId).ToList();
                    foreach(MES_MSG_LINE_WORKING msgLineWorking in msgLineWorkings)
                    {
                        msgLineWorking.DURATION = 0;
                        _dbContext.Entry(msgLineWorking).State = System.Data.Entity.EntityState.Modified;
                    }

                    List<MES_MSG_LINE_STOP> msgLineStops = _dbContext.MES_MSG_LINE_STOP.Where(x => x.LINE_ID == LineId).ToList();
                    foreach (MES_MSG_LINE_STOP msgLineStop in msgLineStops)
                    {
                        msgLineStop.DURATION = 0;
                        _dbContext.Entry(msgLineStop).State = System.Data.Entity.EntityState.Modified;
                    }

                    List<MES_MSG_LINE_EVENT> msgLineEvents = _dbContext.MES_MSG_LINE_EVENT.Where(x => x.LINE_ID == LineId).ToList();
                    _dbContext.MES_MSG_LINE_EVENT.RemoveRange(msgLineEvents);
                    List<MES_MSG_LINE_DETAIL> msgLineDetails = _dbContext.MES_MSG_LINE_DETAIL.Where(x => x.LINE_ID == LineId).ToList();
                    _dbContext.MES_MSG_LINE_DETAIL.RemoveRange(msgLineDetails);
                    List<MES_MSG_LINE_PRODUCT> msgLineProducts = _dbContext.MES_MSG_LINE_PRODUCT.Where(x => x.LINE_ID == LineId).ToList();
                    _dbContext.MES_MSG_LINE_PRODUCT.RemoveRange(msgLineProducts);
                    //Xóa cho NODE
                    foreach (Node node in line.Nodes)
                    {
                        List<MES_MSG_NODE_WORKING> msgNodeWorkings = _dbContext.MES_MSG_NODE_WORKING.Where(x => x.NODE_ID == node.NODE_ID).ToList();
                        _dbContext.MES_MSG_NODE_WORKING.RemoveRange(msgNodeWorkings);
                        List<MES_MSG_NODE_STOP> msgNodeStops = _dbContext.MES_MSG_NODE_STOP.Where(x => x.NODE_ID == node.NODE_ID).ToList();
                        _dbContext.MES_MSG_NODE_STOP.RemoveRange(msgNodeStops);
                        List<MES_MSG_NODE_EVENT> msgNodeEvents = _dbContext.MES_MSG_NODE_EVENT.Where(x => x.NODE_ID == node.NODE_ID).ToList();
                        _dbContext.MES_MSG_NODE_EVENT.RemoveRange(msgNodeEvents);
                    }

                    _dbContext.SaveChanges();
                    _dbContext.Configuration.AutoDetectChangesEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reset Message Line of Line {LineId} Error: {ex}", LogType.Error);
            }
        }
        private void ResetRunningLine(string LineId)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);
                DM_MES_EVENTDEF noPlan = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_NOPLAN);

                line.EventDefId = noPlan.EVENTDEF_ID;
                line.EventDefName_VN = noPlan.EVENTDEF_NAME_VN;
                line.EventDefName_EN = noPlan.EVENTDEF_NAME_EN;
                line.EventDefColor = noPlan.EVENTDEF_COLOR;
                //Xóa bỏ hết cũ
                line.WorkPlan = null;
                line.ReportLine = null;
                line.BreakTimes.Clear();
                line.TimeDatas.Clear();
                line.LineEvents.Clear();
                line.LineWorkings.Clear();
                line.LineStops.Clear();
                line.ReportLineDetails.Clear();
                line.CurrentDetail = 0;
                line.Changed = DateTime.Now;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reset Running Status of Line {LineId} Error: {ex}", LogType.Error);
            }
        }
        #endregion

        #region TimeProcessFunction
        /// <summary>
        /// Khởi tạo bộ thời gian theo thứ tự A,B,C,D,E
        /// </summary>
        /// <param name="LineId"></param>
        /// <param name="isReset"></param>
        private void BuildTimeData(string LineId, bool isReset = true)
        {
            try
            {
                Line line = _Lines.FirstOrDefault(l => l.LINE_ID == LineId);

                if (isReset)
                {
                    line.TimeDatas = null;
                }
                else
                {
                    if (line.TimeDatas != null) { return; }
                }

                DateTime _start = line.WorkPlan.PlanStart;
                DateTime _finish = line.WorkPlan.PlanFinish;
                WorkPlan workPlan = line.WorkPlan;

                //Khởi tạo bộ BreakTime
                line.BreakTimes.Clear();
                List<DM_MES_BREAK_TIME> breakTimes = _BreakTimes.Where(x => x.SHIFT_ID == workPlan.SHIFT_ID).ToList();
                //DateTime eventTime = Num2Time(workPlan.Day, DayArchive);
                foreach (DM_MES_BREAK_TIME breakTime in breakTimes)
                {
                    string _apply = breakTime.APPLY_LINES;
                    if (!_apply.StartsWith(";")) _apply = ";" + _apply;
                    if (!_apply.EndsWith(";")) _apply += ";";

                    if (_apply.Contains(";" + line.LINE_ID + ";"))
                    {
                        BreakTime time = new BreakTime();
                        time.Cast(breakTime);
                        line.BreakTimes.Add(time);
                    }
                }
                line.BreakTimes = line.BreakTimes.OrderBy(x => x.StartTime).ToList();


                //Khởi tạo TimeData
                if (line.TimeDatas == null)
                {
                    //Bắt đầu build TimeSlot
                    List<BreakTime> tblBreakTimes = line.BreakTimes.Where(x => x.SHIFT_ID == workPlan.SHIFT_ID).ToList();
                    List<TimeData> times = new List<TimeData>();
                    DateTime _startTimeSlot = _start;
                    int i = Consts.START_TIME_SLOT;
                    decimal _duration = 0;
                    foreach (BreakTime tblBreakTime in tblBreakTimes)
                    {
                        DateTime _finishTimeSlot = ProcessTimeInWorkPlan(tblBreakTime.START_HOUR, tblBreakTime.START_MINUTE, workPlan.DAY);
                        _duration = (decimal)(_finishTimeSlot - _startTimeSlot).TotalSeconds;
                        TimeData _time = new TimeData()
                        {
                            TimeName = Convert.ToChar(i).ToString(),
                            Start = _startTimeSlot,
                            Finish = _finishTimeSlot,
                            Duration = _duration,
                        };
                        times.Add(_time);
                        _startTimeSlot = ProcessTimeInWorkPlan(tblBreakTime.FINISH_HOUR, tblBreakTime.FINISH_MINUTE, workPlan.DAY);
                        i++;

                        tblBreakTime.StartTime = _finishTimeSlot;
                        tblBreakTime.FinishTime = _startTimeSlot;
                        tblBreakTime.Duration = (decimal)(_startTimeSlot - _finishTimeSlot).TotalSeconds;

                    }
                    //Thằng cuối cùng
                    _duration = (decimal)(_finish - _startTimeSlot).TotalSeconds;
                    TimeData time = new TimeData()
                    {
                        TimeName = Convert.ToChar(i).ToString(),
                        Start = _startTimeSlot,
                        Finish = _finish,
                        Duration = _duration,
                    };
                    times.Add(time);
                    line.TimeDatas = times;
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Build TimeData Error: {ex}", LogType.Error);
            }
        }
        private bool TestInBreakTime(string LineId, DateTime eventTime)
        {
            bool isOk = false;
            try
            {

                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);

                foreach (BreakTime breakTime in line.BreakTimes)
                {
                    //Bỏ qua những thằng cũ
                    if (breakTime.FinishTime < eventTime) continue;

                    if (eventTime >= breakTime.StartTime && eventTime <= breakTime.FinishTime)
                    {
                        isOk = true; break;
                    }
                }
                return isOk;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Test in Break of Line {LineId} - Time {eventTime:yyyy-MM-dd HH:mm:ss} Error: {ex}", LogType.Error);
            }
            return false;
        }
        private DateTime ProcessTimeInWorkPlan(int startHour, int startMinute, decimal day)
        {
            try
            {
                DateTime dateTime = Num2Time(day, DayArchive);
                DateTime ret = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, startHour, startMinute, 0);
                if (startHour < Consts.HOUR_FOR_NEW_DAY)
                {
                    ret = ret.AddDays(1);
                }
                return ret;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process Time {startHour:D2}:{startMinute:D2} In Day {day} Error: {ex}", LogType.Error);
            }
            return DateTime.MinValue;

        }
        private Shift CheckShift(DateTime checkTime)
        {
            try
            {
                DateTime eventTime = checkTime;

                if (eventTime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    eventTime = eventTime.AddDays(-1);
                }
                decimal _fullday = Time2Num(eventTime, DayArchive);
                //_Logger.Write(_LogCategory, $"Start Check Shift for {checkTime}", LogType.Debug);

                List<Shift> checkShifts = new List<Shift>();
                foreach (DG_DM_SHIFT shift in _Shifts)
                {
                    Shift retShift = new Shift();
                    retShift.Cast(shift, _fullday);
                    checkShifts.Add(retShift);
                }
                //Lấy từ lớn đến bé
                checkShifts = checkShifts.OrderByDescending(x => x.Start).ToList();

                foreach (Shift _shift in checkShifts)
                {
                    if (checkTime >= _shift.Start && checkTime < _shift.Finish)
                    {
                        return _shift;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Check Shift Error: {ex}", LogType.Error);
            }
            return null;
        }
        private List<Shift> CheckShiftList(DateTime checkTime)
        {
            try
            {
                List<Shift> model = new List<Shift>();

                DateTime eventTime = checkTime;

                if (eventTime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    eventTime = eventTime.AddDays(-1);
                }
                decimal _fullday = Time2Num(eventTime, DayArchive);
                //_Logger.Write(_LogCategory, $"Start Check Shift for {checkTime}", LogType.Debug);

                List<Shift> checkShifts = new List<Shift>();
                foreach (DG_DM_SHIFT shift in _Shifts)
                {
                    Shift retShift = new Shift();
                    retShift.Cast(shift, _fullday);
                    checkShifts.Add(retShift);
                }
                //Lấy từ lớn đến bé
                checkShifts = checkShifts.OrderByDescending(x => x.Start).ToList();

                foreach (Shift _shift in checkShifts)
                {
                    if (checkTime >= _shift.Start && checkTime < _shift.Finish)
                    {
                        model.Add(_shift);
                    }
                }

                return model.OrderByDescending(x => x.Start).ToList();
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Check Shift Error: {ex}", LogType.Error);
            }
            return null;
        }
        private Shift CheckShift(decimal _date, string _shift)
        {
            DateTime checkTime = Num2Time(_date, DayArchive);

            Shift retShift = new Shift();
            try
            {
                foreach (DG_DM_SHIFT shift in _Shifts)
                {
                    if (shift.SHIFT_ID == _shift)
                    {
                        DateTime _startShift = new DateTime(checkTime.Year, checkTime.Month, checkTime.Day, shift.HOUR_START, shift.MINUTE_START, 0);
                        DateTime _finishShift = new DateTime(checkTime.Year, checkTime.Month, checkTime.Day, shift.HOUR_END, shift.MINUTE_END, 0);
                        if (_finishShift < _startShift)
                        {
                            _finishShift = _finishShift.AddDays(1);
                        }
                        retShift.Start = _startShift;
                        retShift.Finish = _finishShift.AddMinutes(0 - Consts.BUFFER_TIME_IN_MINUTE);

                        foreach (DM_MES_BREAK_TIME tblBreakTime in _BreakTimes)
                        {
                            if (tblBreakTime.SHIFT_ID == shift.SHIFT_ID)
                            {
                                BreakTime breakTime = new BreakTime();
                                breakTime.Cast(tblBreakTime, _date);
                                //Check thêm quả cuối cùng

                                if (breakTime.FinishTime > retShift.Finish)
                                {
                                    breakTime.FinishTime = retShift.Finish;
                                }
                                retShift.BreakTimes.Add(breakTime);

                            }
                        }
                    }
                }
                return retShift;
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Check Shift Error: {ex}", LogType.Error);
            }
            return retShift;
        }
        private List<BreakTime> GetBreakTimes(string LineId, decimal _day, string ShiftId)
        {
            List<BreakTime> model = new List<BreakTime>();
            try
            {
                List<DM_MES_BREAK_TIME> breakTimes = _BreakTimes.Where(x => x.SHIFT_ID == ShiftId).ToList();
                foreach (DM_MES_BREAK_TIME tblBreakTime in breakTimes)
                {
                    string _apply = tblBreakTime.APPLY_LINES;
                    if (!_apply.StartsWith(",")) _apply = "," + _apply;
                    if (!_apply.EndsWith(",")) _apply += ",";

                    if (_apply.Contains("," + LineId + ","))
                    {
                        BreakTime breakTime = new BreakTime();
                        breakTime.Cast(tblBreakTime, _day);
                        model.Add(breakTime);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Load BreakTime for Line {LineId} Shift {ShiftId} Error: {ex}", LogType.Error);
            }
            return model;
        }
        private decimal GetBreakDuration(string LineId, DateTime _start, DateTime _finish)
        {
            decimal _duration = 0;
            try
            {
                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);

                List<BreakTime> breakTimes = line.BreakTimes;

                foreach (BreakTime breakTime in breakTimes)
                {
                    if (_start < breakTime.FinishTime && _finish > breakTime.StartTime)
                    {
                        DateTime _checkStart = (_start > breakTime.StartTime) ? _start : breakTime.StartTime;
                        DateTime _checkFinish = (_finish < breakTime.FinishTime) ? _finish : breakTime.FinishTime;

                        _duration += (decimal)(_checkFinish - _checkStart).TotalSeconds;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Get BreakTime duration for Line {LineId} Start [{_start:yyyy-MM-dd HH:mm:ss}] Finish [{_finish:yyyy-MM-dd HH:mm:ss}] Error: {ex}", LogType.Error);
            }
            return _duration;
        }
        private decimal GetLineStopDuration(string LineId, DateTime eventTime)
        {
            decimal _duration = 0;
            try
            {
                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);

                if (line.LineEvents.Count == 0) return 0;

                MES_LINE_EVENT tblLineEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);
                tblLineEvent.TOTAL_DURATION = (decimal)(eventTime - tblLineEvent.START).TotalSeconds;

                foreach (MES_LINE_WORKING lineRunning in line.LineWorkings)
                {
                    List<MES_LINE_EVENT> lineEvents = line.LineEvents.Where(x => x.EVENTDEF_ID == lineRunning.EVENTDEF_ID).ToList();
                    if (lineEvents.Count > 0)
                    {
                        lineRunning.DURATION = lineEvents.Sum(x => x.TOTAL_DURATION);
                    }
                    else
                    {
                        lineRunning.DURATION = 0;
                    }
                    if ((lineRunning.EVENTDEF_ID != Consts.EVENTDEF_DEFAULT) && (lineRunning.EVENTDEF_ID != Consts.EVENTDEF_RUNNING) && (lineRunning.EVENTDEF_ID != Consts.EVENTDEF_BREAK) && (lineRunning.EVENTDEF_ID != Consts.EVENTDEF_NOPLAN))
                    {
                        _duration += lineRunning.DURATION;
                    }
                }
                //Tính từng loại STOP
                foreach (MES_LINE_STOP lineStop in line.LineStops)
                {
                    List<MES_LINE_EVENT> lineEvents = line.LineEvents.Where(x => x.EVENTDEF_ID == Consts.EVENTDEF_STOP && x.REASON_ID == lineStop.REASON_ID).ToList();
                    if (lineEvents.Count > 0)
                    {
                        lineStop.DURATION = lineEvents.Sum(x => x.TOTAL_DURATION);
                    }
                    else
                    {
                        lineStop.DURATION = 0;
                    }
                }


            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Get Stop duration for Line {LineId} Error: {ex}", LogType.Error);
            }
            return _duration;

        }
        private decimal GetLineStopDuration(string LineId, DateTime _start, DateTime _finish, DateTime eventTime, out int NumberOfStop, out decimal BreakDuration)
        {
            decimal _stopDuration = 0;
            BreakDuration = 0;
            NumberOfStop = 0;
            try
            {
                Line line = _Lines.FirstOrDefault(x => x.LINE_ID == LineId);
                if (line.LineEvents.Count == 0) return 0;

                //Xử lý thằng cuối cùng
                MES_LINE_EVENT lastLineEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);
                if (lastLineEvent != null)
                {
                    lastLineEvent.TOTAL_DURATION = (decimal)(eventTime - lastLineEvent.START).TotalSeconds;
                }
                foreach (MES_LINE_EVENT tblLineEvent in line.LineEvents)
                {
                    if ((tblLineEvent.START < _finish && tblLineEvent.FINISH > _start) || (tblLineEvent.START < _finish && !tblLineEvent.FINISH.HasValue))
                    {
                        DateTime _eventStart = (tblLineEvent.START > _start) ? tblLineEvent.START : _start;
                        DateTime _eventFinish = _finish;
                        if (!tblLineEvent.FINISH.HasValue)
                        {
                            _eventFinish = eventTime;
                        }
                        else
                        {
                            _eventFinish = (DateTime)tblLineEvent.FINISH;
                        }
                        _eventFinish = (_eventFinish < _finish) ? _eventFinish : _finish;

                        decimal _duration = (decimal)(_eventFinish - _eventStart).TotalSeconds;

                        if ((tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_DEFAULT) && (tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_RUNNING) && (tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_BREAK) && (tblLineEvent.EVENTDEF_ID != Consts.EVENTDEF_NOPLAN))
                        {
                            _stopDuration += _duration;
                            NumberOfStop++;
                        }
                        if (tblLineEvent.EVENTDEF_ID == Consts.EVENTDEF_BREAK)
                        {
                            BreakDuration += _duration;
                        }

                    }
                }
                //Tính từng loại hoạt động
                foreach (MES_LINE_WORKING lineRunning in line.LineWorkings)
                {
                    lineRunning.DURATION = 0;
                    List<MES_LINE_EVENT> lineEvents = line.LineEvents.Where(x => x.EVENTDEF_ID == lineRunning.EVENTDEF_ID).ToList();
                    if (lineEvents.Count > 0)
                    {
                        lineRunning.DURATION = lineEvents.Sum(x => x.TOTAL_DURATION);
                    }
                }
                //Tính từng loại STOP
                foreach (MES_LINE_STOP lineStop in line.LineStops)
                {
                    lineStop.DURATION = 0;
                    List<MES_LINE_EVENT> lineEvents = line.LineEvents.Where(x => x.EVENTDEF_ID == Consts.EVENTDEF_STOP && x.REASON_ID == lineStop.REASON_ID).ToList();
                    if (lineEvents.Count > 0)
                    {
                        lineStop.DURATION = lineEvents.Sum(x => x.TOTAL_DURATION);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Get Stop duration and Break for Line {LineId} Error: {ex}", LogType.Error);
            }
            return _stopDuration;

        }

        #endregion

        #region Reload
        private void ReloadConfigurations()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    lock (_Shifts)
                    {
                        _Shifts = _dbContext.DG_DM_SHIFT.ToList();
                    }
                    lock (_BreakTimes)
                    {
                        _BreakTimes = _dbContext.DM_MES_BREAK_TIME.ToList();
                    }
                    lock (_Configurations)
                    {
                        _Configurations = _dbContext.DM_MES_CONFIGURATION.ToList();
                    }

                    ReloadProducts();
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload Configurations Error: {ex}", LogType.Error);
            }
        }

        private void ReloadProducts()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    lock (_Products)
                    {
                        _Products = _dbContext.DM_MES_PRODUCT.Where(x => x.ACTIVE).ToList();
                    }
                    if (_UseProductConfig)
                    {
                        lock (_ProductConfigs)
                        {
                            _ProductConfigs = _dbContext.DM_MES_PRODUCT_CONFIG.ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload Configurations Error: {ex}", LogType.Error);
            }
        }
        private void ReloadShifts()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    lock (_Shifts)
                    {
                        _Shifts = _dbContext.DG_DM_SHIFT.ToList();
                    }
                    lock (_BreakTimes)
                    {
                        _BreakTimes = _dbContext.DM_MES_BREAK_TIME.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload Shift and BreakTime Error: {ex}", LogType.Error);
            }
        }
        private void ReloadUpdateConfig()
        {
            //_Logger.Write(_LogCategory, $"Reload Report detail to update Config", LogType.Debug);
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    DateTime eventTime = DateTime.Now;

                    List<MES_TMP_UPDATE_CONFIG> actualRawDatas = _dbContext.MES_TMP_UPDATE_CONFIG.ToList();

                    if (actualRawDatas.Count > 0)
                    {
                        _Logger.Write(_LogCategory, $"Update Config: count {actualRawDatas.Count}", LogType.Debug);
                        foreach (MES_TMP_UPDATE_CONFIG rawData in actualRawDatas)
                        {
                            foreach (Line line in _Lines)
                            {
                                if (line.ReportLineDetails.Count > 0)
                                {
                                    MES_REPORT_LINE_DETAIL detail = line.ReportLineDetails.FirstOrDefault(x => x.REPORT_LINE_DETAIL_ID == rawData.REPORT_LINE_DETAIL_ID);

                                    if (detail != null)
                                    {
                                        //if (rawData.TAKT_TIME > 0) detail.PLAN_TAKT_TIME = rawData.TAKT_TIME;
                                        //if (rawData.HEADCOUNT > 0) detail.HEAD_COUNT = (int)rawData.HEADCOUNT;
                                        if (rawData.TAKT_TIME > 0) detail.RUNNING_TAKT_TIME = rawData.TAKT_TIME;
                                        if (rawData.HEADCOUNT > 0) detail.RUNNING_HEAD_COUNT = (int)rawData.HEADCOUNT;
                                        _Logger.Write(_LogCategory, $"Update Config done for {detail.REPORT_LINE_DETAIL_ID}: Headcount {detail.RUNNING_HEAD_COUNT}, Takttime {detail.PLAN_TAKT_TIME}", LogType.Debug);
                                    }
                                }
                            }
                        }
                    }

                    //Làm xong xóa
                    _dbContext.MES_TMP_UPDATE_CONFIG.RemoveRange(actualRawDatas);
                    _dbContext.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload ReportDetail update Error: {ex}", LogType.Error);
            }
        }
        private void ReloadEvents()
        {
            try
            {
                _Logger.Write(_LogCategory, $"Reload Event to update Reason", LogType.Debug);
                using (Entities _dbContext = new Entities())
                {
                    foreach (Line line in _Lines)
                    {
                        if (line.WorkPlan != null)
                        {
                            if (line.WorkPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                            {
                                //Lấy những ông STOP
                                //Chỉ lấy những ông Stop
                                List<MES_LINE_EVENT> eventRawDatas = _dbContext.MES_LINE_EVENT.Where(x=>x.WORK_PLAN_ID == line.WorkPlan.WORK_PLAN_ID && x.EVENTDEF_ID == Consts.EVENTDEF_STOP).ToList();
                                foreach(MES_LINE_EVENT rawData in eventRawDatas)
                                {
                                    MES_LINE_EVENT updateEvent = line.LineEvents.FirstOrDefault(x=>x.EVENT_ID == rawData.EVENT_ID);
                                    if (updateEvent != null)
                                    {
                                        updateEvent.REASON_ID = rawData.REASON_ID;
                                        updateEvent.RESPONSIBILITY_ID = rawData.RESPONSIBILITY_ID;
                                    }
                                }
                            }
                        }
                    }

                    //Lấy những ông bấm từ trên WEB

                    List<MES_TMP_EVENT_RAW_DATA> eventRawPushs = _dbContext.MES_TMP_EVENT_RAW_DATA.ToList();
                    if (eventRawPushs.Count > 0)
                    {
                        DateTime eventTime = DateTime.Now;

                        foreach (MES_TMP_EVENT_RAW_DATA eventRawPush in eventRawPushs)
                        {
                            
                            DateTime pushTime = eventRawPush.START_TIME;
                            if (pushTime > eventTime) continue;
                            //Chỉ lấy trong 30s gần nhất
                            if ((eventTime - pushTime).TotalSeconds > Consts.VERIFY_EVENT) continue;


                            Line line = _Lines.FirstOrDefault(l => l.LINE_ID == eventRawPush.LINE_ID);

                            if (!TestInBreakTime(line.LINE_ID, pushTime))
                            {
                                MES_LINE_EVENT lastEvent = line.LineEvents.Last(x => !x.FINISH.HasValue);
                                DM_MES_EVENTDEF eventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID == Consts.EVENTDEF_RUNNING);
                                string eventDefId = Consts.EVENTDEF_RUNNING;
                                if (lastEvent.EVENTDEF_ID != eventRawPush.EVENTDEF_ID)
                                {
                                    eventDefId = eventRawPush.EVENTDEF_ID;
                                    eventDef = _EventDefs.FirstOrDefault(x => x.EVENTDEF_ID== eventRawPush.EVENTDEF_ID);
                                }

                                ChangeLineEvent(line.LINE_ID, pushTime, eventDefId);
                            }
                        }

                        //Xóa luôn trong DB
                        _dbContext.MES_TMP_EVENT_RAW_DATA.RemoveRange(eventRawPushs);
                        _dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload Event For Update Reason Error: {ex}", LogType.Error);
            }
        }
        private void ReloadNodes()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    List<DM_MES_NODE> _Nodes = _dbContext.DM_MES_NODE.Where(n => n.ACTIVE).ToList();

                    foreach (Line line in _Lines)
                    {
                        //foreach (Node node in line.Nodes)
                        //{
                        //    tblNode tblNode = _Nodes.FirstOrDefault(n => n.Id == node.Id);
                        //    node.Min_OnOff = tblNode.Min_OnOff;
                        //    node.Min_CycleTime = tblNode.Min_CycleTime;
                        //}
                    }

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload Nodes Error: {ex}", LogType.Error);
            }
        }
        private void ReloadReportDetail()
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    DateTime eventTime = DateTime.Now;

                    List<MES_TMP_UPDATE_REPORT_LINE_DETAIL> actualRawDatas = _dbContext.MES_TMP_UPDATE_REPORT_LINE_DETAIL.ToList();

                    if (actualRawDatas.Count > 0)
                    {
                        foreach (Line line in _Lines)
                        {
                            WorkPlan workPlan = line.WorkPlan;
                            if (workPlan != null)
                            {
                                List<MES_TMP_UPDATE_REPORT_LINE_DETAIL> sublist = actualRawDatas.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID).ToList();
                                foreach (MES_TMP_UPDATE_REPORT_LINE_DETAIL lineDetail in sublist)
                                {
                                    MES_REPORT_LINE_DETAIL reportLineDetail = line.ReportLineDetails.FirstOrDefault(x => x.REPORT_LINE_DETAIL_ID == lineDetail.REPORT_LINE_DETAIL_ID);
                                    //tblReportLineDetail reportLineDetail = line.ReportLineDetails.FirstOrDefault(l => l.TimeSlot == lineDetail.TimeSlot && l.WorkPlanId == lineDetail.WorkPlanId && l.WorkPlanDetailId == lineDetail.WorkPlanDetailId && l.LineId == l.LineId);

                                    if (reportLineDetail != null)
                                    {
                                        if (reportLineDetail.PLAN_START < eventTime)
                                        {
                                            //Lấy 2 giá trị mới nhập
                                            reportLineDetail.ACTUAL_QUANTITY = lineDetail.ACTUAL_QUANTITY;
                                            reportLineDetail.ACTUAL_NG_QUANTITY = lineDetail.ACTUAL_NG_QUANTITY;
                                            //Chỉ những thằng nào đã chạy thì mới làm
                                            //if (lineDetail.Status == 1)
                                            //{
                                            //    reportLineDetail.Status = (byte)PlanStatus.Ready2Done;
                                            //}
                                            //else
                                            //{
                                            //    //Uncheck thì lại mở ra
                                            //    reportLineDetail.Status = (byte)PlanStatus.Proccessing;
                                            //}
                                        }
                                    }
                                }
                            }
                        }
                        //Làm xong xóa
                        _dbContext.MES_TMP_UPDATE_REPORT_LINE_DETAIL.RemoveRange(actualRawDatas);
                        _dbContext.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Reload ReportDetail update Error: {ex}", LogType.Error);
            }
        }
        #endregion

        #region Rem-Unused-Code
        /*
        private void ReLoadWorkPlans()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                if (eventTime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    eventTime = eventTime.AddDays(-1);
                }
                decimal _day = Time2Num(eventTime, DayArchive);
                _Logger.Write(_LogCategory, $"Reload WorkPlan....", LogType.Debug);

                //Lấy kế hoạch mới bổ sung, hiệu chỉnh vào
                using (Entities _dbContext = new Entities())
                {

                    //==========================================================================================
                    //Bước 1. Ban đầu lấy ra những ông mới bổ sung vào
                    List<MES_WORK_PLAN> tblWorkPlans = _dbContext.MES_WORK_PLAN.Where(w => w.STATUS < (int)PLAN_STATUS.NotStart).ToList();
                    List<MES_WORK_PLAN_DETAIL> tblWorkPlanDetails = _dbContext.MES_WORK_PLAN_DETAIL.Where(w => w.STATUS < (int)PLAN_STATUS.NotStart).ToList();
                    lock (_WorkPlans)
                    {
                        foreach (MES_WORK_PLAN tblWorkPlan in tblWorkPlans)
                        {
                            WorkPlan workPlan = new WorkPlan().Cast(tblWorkPlan);

                            //Đặt trạng thái cho WorkPlan --> trạng thái của WorkPlanDetail sẽ ăn theo
                            if (workPlan.STATUS == (int)PLAN_STATUS.Draft)
                            {
                                workPlan.STATUS = (int)PLAN_STATUS.NotStart;
                            }


                            Shift shift = CheckShift(tblWorkPlan.DAY, tblWorkPlan.SHIFT_ID);

                            workPlan.PlanStart = shift.Start;
                            workPlan.PlanFinish = shift.Finish;

                            List<MES_WORK_PLAN_DETAIL> subList = tblWorkPlanDetails.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID).ToList();
                            workPlan.WorkPlanDetails.AddRange(subList); //_dbContext.tblWorkPlanDetails.Where(x => x.WorkPlanId == workPlan.Id).ToList();

                            //Kiểm tra xem có phải nó đang bị hết hạn không
                            if (workPlan.PlanFinish < eventTime)
                            {
                                workPlan.STATUS = (int)PLAN_STATUS.Timeout;
                                workPlan.Priority = 1; //Đánh dấu để xóa
                            }
                            //Check trùng lắp
                            WorkPlan _check = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID == workPlan.WORK_PLAN_ID);
                            if (_check != null)
                            {
                                _WorkPlans.Remove(_check);
                            }

                            _WorkPlans.Add(workPlan);

                            //Xóa bỏ những thằng thuộc các WorkPlan này
                            tblWorkPlanDetails.RemoveAll(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID);
                        }

                        //Trường hợp vẫn còn lại Detail thêm vào sau khi đã có WorkPlan thì xử lý thêm vào sau
                        if (tblWorkPlanDetails.Count > 0)
                        {
                            foreach (var workPlan in _WorkPlans)
                            {
                                //Nếu chưa chạy hoặc đang chạy thì thêm vào bình thường
                                if (workPlan.STATUS <= (int)PLAN_STATUS.Proccessing)
                                {
                                    //Tính toán trường hợp đã add rồi thì phải tính toán lại 
                                    List<MES_WORK_PLAN_DETAIL> subList = tblWorkPlanDetails.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID).ToList();

                                    //Trước khi thêm vào thì phải check đã
                                    foreach (MES_WORK_PLAN_DETAIL planDetail in subList)
                                    {
                                        MES_WORK_PLAN_DETAIL checkItem = workPlan.WorkPlanDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == planDetail.WORK_PLAN_DETAIL_ID);
                                        if (checkItem != null)
                                        {
                                            workPlan.WorkPlanDetails.Remove(checkItem);
                                        }
                                        workPlan.WorkPlanDetails.Add(planDetail);
                                    }
                                    //Trường hợp đang chạy mà thêm kế hoạch vào thì phải bốc hàng lên ngay
                                    if (workPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                                    {
                                        //Xử lý nó
                                        ProcessPlanDetailWhenRunning(subList);
                                    }
                                }
                            }
                        }

                    }


                    //=========================================================================================
                    //Bước 2: Đối với những thằng đang chạy mà hiệu chỉnh/hủy
                    //Lấy từ bảng tạm

                    //List<tblWorkPlanDetail> updatedWorkPlanDetails = new List<tblWorkPlanDetail>();
                    //List<WorkPlan> runningWorkPlans = _WorkPlans.Where(x => x.Status == (int)PlanStatus.Proccessing).ToList();
                    //List<tblWorkPlanRawData> updatedWorkPlanDetails = _dbContext.tblWorkPlanRawDatas.ToList();
                    //_dbContext.tblWorkPlanRawDatas.RemoveRange(updatedWorkPlanDetails);
                    //_dbContext.SaveChanges();
                    //ProcessChangeWorkPlan(updatedWorkPlanDetails);

                    //foreach (tblWorkPlanRawData rawData in updatedWorkPlanDetails)
                    //{
                    //    //Lấy ra những thằng vừa sửa hoặc Hủy
                    //    ProcessChangeWorkPlan(workPlan, updatedWorkPlanDetails);
                    //}
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ReLoad WorkPlan Error: {ex}, try to restart service again.", LogType.Error);
                //Stop();
            }

        }
        private void ReLoadWorkPlanDetails()
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                if (eventTime.Hour < Consts.HOUR_FOR_NEW_DAY)
                {
                    eventTime = eventTime.AddDays(-1);
                }
                decimal _day = Time2Num(eventTime, DayArchive);
                _Logger.Write(_LogCategory, $"Reload WorkPlan....", LogType.Debug);

                //Lấy kế hoạch mới bổ sung, hiệu chỉnh vào
                using (Entities _dbContext = new Entities())
                {

                    //==========================================================================================
                    //Bước 1. Ban đầu lấy ra những ông mới bổ sung vào
                    List<MES_WORK_PLAN> tblWorkPlans = _dbContext.MES_WORK_PLAN.Where(w => w.STATUS < (int)PLAN_STATUS.NotStart).ToList();
                    List<MES_WORK_PLAN_DETAIL> tblWorkPlanDetails = _dbContext.MES_WORK_PLAN_DETAIL.Where(w => w.STATUS < (int)PLAN_STATUS.NotStart).ToList();
                    lock (_WorkPlans)
                    {
                        foreach (MES_WORK_PLAN tblWorkPlan in tblWorkPlans)
                        {
                            WorkPlan workPlan = new WorkPlan().Cast(tblWorkPlan);

                            //Đặt trạng thái cho WorkPlan --> trạng thái của WorkPlanDetail sẽ ăn theo
                            if (workPlan.STATUS == (int)PLAN_STATUS.Draft)
                            {
                                workPlan.STATUS = (int)PLAN_STATUS.NotStart;
                            }


                            Shift shift = CheckShift(tblWorkPlan.DAY, tblWorkPlan.SHIFT_ID);

                            workPlan.PlanStart = shift.Start;
                            workPlan.PlanFinish = shift.Finish;

                            List<MES_WORK_PLAN_DETAIL> subList = tblWorkPlanDetails.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID).ToList();
                            workPlan.WorkPlanDetails.AddRange(subList); //_dbContext.tblWorkPlanDetails.Where(x => x.WorkPlanId == workPlan.Id).ToList();

                            //Kiểm tra xem có phải nó đang bị hết hạn không
                            if (workPlan.PlanFinish < eventTime)
                            {
                                workPlan.STATUS = (int)PLAN_STATUS.Timeout;
                                workPlan.Priority = 1; //Đánh dấu để xóa
                            }
                            //Check trùng lắp
                            WorkPlan _check = _WorkPlans.FirstOrDefault(wp => wp.WORK_PLAN_ID == workPlan.WORK_PLAN_ID);
                            if (_check != null)
                            {
                                _WorkPlans.Remove(_check);
                            }

                            _WorkPlans.Add(workPlan);

                            //Xóa bỏ những thằng thuộc các WorkPlan này
                            tblWorkPlanDetails.RemoveAll(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID);
                        }

                        //Trường hợp vẫn còn lại Detail thêm vào sau khi đã có WorkPlan thì xử lý thêm vào sau
                        if (tblWorkPlanDetails.Count > 0)
                        {
                            foreach (var workPlan in _WorkPlans)
                            {
                                //Nếu chưa chạy hoặc đang chạy thì thêm vào bình thường
                                if (workPlan.STATUS <= (int)PLAN_STATUS.Proccessing)
                                {
                                    //Tính toán trường hợp đã add rồi thì phải tính toán lại 
                                    List<MES_WORK_PLAN_DETAIL> subList = tblWorkPlanDetails.Where(x => x.WORK_PLAN_ID == workPlan.WORK_PLAN_ID).ToList();

                                    //Trước khi thêm vào thì phải check đã
                                    foreach (MES_WORK_PLAN_DETAIL planDetail in subList)
                                    {
                                        MES_WORK_PLAN_DETAIL checkItem = workPlan.WorkPlanDetails.FirstOrDefault(x => x.WORK_PLAN_DETAIL_ID == planDetail.WORK_PLAN_DETAIL_ID);
                                        if (checkItem != null)
                                        {
                                            workPlan.WorkPlanDetails.Remove(checkItem);
                                        }
                                        workPlan.WorkPlanDetails.Add(planDetail);
                                    }
                                    //Trường hợp đang chạy mà thêm kế hoạch vào thì phải bốc hàng lên ngay
                                    if (workPlan.STATUS == (int)PLAN_STATUS.Proccessing)
                                    {
                                        //Xử lý nó
                                        ProcessPlanDetailWhenRunning(subList);
                                    }
                                }
                            }
                        }

                    }


                    //=========================================================================================
                    //Bước 2: Đối với những thằng đang chạy mà hiệu chỉnh/hủy
                    //Lấy từ bảng tạm

                    //List<tblWorkPlanDetail> updatedWorkPlanDetails = new List<tblWorkPlanDetail>();
                    //List<WorkPlan> runningWorkPlans = _WorkPlans.Where(x => x.Status == (int)PlanStatus.Proccessing).ToList();
                    //List<tblWorkPlanRawData> updatedWorkPlanDetails = _dbContext.tblWorkPlanRawDatas.ToList();
                    //_dbContext.tblWorkPlanRawDatas.RemoveRange(updatedWorkPlanDetails);
                    //_dbContext.SaveChanges();
                    //ProcessChangeWorkPlan(updatedWorkPlanDetails);

                    //foreach (tblWorkPlanRawData rawData in updatedWorkPlanDetails)
                    //{
                    //    //Lấy ra những thằng vừa sửa hoặc Hủy
                    //    ProcessChangeWorkPlan(workPlan, updatedWorkPlanDetails);
                    //}
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"ReLoad WorkPlan Error: {ex}, try to restart service again.", LogType.Error);
                //Stop();
            }

        }
        private void ProcessChangeWorkPlan(List<tblWorkPlanRawData> updatedWorkPlanDetails)
        {
            try
            {
                using (Entities _dbContext = new Entities())
                {
                    foreach (tblWorkPlanRawData rawData in updatedWorkPlanDetails)
                    {
                        //Load lại kế hoạch vừa được sửa từ DB
                        tblWorkPlanDetail workPlanDetail = _dbContext.tblWorkPlanDetails.FirstOrDefault(x => x.Id == rawData.WorkPlanDetailId);

                        WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.Id == workPlanDetail.WorkPlanId);
                        Line line = _Lines.FirstOrDefault(x => x.Id == workPlan.LineId);

                        lock (workPlan)
                        {
                            bool isAdd = false;
                            if (rawData.Status == (int)Enums.UpdateOnline)
                            {
                                isAdd = true;
                                //Check để thay thế
                                tblWorkPlanDetail checkItem = workPlan.WorkPlanDetails.FirstOrDefault(x => x.Id == workPlanDetail.Id);
                                if (checkItem != null)
                                {
                                    workPlan.WorkPlanDetails.Remove(checkItem);
                                }
                                workPlan.WorkPlanDetails.Add(workPlanDetail);
                            }

                            _Logger.Write(_LogCategory, $"Process Detail When Change/Cancel: Line {line.Id} - WorkPlan {workPlan.Id} - WorkPlanDetail: {workPlanDetail.Id} - Total: {workPlanDetail.PlanQuantity}", LogType.Debug);

                            AddWorkPlanDetail2Time(workPlan, workPlanDetail, isAdd);

                            //Xử lý WorkPlanDetail
                            if (rawData.Status == (int)Enums.Cancel)
                            {
                                workPlan.WorkPlanDetails.FirstOrDefault(x => x.Id == workPlanDetail.Id).Status = (int)Enums.Ready2Cancel;
                                //Giảm CurrentDetail xuống
                                line.CurrentDetail--;
                                if (line.CurrentDetail < 0) { line.CurrentDetail = 0; }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process change ReLoad WorkPlan Error: {ex}, try to restart service again.", LogType.Error);
                //Stop();
            }
        }
        /// <summary>
        /// Trường hợp đang chạy mà thêm mới 1 kế hoạch vào
        /// </summary>
        /// <param name="updatedWorkPlanDetails"></param>
        private void ProcessPlanDetailWhenRunning(List<MES_WORK_PLAN_DETAIL> updatedWorkPlanDetails)
        {
            try
            {
                DateTime eventTime = DateTime.Now;
                using (Entities _dbContext = new Entities())
                {
                    foreach (MES_WORK_PLAN_DETAIL workPlanDetail in updatedWorkPlanDetails)
                    {
                        WorkPlan workPlan = _WorkPlans.FirstOrDefault(x => x.WORK_PLAN_ID == workPlanDetail.WORK_PLAN_ID);
                        Line line = _Lines.FirstOrDefault(x => x.LINE_ID == workPlan.LINE_ID);

                        DateTime _start = line.WorkPlan.PlanStart;
                        DateTime _finish = line.WorkPlan.PlanFinish;

                        DateTime _startDetail = workPlanDetail.PLAN_START;
                        DateTime _finishDetail = workPlanDetail.PLAN_FINISH;


                        if (_start < _startDetail) { _start = _startDetail; }
                        if (_finish > _finishDetail) { _finish = _finishDetail; }

                        decimal _planDuration = (decimal)(_finish - _start).TotalSeconds;
                        DateTime actualStartPlan = _start;
                        //Kiểm tra TimeDate
                        BuildTimeData(line.LINE_ID, false);

                        _Logger.Write(_LogCategory, $"Process Detail When Running: Line {line.LINE_ID} - WorkPlan {workPlan.WORK_PLAN_ID} - WorkPlanDetail: {workPlanDetail.WORK_PLAN_DETAIL_ID} - Total: {workPlanDetail.PLAN_QUANTITY}", LogType.Debug);
                        
                        AddWorkPlanDetail2Time(workPlan, workPlanDetail);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Process change ReLoad WorkPlan Error: {ex}, try to restart service again.", LogType.Error);
                //Stop();
            }
        }
        */
        #endregion

        #region RabbitMQ
        private void ConnectRabbitMQ()
        {
            try
            {
                _Logger.Write(_LogCategory, $"Connecting to RabbitMQ {_RabbitMQHost}", LogType.Info);
                _EventBus = RabbitHutch.CreateBus($"host={_RabbitMQHost};virtualHost={_RabbitMQVirtualHost};username={_RabbitMQUser};password={_RabbitMQPassword}");
                if (_EventBus != null && _EventBus.IsConnected)
                {
                    _Logger.Write(_LogCategory, $"Connected to RabbitMQ!", LogType.Info);
                }

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, ex);
            }
        }

        #endregion

        #region SupportFunction
        public decimal Time2Num(DateTime time, int type)
        {
            if (type == Consts.YearArchive) return decimal.Parse($"{time:yyyy}");
            if (type == MonthArchive) return decimal.Parse($"{time:yyyyMM}");
            if (type == DayArchive) return decimal.Parse($"{time:yyyyMMdd}");
            if (type == HourArchive) return decimal.Parse($"{time:yyyyMMddHH}");
            if (type == MinuteArchive) return decimal.Parse($"{time:yyyyMMddHHmm}");
            return 0;
        }
        public DateTime Num2Time(decimal num, int type)
        {
            if (type == MinuteArchive) return new DateTime(int.Parse($"{num:0}".Substring(0, 4)), int.Parse($"{num:0}".Substring(4, 2)), int.Parse($"{num:0}".Substring(6, 2)), int.Parse($"{num:0}".Substring(8, 2)), int.Parse($"{num:0}".Substring(10, 2)), 0);
            if (type == HourArchive) return new DateTime(int.Parse($"{num:0}".Substring(0, 4)), int.Parse($"{num:0}".Substring(4, 2)), int.Parse($"{num:0}".Substring(6, 2)), int.Parse($"{num:0}".Substring(8, 2)), 0, 0);
            if (type == DayArchive) return new DateTime(int.Parse($"{num:0}".Substring(0, 4)), int.Parse($"{num:0}".Substring(4, 2)), int.Parse($"{num:0}".Substring(6, 2)), 0, 0, 0);
            if (type == MonthArchive) return new DateTime(int.Parse($"{num:0}".Substring(0, 4)), int.Parse($"{num:0}".Substring(4, 2)), 1, 0, 0, 0);
            return new DateTime(int.Parse($"{num:0}".Substring(0, 4)), 1, 1, 0, 0, 0);
        }
        public string GetTimeFormat(decimal stopTime, int type)
        {
            //Stoptime in second
            int _second = (int)stopTime % 60;

            int _minute = (int)Math.Round(stopTime / 60, 0);

            int _hour = _minute / 60;
            _minute = _minute % 60;

            string ret = String.Format("{0:00}:{1:00}", _hour, _minute);
            if (type == SecondArchive)
            {
                ret += String.Format(":{0:00}", _second);
            }
            return ret;
        }
        public string GenID()
        {
            return Guid.NewGuid().ToString();
        }
        #endregion


        #region Customer_Ariston
        private PMSData GetPMSInfo(string CODE)
        {

            PMSData result = null;
            try
            {
                //Lấy PMS thực tế

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(_Sync_Url);

                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                HttpResponseMessage response = client.GetAsync(CODE).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Parse the response body.
                    string responseString = response.Content.ReadAsStringAsync().Result;
                    JObject jsonObj = JObject.Parse(responseString);
                    string content = jsonObj["content"].ToString();

                    _Logger.Write(_LogCategory, $"PMS call for LINe {CODE}: {content}", LogType.Debug);

                    result = JsonConvert.DeserializeObject<PMSData>(content);
                }
                else
                {
                    _Logger.Write(_LogCategory, $"PMS call for LINe {CODE} NOT SUCCESSFULL", LogType.Error);
                }
                //Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
                client.Dispose();

                //Lấy PMS giả
                /*
                using (Entities _dbContext = new Entities())
                {
                    MES_TMP_PMS_DATA content = _dbContext.MES_TMP_PMS_DATA.FirstOrDefault(x => x.ProductLineId == CODE && x.Status == "Running");
                    if (content != null)
                    {
                        result = new PMSData()
                        {
                            productlineid = int.Parse(content.ProductLineId),
                            productcode = content.ProductCode,
                            productname = content.ProductName,
                            planid = double.Parse(content.PlanId),
                            ponumber = double.Parse(content.PONumber),
                            model = content.Model,
                            planquantity = (int)content.PlanQuantity,
                            actualquantity = (int)content.ActualQuantity,
                            lastproductiontime = ((DateTime)content.LastProductionTime).ToString("yyyy-MM-dd HH:mm:ss"),
                            status = content.Status,
                        };
                    }
                }
                */
                return result;

            }
            catch (Exception ex)
            {
                _Logger.Write(_LogCategory, $"Proccess Sync Call PMS line {CODE} Error: {ex}", LogType.Error);
            }
            return null;
        }

        #endregion
    }
}
