using MOE.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace MOE.Common.Business
{
    public class FlowRatesLane
    {
        public bool Saturation { get; } = false;
       
        public double PhaseFlowRate
        {
            get
            {
               return 3600 / (TotalGreenTime / Detections.Count());
            }
        }
        public double SaturationFlowRate {
            get
            {
                if (Detections.Count < 10)
                    return 0;
                var start5th = Detections[4].Timestamp;
                var start10th = Detections[9].Timestamp;
                var flowRate = 3600 / ((start10th - start5th).TotalSeconds / 5);
                return flowRate;
            }
        }
        public int DetectorChannel { get; }
        public List<Controller_Event_Log> Detections = new List<Controller_Event_Log>();
        public double TotalGreenTime { get; }

        public FlowRatesLane(List<Controller_Event_Log> events, double totalGreenTime)
        {
            events = events.Where(e => e.EventCode == 82).OrderBy(e => e.Timestamp).ToList();
            TotalGreenTime = totalGreenTime;
            if (events.Any())
            {
                DetectorChannel = events.First().EventParam;
                Detections = events;
                Saturation = events.Count >= 10;
            }
        }
    }
}
