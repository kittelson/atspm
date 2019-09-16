using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Web.UI.DataVisualization.Charting;
using MOE.Common.Business.CustomReport;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business.WCFServiceLibrary
{
    [DataContract]
    public class FlowRatesOptions : MetricOptions
    {
        public FlowRatesOptions(string signalId, DateTime startDate, DateTime endDate, double? yAxisMax)
        {
            SignalID = signalId;
            StartDate = startDate;
            EndDate = endDate;
            YAxisMax = yAxisMax;
        }

        public FlowRatesOptions()
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
                List<FlowRatesPhase> Phases = new List<FlowRatesPhase>();
                foreach (Approach approach in metricApproaches)
                {
                    Phases.Add(new FlowRatesPhase(approach, this, false));
                }
                 
                Phases = Phases.OrderBy(s => s.PhaseNumberSort).ToList();
                foreach (var phase in Phases)
                {
                    GetChart(phase, returnString);
                }
            }
            return returnString;
        }

        private void GetChart(FlowRatesPhase flowRatesPhase, List<string> returnString)
        {
            var flowRatesChart = new FlowRatesChart(this, flowRatesPhase);
            
            string chartName = CreateFileName();
            chartName = chartName.Replace(".", flowRatesPhase.Approach.DirectionType.Description + ".");
            try
            {
                flowRatesChart.Chart.SaveImage(MetricFileLocation + chartName, ChartImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                try
                {
                    flowRatesChart.Chart.SaveImage(MetricFileLocation + chartName, ChartImageFormat.Jpeg);
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