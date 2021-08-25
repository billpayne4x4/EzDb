using System;

namespace MPSHouse
{
    public class HashIdConfig
    {
        public string Salt { get; set; }
        public int Length { get; set; }
        public string Alphabet { get; set; }
        public string Seps { get; set; }
    }
}