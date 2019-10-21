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

        public ResidualQueueCycle(DateTime firstGreenEvent, DateTime redEvent, DateTime yellowEvent,
            DateTime lastGreenEvent) : base(firstGreenEvent, redEvent, yellowEvent, lastGreenEvent)
        {
        }
       
        public int InputDetectionCount { get; set; }
        public int OutputDetectionCount { get; set; }
        public int ResidualQueue { get { return InputDetectionCount - OutputDetectionCount; } }
        public int TotalQueue { get; set; }
        public List<Controller_Event_Log> InputDetectorEvents { get; set; }
        public List<Controller_Event_Log> OutputDetectorEvents { get; set; }
        public TerminationType TerminationEvent { get; }

        public void SetDetections(List<Controller_Event_Log> eventsIn, List<Controller_Event_Log> eventsOut)
        {
            InputDetectorEvents = eventsIn.Where(e => e.Timestamp > StartTime && e.Timestamp < EndTime).ToList();
            InputDetectionCount = InputDetectorEvents.Count;
            OutputDetectorEvents = eventsOut.Where(e => e.Timestamp > StartTime && e.Timestamp < EndTime).ToList();
            OutputDetectionCount = OutputDetectorEvents.Count;
        }
    }
}
