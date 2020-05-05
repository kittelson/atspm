using System.Collections.Generic;
using System.Linq;
using MOE.Common.Business.SplitFail;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business
{
    class ResidualQueueDirection
    {

        public string Direction { get; set; }
        public List<ResidualQueueConcurrencyGroup> Groups { get; }

        public Dictionary<string, string> Statistics { get; }

        public ResidualQueueDirection(string direction, ResidualQueueOptions options, List<ResidualQueueConcurrencyGroup> groups)
        {
            Direction = direction;
            Groups = groups;
           // SetDetectorActivations(options);
            //AddDetectorActivationsToCycles();
            //SetCycleResidualQueues();
           // Plans = PlanFactory.GetSplitFailPlans(Cycles, options, Approach);
        }
        //private void SetDetectorActivations(ResidualQueueOptions options)
        //{
        //    var controllerEventsRepository = ControllerEventLogRepositoryFactory.Create();
        //    var inputDetectors = Approach.GetAllDetectorsOfDetectionType(8); //Input type
        //    var outputDetectors = Approach.GetAllDetectorsOfDetectionType(9); //Output type
        //
        //    // get detector off events
        //    foreach (var detector in inputDetectors)
        //    {
        //        List<Controller_Event_Log> events = controllerEventsRepository.GetEventsByEventCodesParamWithLatencyCorrection(Approach.SignalID,
        //            options.StartDate, options.EndDate, new List<int> { 81 }, detector.DetChannel, detector.LatencyCorrection);
        //        _inputDetectorActivations.AddRange(events);
        //    }
        //    foreach (var detector in outputDetectors)
        //    {
        //        List<Controller_Event_Log> events = controllerEventsRepository.GetEventsByEventCodesParamWithLatencyCorrection(Approach.SignalID,
        //            options.StartDate, options.EndDate, new List<int> { 81 }, detector.DetChannel, detector.LatencyCorrection);
        //        _outputDetectorActivations.AddRange(events);
        //    }
        //}
        //
        //private void AddDetectorActivationsToCycles()
        //{
        //    foreach (var cycle in Cycles)
        //        cycle.SetDetections(_inputDetectorActivations, _outputDetectorActivations);                           
        //}

        
    }
}