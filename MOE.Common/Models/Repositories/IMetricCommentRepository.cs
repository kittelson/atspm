﻿using System.Collections.Generic;

namespace MOE.Common.Models.Repositories
{
    public interface IMetricCommentRepository
    {
        List<MetricComment> GetAllMetricComments();
        MetricComment GetMetricCommentByMetricCommentID(int metricCommentID);
        void AddOrUpdate(MetricComment metricComment);
        void Add(MetricComment metricComment);
        void Remove(MetricComment metricComment);
        MetricComment GetLatestCommentForReport(string signalID, int metricID);
        List<MetricType> GetMetricTypesByMetricComment(MetricComment metricComment);
        Dictionary<string, string> GetLatestCommentBySignalForType(int metricID);
    }
}