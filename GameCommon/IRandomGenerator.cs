using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public interface IRandomGenerator
    {
        // Generates a random integer between the given values.
        int RandomInt(int minInclusive, int maxInclusive);
    }
}
