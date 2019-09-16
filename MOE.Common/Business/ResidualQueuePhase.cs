using System.Collections.Generic;
using System.Linq;
using MOE.Common.Business.SplitFail;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business
{
    class ResidualQueuePhase
    {
        private List<Controller_Event_Log> _inputDetectorActivations = new List<Controller_Event_Log>();
        private List<Controller_Event_Log> _outputDetectorActivations = new List<Controller_Event_Log>();
        public Approach Approach { get; }
        public string PhaseNumberSort { get; set; }
        public List<ResidualQueueCycle> Cycles { get; }
        //public List<PlanSplitFail> Plans { get; }
        public Dictionary<string, string> Statistics { get; }

        public ResidualQueuePhase(Approach approach, ResidualQueueOptions options, bool getPermissivePhase)
        {
            Approach = approach;
            PhaseNumberSort = getPermissivePhase ? approach.PermissivePhaseNumber.Value.ToString() + "-1" : approach.ProtectedPhaseNumber.ToString() + "-2";
            Cycles = CycleFactory.GetResidualQueueCycles(options, approach);
            SetDetectorActivations(options);
            AddDetectorActivationsToCycles();
           // Plans = PlanFactory.GetSplitFailPlans(Cycles, options, Approach);
        }

        private void AddDetectorActivationsToCycles()
        {
            foreach (var cycle in Cycles)
                cycle.SetDetections(_inputDetectorActivations, _outputDetectorActivations);
        }

        private void SetDetectorActivations(ResidualQueueOptions options)
        {
            var controllerEventsRepository = ControllerEventLogRepositoryFactory.Create();
            var inputDetectors = Approach.GetAllDetectorsOfDetectionType(8); //Input type
            var outputDetectors = Approach.GetAllDetectorsOfDetectionType(9); //Output type

            // get detector off events
            foreach (var detector in inputDetectors)
            {
                List<Controller_Event_Log> events = controllerEventsRepository.GetEventsByEventCodesParamWithLatencyCorrection(Approach.SignalID,
                    options.StartDate, options.EndDate, new List<int> { 81 }, detector.DetChannel, detector.LatencyCorrection);
                _inputDetectorActivations.AddRange(events);
            }
            foreach (var detector in outputDetectors)
            {
                List<Controller_Event_Log> events = controllerEventsRepository.GetEventsByEventCodesParamWithLatencyCorrection(Approach.SignalID,
                    options.StartDate, options.EndDate, new List<int> { 81 }, detector.DetChannel, detector.LatencyCorrection);
                _outputDetectorActivations.AddRange(events);
            }
        }
    }
}