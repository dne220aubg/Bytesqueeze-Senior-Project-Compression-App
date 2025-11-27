using System;

namespace SeniorProjectCompressionApp.Models
{
    internal readonly struct Token
    {
        private Token(bool l, bool e, byte v, int len, int dist, int lc, int leb, int lev, int dc, int deb, int dev)
        {
            IsLiteral = l; IsEndOfBlock = e; LiteralValue = v; Length = len; Distance = dist;
            LengthCode = lc; LengthExtraBits = leb; LengthExtraValue = lev;
            DistanceCode = dc; DistanceExtraBits = deb; DistanceExtraValue = dev;
        }
        public bool IsLiteral { get; }
        public bool IsEndOfBlock { get; }
        public byte LiteralValue { get; }
        public int Length { get; }
        public int Distance { get; }
        public int LengthCode { get; }
        public int LengthExtraBits { get; }
        public int LengthExtraValue { get; }
        public int DistanceCode { get; }
        public int DistanceExtraBits { get; }
        public int DistanceExtraValue { get; }

        public static Token ForLiteral(byte v) => new Token(true, false, v, 0, 0, 0, 0, 0, 0, 0, 0);
        public static Token ForMatch(int len, int dist, int lc, int leb, int lev, int dc, int deb, int dev) => new Token(false, false, 0, len, dist, lc, leb, lev, dc, deb, dev);
        public static Token EndOfBlock() => new Token(false, true, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
