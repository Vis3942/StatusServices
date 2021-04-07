using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusServices
{
    internal class TimeEnvelope
    {
        private readonly int _count;
        private int _time = 0;
        private readonly bool _throwOnOverflow;

        public static void AddTime(IEnumerable<TimeEnvelope> envelopes, int value)
        {
            foreach (var envelope in envelopes)
            {
                envelope.AddTime(value);
            }
        }

        public TimeEnvelope(int count, bool throwOnOverflow = true)
        {
            _count = count;
            _throwOnOverflow = throwOnOverflow;
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public int Time
        {
            get
            {
                return _time;
            }
        }

        public int AddTime(int value)
        {
            _time += value;
            if (_time >= _count)
            {
                _time = 0;
            }
            else if (_throwOnOverflow && _time > _count)
            {
                throw new Exception("TimeEnvelope count overflowed!");
            }

            return _time;
        }

        public bool IsFirstMultiple(int value)
        {
            return _time == value;
        }

        public bool IsLastMultiple(int value)
        {
            return _count - value == _time;
        }

        public bool IsMultiple(int value)
        {
            return _time % value == 0;
        }
    }
}
