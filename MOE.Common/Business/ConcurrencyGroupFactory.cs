using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business
{
    public static class ConcurrencyGroupFactory
    {
        public static List<ResidualQueueConcurrencyGroup> GetResidualQueueConcurrencyGroups(string signalID, DateTime startTime, DateTime endTime)
        {
            List<ResidualQueueConcurrencyGroup> groups = new List<ResidualQueueConcurrencyGroup>();
            var celRepository = ControllerEventLogRepositoryFactory.Create();
            var approachRepository = ApproachRepositoryFactory.Create();
            var signalRepository = SignalsRepositoryFactory.Create();
            var sig = signalRepository.GetVersionOfSignalByDate(signalID, startTime);
            List<Controller_Event_Log> barrierEvents;

            
            barrierEvents = celRepository.GetSignalEventsByEventCode(signalID, startTime, endTime, 31)
                .OrderBy(c=>c.EventParam)
                .ThenBy(c=>c.Timestamp)
                .ToList();

            //Red to Red
            var termEvents = celRepository.GetSignalEventsByEventCode(signalID, startTime, endTime, 11);

            for (var i = 0; i < barrierEvents.Count -1; i++)
            {
                if (barrierEvents[i].EventParam == barrierEvents[i + 1].EventParam)
                {
                    
                    foreach (var termEvent in termEvents.Where(e => e.Timestamp == barrierEvents[i + 1].Timestamp))
                    {
                        var concurrencyApproach = approachRepository.GetApproachBySignalPhase(termEvent.EventParam, signalID);
                        var current = new ResidualQueueConcurrencyGroup(barrierEvents[i].Timestamp, barrierEvents[i + 1].Timestamp)
                        {
                            TerminationEvent = termEvent,
                            Approach = concurrencyApproach
                        };
                        groups.Add(current);
                    }
                }

            }

            var distinctDirections = groups.GroupBy(a => a.Approach.DirectionTypeID,
                                                    a => a.Approach.DirectionTypeID,
                                                    (k, v) => new { Direction = k});

            foreach (var direction in distinctDirections)
            {

                // get detector events in direction     
                var detectors = sig.GetDetectorsForSignalThatSupportAMetricByApproachDirection(31, direction.Direction);

                var inputDetectorChannels = detectors.Where(d => d.DetectionTypes.Select(dt=>dt.DetectionTypeID).Contains(8)).Select(d => d.DetChannel).ToList();
                var outputDetectorChannels = detectors.Where(d => d.DetectionTypes.Select(dt => dt.DetectionTypeID).Contains(9)).Select(d => d.DetChannel).ToList();

                var inputEvents = celRepository.GetRecordsByParameterAndEvent(signalID, startTime, endTime, inputDetectorChannels, new List<int> { 82 });
                var outputEvents = celRepository.GetRecordsByParameterAndEvent(signalID, startTime, endTime, outputDetectorChannels, new List<int> { 82 });

                var directionGroups = groups.Where(g => g.Approach.DirectionTypeID == direction.Direction).ToList();

                for(int i = 0; i < directionGroups.Count(); i++)
                {
                    int indx = groups.IndexOf(directionGroups[i]);
                    groups[indx].SetDetections(inputEvents, outputEvents);
                    if (i == 0)
                        groups[indx].TotalQueue = groups[indx].ResidualQueue;
                    else
                    {
                        var pindx = groups.IndexOf(directionGroups[i - 1]);
                        groups[indx].TotalQueue = groups[indx].ResidualQueue + groups[pindx].TotalQueue;
                    }
                }

            }

            return groups;
        }
      
    }
}