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
    class FlowRatesChart
    {
        public Chart Chart;
        public WCFServiceLibrary.FlowRatesOptions Options;
        public FlowRatesPhase FlowRatesPhase;

        public FlowRatesChart(WCFServiceLibrary.FlowRatesOptions options, FlowRatesPhase phase )
        {
            Options = options;
            FlowRatesPhase = phase;
            options.YAxisMax = Math.Round(FlowRatesPhase.Cycles.Max(p => p.SaturationFlowRate));
            Chart = ChartFactory.CreateDefaultChartNoX2AxisNoY2Axis(options);
            Chart.ChartAreas.First().AxisY.Title = "Flow Rate (vehicles per hour)";
            Chart.ChartAreas.First().AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
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
            var phaseFlowRates = new Series();
            phaseFlowRates.ChartType = SeriesChartType.Line;
            phaseFlowRates.BorderDashStyle = ChartDashStyle.Solid;
            phaseFlowRates.BorderWidth = 2;
            phaseFlowRates.Color = Color.DarkGreen;
            phaseFlowRates.Name = "Phase Flow Rate";
            phaseFlowRates.XValueType = ChartValueType.DateTime;

            var satFlowRates = new Series();
            satFlowRates.ChartType = SeriesChartType.Line;
            satFlowRates.BorderDashStyle = ChartDashStyle.Solid;
            satFlowRates.BorderWidth = 2;
            satFlowRates.Color = Color.DarkRed;
            satFlowRates.Name = "Saturation Flow Rate";
            satFlowRates.XValueType = ChartValueType.DateTime;

            chart.Series.Add(phaseFlowRates);
            chart.Series.Add(satFlowRates);

        }
        protected void AddDataToChart(Chart chart)
        {
            foreach (var cycle in FlowRatesPhase.Cycles)
            {
                chart.Series["Phase Flow Rate"].Points.AddXY(cycle.StartTime, cycle.PhaseFlowRate);
                if (cycle.SaturationFlowRate > 0)
                    chart.Series["Saturation Flow Rate"].Points.AddXY(cycle.StartTime, cycle.SaturationFlowRate);   
            }
        }
    }
}
