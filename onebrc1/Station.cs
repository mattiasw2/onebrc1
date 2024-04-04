using System.Runtime.CompilerServices;
using System.Text;

namespace onebrc1
{
    public struct Station
    {
        internal int Count;
        internal float Total;

        internal float Max;
        internal float _min;

        internal readonly string _name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Station(ReadOnlySpan<byte> name, float init)
        {
            _name = Encoding.UTF8.GetString(name);

            Total = init;
            _min = init;
            Max = init;
            Count = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Combine(Station station)
        {
            Count += station.Count;
            Total += station.Total;

            if (station.Max > Max)
            {
                Max = station.Max;
            }
            else if (station._min < _min)
            {
                _min = station._min;
            }
        }

        public readonly string ToString()
        {
            return $"{_name} = {_min}/{Math.Round(Total / Count, 1)}/{Max}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(float value)
        {
            Count++;
            Total += value;

            if (value > Max)
            {
                Max = value;
            }
            else if (value < _min)
            {
                _min = value;
            }
        }
    }
}
