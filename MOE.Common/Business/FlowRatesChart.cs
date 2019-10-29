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

        private List<Color> Palette = new List<Color> {
            Color.Aqua,
            Color.ForestGreen,
            Color.Firebrick,
            Color.Navy,
            Color.Orange,
            Color.PaleGreen,
            Color.Tomato,
            Color.SlateGray,
            Color.PeachPuff,
            Color.Lavender,
            Color.Ivory,
            Color.HotPink,
            Color.Brown,
            Color.Crimson,
            Color.LimeGreen,
            Color.OliveDrab
        };

        public FlowRatesChart(WCFServiceLibrary.FlowRatesOptions options, FlowRatesPhase phase )
        {
            Options = options;
            FlowRatesPhase = phase;
            options.YAxisMax = (((int)Math.Max(FlowRatesPhase.Cycles.Max(p => p.CycleSaturationFlowRate), FlowRatesPhase.Cycles.Max(p => p.CyclePhaseFlowRate))/250) +1)*250 + 250;
            Chart = ChartFactory.CreateDefaultChartNoX2AxisNoY2Axis(options);
            Chart.ChartAreas.First().AxisY.Title = "Flow Rate (vehicles per hour)";
            Chart.ChartAreas.First().AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            ChartFactory.SetImageProperties(Chart);

            //Create the chart legend
            var chartLegend = new Legend();
            chartLegend.Name = "MainLegend";
            chartLegend.Docking = Docking.Left;
            Chart.Legends.Add(chartLegend);
            AddSeries(Chart, phase);
            AddDataToChart(Chart);
            Chart.ChartAreas.First().RecalculateAxesScale();
            SetChartTitles(phase, phase.Statistics);
        }

        private void SetChartTitles(FlowRatesPhase signalPhase, Dictionary<string, string> statistics)
        {
            Chart.Titles.Add(ChartTitleFactory.GetChartName(Options.MetricTypeID));
            Chart.Titles.Add(ChartTitleFactory.GetSignalLocationAndDateRange(
                Options.SignalID, Options.StartDate, Options.EndDate));
            Chart.Titles.Add(ChartTitleFactory.GetPhaseAndPhaseDescriptions(
                signalPhase.Approach, false));
            Chart.Titles.Add(ChartTitleFactory.GetStatistics(statistics));
            Chart.Titles.Add(ChartTitleFactory.GetTitle(
                "Flow Rates Per Lane by Cycle per Phase."));
            Chart.Titles.LastOrDefault().Docking = Docking.Bottom;
        }
        private void AddSeries(Chart chart, FlowRatesPhase phase)
        {
            foreach (var lane in phase.Detectors)
            {
                var phaseFlowRates = new Series();
                phaseFlowRates.ChartType = SeriesChartType.Line;
                phaseFlowRates.BorderDashStyle = ChartDashStyle.Dash;
                phaseFlowRates.BorderWidth = 2;
                phaseFlowRates.Color = Palette[lane.DetChannel % 16];
                phaseFlowRates.Name = String.Format("Phase Flow Rate - Ch{0}", lane.DetChannel);
                phaseFlowRates.XValueType = ChartValueType.DateTime;

                var satFlowRates = new Series();
                satFlowRates.ChartType = SeriesChartType.Line;
                satFlowRates.BorderDashStyle = ChartDashStyle.Solid;
                satFlowRates.BorderWidth = 2;
                satFlowRates.Color = Palette[lane.DetChannel % 16];
                satFlowRates.Name = String.Format("Saturation Flow Rate - Ch{0}", lane.DetChannel);
                satFlowRates.XValueType = ChartValueType.DateTime;

                chart.Series.Add(phaseFlowRates);
                chart.Series.Add(satFlowRates);
            }
        }
        protected void AddDataToChart(Chart chart)
        {
            foreach (var cycle in FlowRatesPhase.Cycles)
            {
                foreach (var lane in cycle.Lanes)
                {
                    if (lane.Detections.Count > 0)
                    {
                        chart.Series[String.Format("Phase Flow Rate - Ch{0}", lane.DetectorChannel)].Points.AddXY(cycle.StartTime, lane.PhaseFlowRate);
                        if (lane.SaturationFlowRate > 0)
                            chart.Series[String.Format("Saturation Flow Rate - Ch{0}", lane.DetectorChannel)].Points.AddXY(cycle.StartTime, lane.SaturationFlowRate);
                    }
                }
            }
        }
    }
}
