using System;
using EmergeTk;
using System.Threading;
using System.Collections.Generic;

namespace EmergeTk
{
	public class StopWatch
	{
		private static readonly EmergeTkLog defaultLog = EmergeTkLogManager.GetLogger(typeof(StopWatch));

        [ThreadStaticAttribute]
        private static Dictionary<String, TimeSpan> groupTimes;

        public static Dictionary<String, TimeSpan> GroupTimes
        {
            get
            {
                if (StopWatch.groupTimes == null)
                    StopWatch.groupTimes = new Dictionary<String, TimeSpan>();

                return StopWatch.groupTimes;
            }
        }

        public static void Summary(EmergeTkLog emergeTklog)
        {
            if (StopWatch.groupTimes != null)
            {
                foreach (KeyValuePair<String, TimeSpan> kvp in StopWatch.groupTimes)
                {
                    emergeTklog.InfoFormat("StopWatch Group Summary Time: Group = {0}, Time = {1} ms.", kvp.Key, kvp.Value.TotalMilliseconds);
                }
            }
        }

        private EmergeTkLog log;	
		public StopWatch(string name)
		{
			this.Name = name;
            this.log = StopWatch.defaultLog;
		}

        public StopWatch(string name, EmergeTkLog log)
        {
            this.Name = name;
            this.log = log;
        }

        public StopWatch(string name, string group)
        {
            this.Group = group;
            this.Name = name;
            this.log = StopWatch.defaultLog;
        }

        public StopWatch(string name, string group, EmergeTkLog log)
        {
            this.Group = group;
            this.Name = name;
            this.log = log;
        }

        public EmergeTkLog Log
        {
            set
            {
                log = value;
            }
        }
		
		public string Name { get; set; }
        public string Group { get; set; }
        public DateTime StartTime
        {
            get
            {
                return this.start;
            }
        }
		DateTime lap;
        DateTime start;
		int laps = 0;
		
		public void Start()
		{
			lap = start = DateTime.Now;			
            if (String.IsNullOrEmpty(this.Group))
			    log.InfoFormat("[{0}] Starting stopwatch at {1}", Name, start );
		}
		
		public void Lap(string message)
		{
			DateTime now = DateTime.Now;
            TimeSpan ts = now - lap;
            if (String.IsNullOrEmpty(this.Group))
			    log.InfoFormat("[{0}] {1} Lap #{2}. Elapsed time: {3}ms", Name, message, laps++, ts.TotalMilliseconds);

			lap = now;
		}
		
		public void Stop()
		{
			DateTime stopTime = DateTime.Now;
            TimeSpan total = stopTime - start;
			laps = 0;

            // if we're not using the group functionality, then it's the normal case; spew to log.
            if (String.IsNullOrEmpty(this.Group))
            {
                log.InfoFormat("[{0}] Stopping timer at {1}.  Last lap time: {2}ms.  Elapsed time: {3}ms ",
                    Name, stopTime, (stopTime - lap).TotalMilliseconds, total.TotalMilliseconds);
            }
            else  // we are using group functionality, don't spew, just add to rollup totals for summary.
            {
                if (StopWatch.GroupTimes.ContainsKey(this.Group))
                {
                    StopWatch.GroupTimes[this.Group] += total;
                }
                else
                {
                    StopWatch.GroupTimes[this.Group] = total;
                }
            }
		}

	}
}
