using MOE.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MOE.Common.Business.PhaseCycleBase;

namespace MOE.Common.Business
{
    public class ResidualQueueCycle : RedToRedCycle
    {

       // public readonly int FirstSecondsOfRed;

        public ResidualQueueCycle(DateTime firstGreenEvent, DateTime redEvent, DateTime yellowEvent,
            DateTime lastGreenEvent) : base(firstGreenEvent, redEvent, yellowEvent, lastGreenEvent)
        {
        }
       
        public int InputDetectionCount { get; set; }
        public int OutputDetectionCount { get; set; }
        public int ResidualQueue { get { return InputDetectionCount - OutputDetectionCount; } }
        public List<Controller_Event_Log> InputDetectorEvents { get; set; }
        public List<Controller_Event_Log> OutputDetectorEvents { get; set; }
        public TerminationType TerminationEvent { get; }
       // public double RedOccupancyTimeInMilliseconds { get; private set; }
       // public double GreenOccupancyTimeInMilliseconds { get; private set; }
       // public double GreenOccupancyPercent { get; private set; }
       // public double RedOccupancyPercent { get; private set; }
       // public bool IsSplitFail { get; private set; }
       // public List<SplitFailDetectorActivation> ActivationsDuringGreen { get; set; }
       // public List<SplitFailDetectorActivation> ActivationsDuringRed { get; set; }
       public void SetDetections(List<Controller_Event_Log> eventsIn, List<Controller_Event_Log> eventsOut)
        {
            InputDetectorEvents = eventsIn.Where(e => e.Timestamp > StartTime && e.Timestamp < EndTime).ToList();
            InputDetectionCount = InputDetectorEvents.Count;
            OutputDetectorEvents = eventsOut.Where(e => e.Timestamp > StartTime && e.Timestamp < EndTime).ToList();
            OutputDetectionCount = OutputDetectorEvents.Count;
        }
    }
}
