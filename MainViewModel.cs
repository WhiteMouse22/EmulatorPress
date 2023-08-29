using LiteDB;
using SciChart.Charting.Common.Helpers;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Data.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace EmulatorPress
{

    public class MainViewModel : BindableObject
    {
        private readonly DummyDataProvider _dummyDataProvider = new DummyDataProvider();
        private XyDataSeries<double, double> lineData = new XyDataSeries<double, double> { SeriesName = "Давление испытания" };

        #region _renderableSeries // отрисовывает график
        private ObservableCollection<IRenderableSeriesViewModel> _renderableSeries;
        public ObservableCollection<IRenderableSeriesViewModel> RenderableSeries
        {
            get { return _renderableSeries; }
            set
            {
                _renderableSeries = value;
                OnPropertyChanged(nameof(RenderableSeries));
            }
        }
        #endregion
        #region  Масштабирование, панорамирование
        private bool _enableZoom = true;
        public bool EnableZoom
        {
            get { return _enableZoom; }
            set
            {
                if (_enableZoom != value)
                {
                    _enableZoom = value;
                    OnPropertyChanged(nameof(EnableZoom));
                    if (_enableZoom) EnablePan = false;
                }
            }
        }
        private bool _enablePan;
        public bool EnablePan
        {
            get { return _enablePan; }
            set
            {
                if (_enablePan != value)
                {
                    _enablePan = value;
                    OnPropertyChanged(nameof(EnablePan));
                    if (_enablePan) EnableZoom = false;
                }
            }
        }
        #endregion
        #region Value
        private double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                _value = Math.Abs(value);
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(GetResult));
                SaveSetting();
                _dummyDataProvider.Value = _value;
            }
        }
        #endregion
        #region IsStopEnabled
        private bool _isStopEnabled=false;
        public bool IsStopEnabled
        {
            get { return _isStopEnabled; }
            set
            {
                _isStopEnabled = value;
                OnPropertyChanged(nameof(IsStopEnabled));
            }
        }
        #endregion
        #region IsStartEnabled
        private bool _isStartEnabled = true;
        public bool IsStartEnabled
        {
            get { return _isStartEnabled; }
            set
            {
                _isStartEnabled = value;
                OnPropertyChanged(nameof(IsStartEnabled));
            }
        }
        #endregion
        #region Тип сигнала
        SignalType _signalType = SignalType.Constant;

        public SignalType SignalType
        {
            get { return _signalType; }
            set
            {
                if (_signalType == value)
                    return;

                _signalType = value;
                OnPropertyChanged(nameof(SignalType));
                OnPropertyChanged(nameof(IsConstant));
                OnPropertyChanged(nameof(IsRandoms));
                OnPropertyChanged(nameof(IsStep));
                OnPropertyChanged(nameof(IsNegativeStep));
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(GetResult));
                _dummyDataProvider.SignalType = value;
                SaveSetting();
            }
        }
        public bool IsConstant
        {
            get { return SignalType == SignalType.Constant; }
            set { SignalType = value ? SignalType.Constant : SignalType; }
        }

        public bool IsRandoms
        {
            get { return SignalType == SignalType.Randoms; }
            set { SignalType = value ? SignalType.Randoms : SignalType; }
        }

        public bool IsStep
        {
            get { return SignalType == SignalType.Step; }
            set { SignalType = value ? SignalType.Step : SignalType; }
        }
        public bool IsNegativeStep
        {
            get { return SignalType == SignalType.NegativeStep; }
            set { SignalType = value ? SignalType.NegativeStep : SignalType; }
        }
        public string GetResult
        {
            get
            {
                switch (SignalType)
                {
                    case SignalType.Constant:
                        return "Статическое давление " + Value + " усл.ед.";
                    case SignalType.Randoms:
                        return "Случайное давление от 0 до " + Value + " усл.ед.";
                    case SignalType.Step:
                            return "Увеличение давления на " + Value + " усл.ед.";
                    case SignalType.NegativeStep:
                        return "Падение давления на " + Value + " усл.ед.";
                }
                return "";
            }
        }
        #endregion

        // Команды
        #region Старт
        private ActionCommand onEmulation;
        public ICommand OnEmulation
        {
            get
            {
                if (onEmulation == null)
                    onEmulation = new ActionCommand(PerformOnEmulation);
                return onEmulation;
            }
        }
        private void PerformOnEmulation()
        {
            SaveSetting();
            _dummyDataProvider.SignalType = _signalType;
            _dummyDataProvider.Value = _value;
            _dummyDataProvider.Timer.Start();
            _isStopEnabled = true;
            OnPropertyChanged(nameof(IsStopEnabled));
        }
        #endregion
        #region Стоп
        private ActionCommand offEmulation;
        public ICommand OffEmulation
        {
            get
            {
                if (offEmulation == null)
                    offEmulation = new ActionCommand(PerformOffEmulation);
                return offEmulation;
            }
        }
        private void PerformOffEmulation()
        {
            _dummyDataProvider.Timer.Stop();
            _isStopEnabled = false;
            _isStartEnabled = true;
            OnPropertyChanged(nameof(IsStopEnabled));
            OnPropertyChanged(nameof(IsStartEnabled));
        }
        #endregion
        #region Очистка графика
        private ActionCommand clearChart;
        public ICommand ClearChart
        {
            get
            {
                if (clearChart == null)
                    clearChart = new ActionCommand(PerformClearChart);
                return clearChart;
            }
        }
        private void PerformClearChart()
        {
            lineData.Clear();
            LoadNewChart();
        }
        #endregion

        public MainViewModel()
        {
            UpdateSettings(); // выгружаем настройки из БД
            _renderableSeries = new ObservableCollection<IRenderableSeriesViewModel>() { };
            RenderableSeries.Add(new LineRenderableSeriesViewModel()
            {
                StrokeThickness = 2, // толщина линии
                Stroke = Colors.LightBlue, // цвет линии
                DataSeries = lineData, 
                StyleKey = "LineSeriesStyle"
            });

            lineData.Append(0, 0); //начальная точка
            _dummyDataProvider.SubscribeUpdates((newValues) =>
            {
                lineData.Append(newValues.XValue, newValues.YValue);
                lineData.InvalidateParentSurface(RangeMode.ZoomToFit);// Масштабирование диаграммы по размеру
            });
        }

        private void LoadNewChart()
        {

        }

        private void SaveSetting()
        {
            using (var db = new LiteDatabase(@"SettingDB.db"))
            {
                var col = db.GetCollection<Setting>("setting");
                var _setting = new Setting
                {
                    Type = _signalType,
                    Value = _value
                };
                col.DeleteAll();
                col.Insert(_setting);
            }
        }
        private void UpdateSettings()
        {
            using (var db = new LiteDatabase(@"SettingDB.db"))
            {
                var col = db.GetCollection<Setting>("setting");
                var result = col.FindAll();
                foreach (var item in result)
                {
                    _signalType = item.Type;
                    _value = item.Value;
                }
            }
        }
    }
}
