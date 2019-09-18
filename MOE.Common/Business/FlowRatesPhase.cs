using System.Collections.Generic;
using System.Linq;
using MOE.Common.Business.SplitFail;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business
{
    class FlowRatesPhase
    {
        private List<Controller_Event_Log> _outputDetectorActivations = new List<Controller_Event_Log>();
        public Approach Approach { get; }
        public string PhaseNumberSort { get; set; }
        public List<FlowRatesCycle> Cycles { get; }
        public List<Models.Detector> Detectors { get; set; } = new List<Models.Detector>();
        //public List<PlanSplitFail> Plans { get; }
        public Dictionary<string, string> Statistics { get; } = new Dictionary<string, string>();

        public FlowRatesPhase(Approach approach, FlowRatesOptions options, bool getPermissivePhase)
        {
            Approach = approach;
            PhaseNumberSort = getPermissivePhase ? approach.PermissivePhaseNumber.Value.ToString() + "-1" : approach.ProtectedPhaseNumber.ToString() + "-2";
            Cycles = CycleFactory.GetFlowRatesCycles(options, approach);
            SetDetectorActivations(options);
            AddDetectorActivationsToCycles();
            SetStatistics();
            // Plans = PlanFactory.GetSplitFailPlans(Cycles, options, Approach);
        }

        private void AddDetectorActivationsToCycles()
        {
            foreach (var cycle in Cycles)
                cycle.SetDetections(_outputDetectorActivations);
        }

        private void SetDetectorActivations(FlowRatesOptions options)
        {
            var controllerEventsRepository = ControllerEventLogRepositoryFactory.Create();
            Detectors = Approach.GetAllDetectorsOfDetectionType(9); //Output type

            foreach (var detector in Detectors)
            {
                List<Controller_Event_Log> events = controllerEventsRepository.GetEventsByEventCodesParamWithLatencyCorrection(Approach.SignalID,
                    options.StartDate, options.EndDate, new List<int> { 81, 82 }, detector.DetChannel, detector.LatencyCorrection);
                _outputDetectorActivations.AddRange(events);
            }
        }

        private void SetStatistics()
        {
            Statistics.Add("Total Cycles", Cycles.Count().ToString());
            Statistics.Add("Total Saturated Cycles", Cycles.Where(c => c.Lanes.Where(l => l.Saturation).Count() > 0).Count().ToString());
            Statistics.Add("Average Phase Flow Rate", System.Math.Round(Cycles.Average(c => c.CyclePhaseFlowRate)).ToString());
            Statistics.Add("Average Saturation Flow Rate", System.Math.Round(Cycles.Where(c=>c.CycleSaturationFlowRate > 0).Average(c => c.CycleSaturationFlowRate)).ToString());
        }
    }
}