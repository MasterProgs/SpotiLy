using System;
using System.Collections.Generic;
using System.Text;

namespace SpotiLyAccUpgrade
{
    public class Token
    {
        public string InviteCode { get; set; }
        public string FullAdresse { get; set; }

        public override string ToString()
        {
            return $"{InviteCode}:{FullAdresse}";
        }
    }
}
