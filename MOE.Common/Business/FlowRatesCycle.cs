using MOE.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MOE.Common.Business.PhaseCycleBase;

namespace MOE.Common.Business
{
    public class FlowRatesCycle : RedToRedCycle
    {

        public readonly int FirstSecondsOfRed;

        public FlowRatesCycle(DateTime firstRedEvent, DateTime greenEvent, DateTime yellowEvent,
            DateTime lastRedEvent) : base(firstRedEvent, greenEvent, yellowEvent, lastRedEvent)
        {
        }
       
        public int OutputDetectionCount { get; set; }
        public List<Controller_Event_Log> OutputDetectorEvents { get; set; }
        public TerminationType TerminationEvent { get; }
        public double PhaseFlowRate { get { return OutputDetectionCount / Math.Abs(TotalGreenTime) * 3600; } }
        public double SaturationFlowRate { get { return GetSaturationFlowRate(); } }

        private double GetSaturationFlowRate()
        {
            if (OutputDetectionCount < 10)
                return 0;
            var outputOnEvents = OutputDetectorEvents.Where(e => e.EventCode == 82).OrderBy(e => e.Timestamp).ToList();
            var start5th = outputOnEvents[4].Timestamp;
            var start10th = outputOnEvents[9].Timestamp;
            var flowRate = (5 / (start10th - start5th).TotalSeconds) * 3600;
            return flowRate;
        }

        public void SetDetections(List<Controller_Event_Log> eventsOut)
        {
            OutputDetectorEvents = eventsOut.Where(e => e.Timestamp > StartTime && e.Timestamp < EndTime).ToList();
            OutputDetectionCount = OutputDetectorEvents.Where(e => e.EventCode == 82).ToList().Count;
        }
    }
}
