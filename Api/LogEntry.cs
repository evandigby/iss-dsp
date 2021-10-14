using CsvHelper.Configuration;
using System;

namespace Api
{
    public class LogEntry
    {
        //2021-10-13 23:36:57,2021-10-13 23:37:51,2021-10-13 23:38:57,6456,6570,32.9002239196049,-96.96302798539023
        public DateTime SignalStart { get; set; }
        public DateTime SignalPeak { get; set; }
        public DateTime SignalEnd { get; set; }

        public decimal SignalFrequency { get; set; }
        public decimal SignalPower { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

    }

    public sealed class LogEntryMap : ClassMap<LogEntry>
    {
        public LogEntryMap()
        {
            Map(m => m.SignalStart).Index(0);
            Map(m => m.SignalPeak).Index(1);
            Map(m => m.SignalEnd).Index(2);
            Map(m => m.SignalFrequency).Index(3);
            Map(m => m.SignalPower).Index(4);
            Map(m => m.Latitude).Index(5);
            Map(m => m.Longitude).Index(6);
        }
    }
}
