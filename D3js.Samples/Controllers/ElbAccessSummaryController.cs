using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace D3js.Samples.Controllers
{
    public class ElbAccessSummaryController : ApiController
    {
        public async Task<SummaryResult> GetDataBlob()
        {
            var cloudwatch = new AmazonCloudWatchClient(RegionEndpoint.APNortheast1);
            var res = await cloudwatch.GetMetricStatisticsAsync(new GetMetricStatisticsRequest()
            {
                Namespace = @"AWS/ELB",
                MetricName = "RequestCount",
                Statistics = new List<string>() { "Sum" },
                Unit = StandardUnit.Count,
                Dimensions = new List<Dimension>()
                {
                    new Dimension() { Name = "LoadBalancerName", Value = ConfigurationManager.AppSettings["ELBName"] }, 
                    new Dimension() { Name = "AvailabilityZone", Value = ConfigurationManager.AppSettings["AvailabilityZone"] }
                },
                StartTime = DateTime.UtcNow - TimeSpan.FromDays(30),
                EndTime = DateTime.UtcNow,
                Period = 60 * 60
            });
            return new SummaryResult()
            {
                MaxValue = (int)res.Datapoints.Max(d => d.Sum),
                MinValue = (int)res.Datapoints.Min(d => d.Sum),
                Groups = res.Datapoints.Select(d => new DateData() { Date = d.Timestamp.Date, Hour = d.Timestamp.Hour, Y = d.Sum })
                    .GroupBy(d => d.Date)
                    .OrderBy(d => d.Key)
                    .ToDictionary(d => d.Key, d => d.OrderBy(da => da.Hour).ToList())
                    .Select(pair => new DateGroup()
                    {
                        Key = pair.Key,
                        Values = pair.Value,
                    })
            };
        }
    }

    public class SummaryResult
    {
        public int MaxValue;
        public int MinValue;
        public IEnumerable<DateGroup> Groups;
    }

    public class DateGroup
    {
        public DateTime Key { get; set; }
        public IEnumerable<DateData> Values { get; set; }
    }

    public class DateData
    {
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public double Y { get; set; }
    }
}