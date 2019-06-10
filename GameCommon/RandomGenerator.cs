using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GameCommon
{
    public interface IRandomGenerator
    {
        // Generates a crypto-random integer between the given values (inclusive).
        int RandomInt(int minInclusive, int maxInclusive);
    }

    /// <summary>
    /// A good source of random ints.
    /// </summary>
    public class RandomGenerator : IRandomGenerator
    {
        readonly RNGCryptoServiceProvider m_random = new RNGCryptoServiceProvider();

        // Generates a crypto-random integer between the given values (inclusive).
        public int RandomInt(int minInclusive, int maxInclusive)
        {
            int maxExclusive = maxInclusive + 1;
            long diff = (long)maxExclusive - minInclusive;
            long upperBound = uint.MaxValue / diff * diff;

            uint randomUInt;
            do
            {
                byte[] buffer = new byte[4];
                m_random.GetBytes(buffer);
                randomUInt = BitConverter.ToUInt32(buffer, 0);
            } while (randomUInt >= upperBound);

            return (int)(minInclusive + (randomUInt % diff));
        }
    }
}
