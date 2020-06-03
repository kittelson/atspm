using MOE.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<FlowRatesLane> Lanes { get; } = new List<FlowRatesLane>();
        public List<Controller_Event_Log> OutputDetectorEvents { get; set; }
        public TerminationType TerminationEvent { get; }
        public double CyclePhaseFlowRate { get { return GetPhaseFlowRate(); } }
        public double CycleSaturationFlowRate { get { return GetSaturationFlowRate(); } }

        private double GetSaturationFlowRate()
        {
            return Lanes.Where(l => l.Saturation).Count() == 0 ? 0 : Lanes.Sum(l=>l.SaturationFlowRate) / Lanes.Where(l=>l.Saturation).Count();
        }

        private double GetPhaseFlowRate()
        {
            return Lanes.Count() == 0 ? 0 : Lanes.Sum(l => l.PhaseFlowRate) / Lanes.Count();
        }

        public void SetDetections(List<Controller_Event_Log> eventsOut)
        {
            OutputDetectorEvents = eventsOut.Where(e => e.Timestamp > GreenEvent && e.Timestamp < EndTime).ToList();
            OutputDetectionCount = OutputDetectorEvents.Where(e => e.EventCode == 82).ToList().Count;
            var detectors = OutputDetectorEvents.GroupBy(e => e.EventParam);
            foreach (var d in detectors)
            {
                Lanes.Add(new FlowRatesLane(d.ToList(), TotalGreenTime));
            }
        }
    }
}
