using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace SamaPayamakService
{
    public partial class Message : ServiceBase
    {
        SqlConnection conn;
        LinePayamak.SendSoapClient Sms = new LinePayamak.SendSoapClient();
        private Timer _timer;
        private string msg="";
        private DateTime _lastRun = DateTime.Now.AddDays(-1);
        private void AddEvent(string msg, EventLogEntryType type = EventLogEntryType.Information)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "SamaPayamakServiceI";
                eventLog.WriteEntry(msg, type, 0, 1);
            }
        }
        public Message()
        {
            InitializeComponent();
        }

        private void SendMessage()
        {
           
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT ID,Tel,Msg FROM T_SMSMsg where State=0";
                cmd.CommandType = CommandType.Text;
                var dr = cmd.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load(dr);

                foreach (DataRow ro in dt.Rows)
                {

                    LinePayamak.ArrayOfString PhoneNumber = new LinePayamak.ArrayOfString();
                    LinePayamak.ArrayOfLong recId = new LinePayamak.ArrayOfLong();
                    LinePayamak.ArrayOfbyte status = new LinePayamak.ArrayOfbyte();

                    recId.Add("1"); status.Add(0);

                    PhoneNumber.Add(ro[1].ToString());

                    var result = Sms.SendSms("didgah", "1515276", PhoneNumber, "10000263", ro[2].ToString(), false, "882", ref recId, ref status);
                    switch (result)
                    {

                        case 0:
                            msg = "نام کاربری یا رمز عبور صحیح نمی باشد";
                            break;
                        case 1:
                            msg = "ارسال با موفقیت انجام شد";
                            SqlCommand cmdupdate = new SqlCommand();
                            cmdupdate.Connection = conn;
                            cmdupdate.CommandText = "Update T_SMSMsg Set State=1,SendDate=GETDATE() where ID=@ID";
                            cmdupdate.CommandType = CommandType.Text;
                            cmdupdate.Parameters.Add(new SqlParameter("@ID", ro[0].ToString()));
                            cmdupdate.ExecuteNonQuery();
                            break;
                        case 2:
                            msg = "اعتبار کافی نیست";
                            break;
                        case 3:
                            msg = "محدودیت در ارسال روزانه";
                            break;
                        case 4:
                            msg = "محدودیت در حجم ارسال";
                            break;
                        case 5:
                            msg = "شماره فرستنده معتبر نیست و یا غیرفعال می باشد";
                            break;
                        case 6:
                            msg = "شماره موبایل صحیح جهت ارسال وجود ندارد";
                            break;
                        case 7:
                            msg = "متن پیامک خالی می باشد";
                            break;
                        case 8:
                            msg = "کاربر ارسال کننده و یا سازنده ی آن غیرفعال می باشد";
                            break;
                        case 9:
                            msg = "تعداد شماره موبایل ها بیشتر از حد مجاز می باشد";
                            break;
                        case 100:
                            msg = "شما مجاز به استفاده از وب سرویس نمی باشید";
                            break;

                    }
                    AddEvent(msg);
                }

                conn.Close();
            }
            catch(Exception ex)
            {
                AddEvent(ex.Message.ToString());

                System.IO.File.AppendAllText(
                    $"{System.IO.Directory.GetCurrentDirectory()}\\log.txt", 
                    $"{DateTime.Now} {ex.Message}\r\n");
            }
        }
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            {
                _timer.Stop();
                string connectionString="";
                AddEvent("Timer is starting ...");
                try
                {
                    
                    connectionString = ConfigurationManager.ConnectionStrings["SamaPayamakService"].ToString();
                    conn = new SqlConnection(connectionString);
                    SendMessage();

                    _lastRun = DateTime.Now;
                    _timer.Start();
                    AddEvent("Timer is finishing ...");
                }

                catch (Exception ex)
                {
                    AddEvent(ex.Message.ToString(), EventLogEntryType.Error);
                }

                
            }
        }
        protected override void OnStart(string[] args)
        {
            AddEvent("Service is starting ...");
            _timer = new Timer(.1 * 60 * 1000); // every 10 minutes
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            _timer.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
