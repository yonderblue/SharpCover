using System;

namespace Gaillard.SharpCover
{
    class HitCounter
    {
        public int Hit;
        public int Miss;

        public HitCounter()
        {
            Hit = 0;
            Miss = 0;
        }

        public static HitCounter operator +(HitCounter a, HitCounter b)
        {
            HitCounter o = new HitCounter();
            o.Hit = a.Hit + b.Hit;
            o.Miss = a.Miss + b.Miss;
            return o;
        }
    }
}

