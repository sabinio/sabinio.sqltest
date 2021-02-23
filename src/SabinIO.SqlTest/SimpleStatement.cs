using System;

namespace SabinIO.SqlTest
{
    public struct SimpleStatement
    {
        public int eventid;
        public DateTime start;
        public int logical;
        public int physical;
        public int cpu;
        public int duration;

        public SimpleStatement(int eventid, DateTime start, int logical, int physical, int cpu, int duration)
        {
            this.eventid = eventid;
            this.start = start;
            this.logical = logical;
            this.physical = physical;
            this.cpu = cpu;
            this.duration = duration;
        }

        public override bool Equals(object obj)
        {
            return obj is SimpleStatement other &&
                   eventid == other.eventid &&
                   start == other.start &&
                   logical == other.logical &&
                   physical == other.physical &&
                   cpu == other.cpu &&
                   duration == other.duration;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(eventid, start, logical, physical, cpu, duration);
        }

        public void Deconstruct(out int eventid, out DateTime start, out int logical, out int physical, out int cpu, out int duration)
        {
            eventid = this.eventid;
            start = this.start;
            logical = this.logical;
            physical = this.physical;
            cpu = this.cpu;
            duration = this.duration;
        }

        public static implicit operator (int eventid, DateTime start, int logical, int physical, int cpu, int duration)(SimpleStatement value)
        {
            return (value.eventid, value.start, value.logical, value.physical, value.cpu, value.duration);
        }

        public static implicit operator SimpleStatement((int eventid, DateTime start, int logical, int physical, int cpu, int duration) value)
        {
            return new SimpleStatement(value.eventid, value.start, value.logical, value.physical, value.cpu, value.duration);
        }
    }
}
