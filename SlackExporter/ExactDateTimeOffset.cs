using System;

namespace SlackExporter
{
    struct ExactDateTimeOffset : IComparable<ExactDateTimeOffset>, IEquatable<ExactDateTimeOffset>
    {
        private static readonly DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        double v;
        public ExactDateTimeOffset(double v) => this.v = v;
        public int CompareTo(ExactDateTimeOffset other) => v.CompareTo(other.v);
        public bool Equals(ExactDateTimeOffset other) => v.Equals(other.v);
        public DateTimeOffset ToStandardOffset() => TimeZoneInfo.ConvertTime(epoch.AddSeconds(v), Program.TargetTimeZone);
        public override string ToString() => ToStandardOffset().ToString("G");
        public string ToTimeString() => ToStandardOffset().ToString("T");
    }
}
