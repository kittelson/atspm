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
        public List<ResidualQueueConcurrencyGroup> Groups;

        public ResidualQueueChart(WCFServiceLibrary.ResidualQueueOptions options, List<ResidualQueueConcurrencyGroup> groups)
        {
            Options = options;
            Groups = groups;
            Chart = ChartFactory.CreateDefaultChart(Options);
            ChartFactory.SetImageProperties(Chart);
            SetAxis(Chart);
            AddSeries(Chart);
            AddDataToChart(Chart);
            Chart.Titles.Add(groups.First().Direction);
        }

        private void SetAxis(Chart chart)
        {
            chart.ChartAreas.First().AxisY.Title = "Residual Queue";
            chart.ChartAreas.First().AxisY.Maximum = ((Groups.Max(p => p.TotalQueue) / 5) + 1) * 5;
            chart.ChartAreas.First().AxisY.Minimum = ((Groups.Min(p => p.TotalQueue) / 5) - 1) * 5;
            chart.ChartAreas.First().AxisX2.Enabled = AxisEnabled.False;
            chart.ChartAreas.First().AxisY2.Enabled = AxisEnabled.False;
        }

        private void AddSeries(Chart chart)
        {
            var residualQueue = new Series();
            residualQueue.ChartType = SeriesChartType.StepLine;
            residualQueue.BorderDashStyle = ChartDashStyle.Solid;
            residualQueue.BorderWidth = 2;
            residualQueue.Color = Color.DarkGreen;
            residualQueue.Name = "Residual Queue";
            residualQueue.XValueType = ChartValueType.DateTime;

            chart.Series.Add(residualQueue);
        }

        protected void AddDataToChart(Chart chart)
        {
            foreach (var group in Groups)
            {
                chart.Series["Residual Queue"].Points.AddXY(group.EndTime, group.TotalQueue);
            }
        }
    }
}
