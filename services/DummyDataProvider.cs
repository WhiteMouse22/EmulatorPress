using System;
using System.Collections.Generic;
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
        public IList<TimeSpan> XValues;
        public IList<double> YValues;
    }

    public class DummyDataProvider : IDataProvider
    {
        private readonly Random _random = new Random();
        private double _last; // предыдущее значение давления
        private double _next; // следующее значение давления
        private TimeSpan _x = TimeSpan.FromSeconds(0); //координата Х
        private int _interval = 100; // интервал времени
        private int speed = 1; //количество координат за интервал времени
        public int X = 1;
        public DispatcherTimer Timer = new DispatcherTimer(DispatcherPriority.Render);//Render - Операции обрабатываются с таким же приоритетом, как и отрисовка.
        public double Value; // эмуляционное значение
        public SignalType _signalType; // режим эмуляции

        public void SubscribeUpdates(Action<XyValues> onDataUpdated)
        {
            // LicenseManager.UsageMode Возвращает объект LicenseUsageMode,
            // определяющий, когда можно использовать лицензированный объект для контекста CurrentContext.
            bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            if (designMode) return;

            Timer.Interval = TimeSpan.FromMilliseconds(_interval); //шаг
            Timer.Tick += (s, e) =>
            {
                var xyValues = GenerateRandomWalk(speed); // генерируем новые координаты
                onDataUpdated(xyValues);            // отправляем их на визуализацию
            };
        }
        private XyValues GenerateRandomWalk( int speed)
        {
            XyValues values = new XyValues
            {
                XValues = new List<TimeSpan>(),
                YValues = new List<double>(),
            };

            for (int i = 0; i < speed; i++)
            {
                double step; // шаг изменения давления
                int shag = _interval / speed;
                if (_signalType == SignalType.Constant) // режим постоянного давления
                {
                    _last = _next + Value / 5;
                    _next = (_last < Value) ? _last : Value;
                }
                else if (_signalType == SignalType.Randoms) // режим случайного давления
                {

                    step = _random.NextDouble() + _random.Next(-(int)Value / 4, (int)Value / 4);
                    _last = _next + step;
                    if (_last > Value) _last = Value;
                    else if (_last < 0) _last = 0;
                    _next = _last;
                }
                else // режимы роста и падения
                {
                    step = (_signalType != SignalType.NegativeStep) ? Value : -Value; // если падение, то Value отрицательный
                    _next = _last + step;
                    _last = _next;
                }

                X = X + _interval / speed;
                values.XValues.Add(_x.Add(TimeSpan.FromMilliseconds(X)));  // устанавливаем новую координату Х
                values.YValues.Add(_next); // устанавливаем новую координату Y
            }
            return values;
        }
    }
}
