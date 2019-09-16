using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.DataVisualization.Charting;
using Microsoft.EntityFrameworkCore.Internal;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;
namespace MOE.Common.Business
{
    class ResidualQueueChart
    {
        public Chart Chart;
        public WCFServiceLibrary.ResidualQueueOptions Options;
        public ResidualQueuePhase ResidualQueuePhase;

        public ResidualQueueChart(WCFServiceLibrary.ResidualQueueOptions options, ResidualQueuePhase phase)
        {
            Options = options;
            ResidualQueuePhase = phase;
            options.YAxisMax = ResidualQueuePhase.Cycles.Max(p => p.ResidualQueue);
            options.Y2AxisMax = options.YAxisMax;
            options.Y2AxisTitle = "Residual Queue";
            // options.YAxisMin = ResidualQueuePhase.Cycles.Min(p => p.ResidualQueue);
            Chart = ChartFactory.CreateDefaultChartNoX2AxisNoY2Axis(options);
            Chart.ChartAreas.First().AxisY.Title = "Residual Queue";
            ChartFactory.SetImageProperties(Chart);

            //Create the chart legend
            var chartLegend = new Legend();
            chartLegend.Name = "MainLegend";
            chartLegend.Docking = Docking.Left;
            Chart.Legends.Add(chartLegend);
            AddSeries(Chart);
            AddDataToChart(Chart);
            Chart.ChartAreas.First().RecalculateAxesScale();
        }

        private void SetChartTitles(SignalPhase signalPhase, Dictionary<string, string> statistics)
        {
            Chart.Titles.Add(ChartTitleFactory.GetChartName(Options.MetricTypeID));
            Chart.Titles.Add(ChartTitleFactory.GetSignalLocationAndDateRange(
                Options.SignalID, Options.StartDate, Options.EndDate));
            Chart.Titles.Add(ChartTitleFactory.GetPhaseAndPhaseDescriptions(
                signalPhase.Approach, signalPhase.GetPermissivePhase));
            Chart.Titles.Add(ChartTitleFactory.GetStatistics(statistics));
            Chart.Titles.Add(ChartTitleFactory.GetTitle(
                "Simplified Approach Delay. Displays time between approach activation during the red phase and when the phase turns green."
                + " \n Does NOT account for start up delay, deceleration, or queue length that exceeds the detection zone."));
            Chart.Titles.LastOrDefault().Docking = Docking.Bottom;
        }
        private void AddSeries(Chart chart)
        {
            var phaseResidualQueue = new Series();
            phaseResidualQueue.ChartType = SeriesChartType.StepLine;
            phaseResidualQueue.BorderDashStyle = ChartDashStyle.Solid;
            phaseResidualQueue.BorderWidth = 2;
            phaseResidualQueue.Color = Color.DarkGreen;
            phaseResidualQueue.Name = "Residual Queue";
            phaseResidualQueue.XValueType = ChartValueType.DateTime;

            chart.Series.Add(phaseResidualQueue);


        }
        protected void AddDataToChart(Chart chart)
        {
            foreach (var cycle in ResidualQueuePhase.Cycles)
            {
                chart.Series["Residual Queue"].Points.AddXY(cycle.StartTime, cycle.ResidualQueue);
            }
        }
    }
}
