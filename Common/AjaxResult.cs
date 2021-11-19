using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class AjaxResult
    {
        public String Source { get; set; }
        public bool Success { get; set; }
        public String Message { get; set; }
        public string TaskTime { get; set; }
        public Dictionary<String, Object> Data { get; set; }

        private TaskTimer m_timer;

        public Object this[String name]
        {
            get
            {
                Object result = null;
                Data.TryGetValue(name, out result);
                return result;
            }
            set
            {
                Data[name] = value;
            }
        }

        public AjaxResult()
        {
            Success = false;
            Message = "Not initialized";
            Source = "";
            Data = new Dictionary<string, object>();
            m_timer = new TaskTimer(Source);
        }


        public AjaxResult(String source, String default_message = "Not initialized")
        {
            Success = false;
            Message = default_message;
            Source = source;
            Data = new Dictionary<string, object>();
            m_timer = new TaskTimer(Source);
        }

        public AjaxResult Set(String name, Object value)
        {
            Data[name] = value;
            return this;
        }

        public AjaxResult Fail(String message)
        {
            Message = message;
            Success = false;
            TaskTime = String.Format("{0:0.00} ms", m_timer.ElapsedMilliseconds);
            return this;
        }

        public AjaxResult Fail(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            Fail("Exception: " + ex.Message);
            TaskTime = String.Format("{0:0.00} ms", m_timer.ElapsedMilliseconds);
            return this;
        }

        public void Succeed(String message = "SUCCESS")
        {
            Message = message;
            Success = true;
            TaskTime = String.Format("{0:0.00} ms", m_timer.ElapsedMilliseconds);
        }

        public void Succeed(String message, String data_name, Object data_value)
        {
            Message = message;
            Set(data_name, data_value);
            Success = true;
            TaskTime = String.Format("{0:0.00} ms", m_timer.ElapsedMilliseconds);
        }
    }
}
