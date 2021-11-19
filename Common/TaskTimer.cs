using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class TaskTimer
    {
        String TaskName { get; set; }
        public DateTime StartTime { get; set; }
        public double ElapsedSeconds
        {
            get { return (DateTime.Now - StartTime).TotalSeconds; }
        }

        public double ElapsedMilliseconds
        {
            get { return (DateTime.Now - StartTime).TotalMilliseconds; }
        }

        public TaskTimer(String name)
        {
            TaskName = name;
            StartTime = DateTime.Now;
        }

        public void Reset()
        {
            StartTime = DateTime.Now;
        }

        public string Format()
        {
            string result;

            double elapsed = ElapsedSeconds;
            if (elapsed < 200.0) result = String.Format("{0:0} milliseconds", ElapsedMilliseconds);
            else result = String.Format("{0:0.00} seconds", elapsed);
            return result;
        }
    }
}
