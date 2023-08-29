using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorPress
{
    public class Setting
    {
        public Guid Id { get; set; }
        public SignalType Type { get; set; }
        public double Value { get; set; }

    }
    public enum SignalType
    {
        Constant,
        Randoms,
        Step,
        NegativeStep
    }
}
