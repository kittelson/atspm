using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.UI.DataVisualization.Charting;
using MOE.Common.Business.CustomReport;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business.WCFServiceLibrary
{
    [DataContract]
    public class ResidualQueueOptions : MetricOptions
    {
        public ResidualQueueOptions(string signalId, DateTime startDate, DateTime endDate, double? yAxisMax)
        {
            SignalID = signalId;
            StartDate = startDate;
            EndDate = endDate;
            YAxisMax = yAxisMax;
        }

        public ResidualQueueOptions()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            YAxisMax = 3;
            Y2AxisMax = 10;
        }

        public override List<string> CreateMetric()
        {
            base.CreateMetric();
            //EndDate = EndDate.AddSeconds(59);
            var returnString = new List<string>();
            var sr = SignalsRepositoryFactory.Create();
            var signal = sr.GetVersionOfSignalByDate(SignalID, StartDate);
            var metricApproaches = signal.GetApproachesForSignalThatSupportMetric(MetricTypeID);
            if (metricApproaches.Count > 0)
            {
                List<ResidualQueuePhase> Phases = new List<ResidualQueuePhase>();
                foreach (Approach approach in metricApproaches)
                {
                    Phases.Add(new ResidualQueuePhase(approach, this, false));
                }

                Phases = Phases.OrderBy(s => s.PhaseNumberSort).ToList();
                foreach (var phase in Phases)
                {
                    GetChart(phase, returnString);
                }
            }
            return returnString;
        }

        private void GetChart(ResidualQueuePhase ResidualQueuePhase, List<string> returnString)
        {
            var ResidualQueueChart = new ResidualQueueChart(this, ResidualQueuePhase);

            string chartName = CreateFileName();
            chartName = chartName.Replace(".", ResidualQueuePhase.Approach.DirectionType.Description + ".");
            try
            {
                ResidualQueueChart.Chart.SaveImage(MetricFileLocation + chartName, ChartImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                try
                {
                    ResidualQueueChart.Chart.SaveImage(MetricFileLocation + chartName, ChartImageFormat.Jpeg);
                }
                catch
                {
                    var appEventRepository =
                        ApplicationEventRepositoryFactory.Create();
                    var applicationEvent = new ApplicationEvent();
                    applicationEvent.ApplicationName = "SPM Website";
                    applicationEvent.Description = MetricType.ChartName + ex.Message + " Failed While Saving File";
                    applicationEvent.SeverityLevel = ApplicationEvent.SeverityLevels.Medium;
                    applicationEvent.Timestamp = DateTime.Now;
                    appEventRepository.Add(applicationEvent);
                }
            }
            returnString.Add(MetricWebPath + chartName);
        }
    }
}