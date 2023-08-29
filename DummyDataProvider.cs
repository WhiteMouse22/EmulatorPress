using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace EmulatorPress
{
    public interface IDataProvider
    {
        void SubscribeUpdates(Action<XyValues> onDataUpdated);
    }
    public struct XyValues
    {
        public double XValue;
        public double YValue;
    }

    public class DummyDataProvider : IDataProvider
    {
        private readonly Random _random = new Random();
        private double _last;
        private double _next;
        private int _x = 1;
        public DispatcherTimer Timer = new DispatcherTimer(DispatcherPriority.Render);//Render - Операции обрабатываются с таким же приоритетом, как и отрисовка.
        public double Value;
        public SignalType SignalType;

        public void SubscribeUpdates(Action<XyValues> onDataUpdated)
        {
            // LicenseManager.UsageMode Возвращает объект LicenseUsageMode,
            // определяющий, когда можно использовать лицензированный объект для контекста CurrentContext.
            bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            if (designMode) return;

            Timer.Interval = TimeSpan.FromMilliseconds(100); 
            Timer.Tick += (s, e) =>
            {
                var xyValues = GenerateRandomWalk();
                onDataUpdated(xyValues);
            };
        }
        private XyValues GenerateRandomWalk()
        {
            XyValues values = new XyValues();
            double step;

            if (SignalType == SignalType.Constant)
            {
                _last = _next + Value / 5;
                _next = (_last < Value) ? _last : Value;
            }
            else if (SignalType == SignalType.Randoms)
            {

                step = _random.NextDouble() + _random.Next(-(int)Value / 4, (int)Value / 4);
                _last = _next + step;
                if (_last > Value) _last = Value;
                else if (_last < 0) _last = 0;
                _next = _last;
            }
            else
            {
                step = (SignalType != SignalType.NegativeStep) ? Value : - Value;
                _next = _last + step;
                _last = _next;
            }

            values.XValue = _x++;
            values.YValue = _next;

            return values;
        }
    }
}
