using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace RootNav.Core.MixtureModels
{
    public class EMPatch
    {
        public class EMPatchException : Exception
        {
            public EMPatchException(String message) : base(message) { }
        }

        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public EMPatch(int x, int y, int left, int right, int top, int bottom)
        {
            if (right <= left || bottom <= top)
                throw new EMPatchException("Invalid parameters for EMPatch");
            
            this.Left = left;
            this.Right = right;
            this.Top = top;
            this.Bottom = bottom;
            this.X = x;
            this.Y = y;
        }

        public static EMPatch CreateKey(int x, int y)
        {
            return new EMPatch(x, y, 0, 1, 0, 1);
        }

        public override bool Equals(object obj)
        {
            EMPatch patch = obj as EMPatch;
            if (patch == null)
                return false;
            else
            {
                return patch.X == this.X && patch.Y == this.Y;
            }
        }

        public override int GetHashCode()
        {
            int hCode = this.X ^ this.Y;
            return hCode.GetHashCode();
        }

        public int[] CreateHistogram(byte[] intensityBuffer, int bufferWidth, int bufferHeight)
        {
            // Check bounds
            if (this.Left < 0 || this.Right > bufferWidth || this.Top < 0 || this.Bottom > bufferHeight)
                throw new EMPatch.EMPatchException("Patch falls outside the bounds of the source image");

            // Copy data
            int patchWidth = this.Right - this.Left;
            int patchHeight = this.Bottom - this.Top;
            int left = this.Left, right = this.Right, top = this.Top, bottom = this.Bottom;

            int[] histogram = new int[255];

            for (int pX = left; pX < right; pX++)
                for (int pY = top; pY < bottom; pY++)
                    histogram[intensityBuffer[pY * bufferWidth + pX]]++;

            return histogram;
        }
    }

    public class EMPatchComparer : IEqualityComparer<EMPatch>
    {
        public bool Equals(EMPatch p1, EMPatch p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public int GetHashCode(EMPatch p1)
        {
            int hCode = p1.X ^ p1.Y;
            return hCode.GetHashCode();
        }
    }
}
