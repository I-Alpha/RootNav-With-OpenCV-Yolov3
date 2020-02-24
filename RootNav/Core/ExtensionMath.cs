using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core
{
    /// <summary>
    /// Static class providing some additional maths algorithms.
    /// </summary>
    public static class ExtentionMath
    {
        /// <summary>
        /// Clamps a value into a specified range.
        /// </summary>
        /// <param name="val">The value to be clamped.</param>
        /// <param name="lowerBound">The lower bound at which to clamp the value.</param>
        /// <param name="upperBound">The upper bound at which to clamp the value.</param>
        /// <returns></returns>
        public static int Clamp(int val, int lowerBound, int upperBound)
        {
            return (val > upperBound) ? upperBound : (val < lowerBound ? lowerBound : val);
        }
    }
}
