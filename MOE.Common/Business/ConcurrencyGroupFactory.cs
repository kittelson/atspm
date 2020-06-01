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

            //Red to Red
            var termEventsAll = celRepository.GetSignalEventsByEventCode(signalID, startTime, endTime, 11);

            foreach (var approachDir in sig.Approaches.Select(a => a.DirectionTypeID).Distinct())
            {
                var approaches = sig.Approaches.Where(a => a.DirectionTypeID == approachDir);
                var opposingApproaches = sig.Approaches.Where(a => a.DirectionTypeID == OpposingApproach(approachDir));

                var primaryPhases = approaches.Select(a => a.ProtectedPhaseNumber)
                    .Union(approaches.Where(a=>a.PermissivePhaseNumber != null).Select(a => (int)a.PermissivePhaseNumber));

                var opposingPhases = opposingApproaches.Select(a => a.ProtectedPhaseNumber)
                    .Union(approaches.Where(a => a.PermissivePhaseNumber != null).Select(a => (int)a.PermissivePhaseNumber));

                var phases = primaryPhases.Union(opposingPhases);

                //Red to Red End Clearance events for appraoch
                var termEvents = celRepository.GetSignalEventsByEventCode(signalID, startTime, endTime, 11)
                    .Where(c => phases.Contains(c.EventParam));

                //Barrier Events for approach
                barrierEvents = celRepository.GetSignalEventsByEventCode(signalID, startTime, endTime, 31)
                    .Where(c => termEvents.Select(e=>e.Timestamp).Distinct().Contains(c.Timestamp))
                    .OrderBy(c => c.EventParam)
                    .ThenBy(c => c.Timestamp)
                    .ToList();

                for (var i = 0; i < barrierEvents.Count - 1; i++)
                {
                    //Same barrier #
                    if (barrierEvents[i].EventParam == barrierEvents[i + 1].EventParam)
                    {

                        var barrierTermEvents = termEvents.Where(e => e.Timestamp >= barrierEvents[i + 1].Timestamp.AddSeconds(-5)
                                                                     && e.Timestamp <= barrierEvents[i + 1].Timestamp);
                        foreach (var termEvent in barrierTermEvents)
                        {           
                            var subjectPhase = termEvent;
                            // if this is not a temrination event in the subject phase, find the closest primary phase termination event
                            if (primaryPhases.Intersect(barrierTermEvents.Select(e => e.EventParam)).Count() == 0)
                            {
                                subjectPhase = termEvents.Where(e => primaryPhases.Contains(e.EventParam) 
                                                                      && e.Timestamp <= barrierEvents[i + 1].Timestamp.AddSeconds(-5))
                                                             .OrderByDescending(e=>e.Timestamp).FirstOrDefault();
                                if (subjectPhase == null) continue;
                            }
                            else if (!primaryPhases.Contains(termEvent.EventParam))
                            {
                                continue;
                            }

                            var current = new ResidualQueueConcurrencyGroup(barrierEvents[i].Timestamp, barrierEvents[i + 1].Timestamp)
                            {
                                TerminationEvent = subjectPhase,
                                Approach = approaches.Where(a => a.PermissivePhaseNumber == subjectPhase.EventParam ||
                                                                 a.ProtectedPhaseNumber == subjectPhase.EventParam).First()
                            };
                            groups.Add(current);
                            break;
                        }
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

                var inputEvents = celRepository.GetRecordsByParameterAndEvent(signalID, startTime, endTime, inputDetectorChannels.Count > 0 ? inputDetectorChannels : new List<int>() { 0 }, new List<int> { 82 });
                var outputEvents = celRepository.GetRecordsByParameterAndEvent(signalID, startTime, endTime, outputDetectorChannels.Count > 0 ? outputDetectorChannels : new List<int>() { 0 }, new List<int> { 82 });

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
//#if DEBUG
//            using (System.IO.StreamWriter w = System.IO.File.AppendText($"C:\\temp\\Concurrency Groups {DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}.csv"))
//           {
//               w.WriteLine("Start Time,End Time,Termination Event Time,Approach,Phase,Input Count,Output Count,Total Queue");
//               foreach (ResidualQueueConcurrencyGroup group in groups)
//               {
//                   w.WriteLine($"{group.StartTime},{group.EndTime},{group.TerminationEvent.Timestamp},{group.Approach.DirectionType.Abbreviation},{group.TerminationEvent.EventParam},{group.InputDetectionCount},{group.OutputDetectionCount},{group.TotalQueue}");
//               }
//           }
//#endif 
            return groups;
        }

        private static int OpposingApproach(int a)
        {
            switch (a)
            {
                case 1:
                    return 2;
                case 2:
                    return 1;
                case 3:
                    return 4;
                case 4:
                    return 3;
                default:
                    return 0;
            }
        }
    }   
}