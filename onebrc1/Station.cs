using System.Runtime.CompilerServices;
using System.Text;

namespace onebrc1
{
    public class Station
    {
        internal int Count;
        internal float Total;

        internal float Max;
        internal float Min;

        internal readonly string _name;

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Station(ReadOnlySpan<byte> name, float first)
        {
            _name = Encoding.UTF8.GetString(name);

            Total = first;
            Min = first;
            Max = first;
            Count = 1;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Combine(Station station)
        {
            Count += station.Count;
            Total += station.Total;

            if (station.Max > Max)
            {
                Max = station.Max;
            }
            else if (station.Min < Min)
            {
                Min = station.Min;
            }
        }

        public override string ToString()
        {
            return $"{_name} = {Min}/{Math.Round(Total / Count, 1)}/{Max}";
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(float value)
        {
            Count++;
            Total += value;

            if (value > Max)
            {
                Max = value;
            }
            else if (value < Min)
            {
                Min = value;
            }
        }
    }
}
