using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Svg
{
    public class Vector
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Vector));
		
        private float x;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        private float y;

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        private float z;

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public string ToHtmlColor()
        {
            int r = (int)(X), g = (int)(Y), b = (int)(Z);
            return "#" + r.ToString("x2") + g.ToString("x2") + b.ToString("x2");
        }

        public static Vector operator *(Vector a, float m)
        {
            return new Vector( a.x * m, a.y * m, a.z * m);            
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return a + (b * -1);
        }

    }
}
