using System;
using System.Collections;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Timers;
using System.Windows;
using CommonObjects.BL;

namespace SmExportForWeb
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string ExportDate = "exportDate";
        private const string ExportPeriod = "exportPeriod";
        private const string IsPeriodicalExportChecked = "isPeriodicalExportChecked";

        private bool dayChanged;
        private bool exportIsActive;

        private Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            Title =
                $"SM file exporter {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            exportIsActive = true;
            var dataExporter = new MultiColumnsDataExporter();
            try
            {
                var exportDateTime = DateTime.Now;
                if (!(sender is Timer))
                    ExportButton.IsEnabled = false;
                var resultDescriptor = await dataExporter.DoExportAsync(TextBox.Text, DatePicker.SelectedDate,
                    false);
                if (!(sender is Timer))
                {
                    Clipboard.SetData("Text", resultDescriptor.FileName);
                    MessageBox.Show(
                        $"Данные выгружены в файл {resultDescriptor.FileName}, имя файла скопировано в буфер.");
                }
                await ServerInteraction.WriteLogAsync($"Exported {resultDescriptor.TotalCount} products");

                SaveParamValue(ExportDate, exportDateTime.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                if (!(sender is Timer))
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                await ServerInteraction.WriteLogAsync(ex.Message + ex.StackTrace);
            }
            finally
            {
                if (!(sender is Timer))
                {
                    ExportButton.IsEnabled = true;
                    ProgressBar.Visibility = Visibility.Hidden;
                }
                exportIsActive = false;
            }
        }

        private void SaveParamValue(string paramName, string paramValue)
        {
            var configuration =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings[paramName] == null)
                configuration.AppSettings.Settings.Add(new KeyValueConfigurationElement(paramName, paramValue));
            else
                configuration.AppSettings.Settings[paramName].Value = paramValue;
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private string LoadParamValue(string paramName)
        {
            return
            ((IList) ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                .AppSettings.Settings.AllKeys).Contains(paramName)
                ? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).AppSettings.Settings[
                    paramName].Value
                : null;
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Smdk"]?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Не настроено подключение к базе данных(Smdk) в файле app.config", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ExportButton.IsEnabled = false;
            }

            var strDate = LoadParamValue(ExportDate);
            if (strDate != null)
                DatePicker.SelectedDate =
                    DateTime.ParseExact(strDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var exportPeriod = LoadParamValue(ExportPeriod);
            if (exportPeriod != null)
            {
                TimePicker.Value = DateTime.ParseExact(exportPeriod, "MM/dd/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture);
                exportTime = TimePicker.Value ?? DateTime.MinValue;
            }
            var isPeriodicalExportChecked = LoadParamValue(IsPeriodicalExportChecked);
            timer = new Timer { Interval = 1000 };
            timer.Elapsed += Timer_Elapsed;
            if (!string.IsNullOrEmpty(isPeriodicalExportChecked))
            {
                CbPeriodicalExport.IsChecked = bool.Parse(isPeriodicalExportChecked);
                TimePicker.IsEnabled = CbPeriodicalExport.IsChecked ?? false;
                if (TimePicker.IsEnabled)
                {
                    timer.Start();
                    dayChanged = true;
                }
            }
          
            await ServerInteraction.InitAsync();
        }

        private void TimePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SaveParamValue(ExportPeriod,
                TimePicker.Value?.ToString(CultureInfo.InvariantCulture) ??
                DateTime.MinValue.ToString(CultureInfo.InvariantCulture));
            exportTime = TimePicker.Value ?? DateTime.MinValue;
            
        }

        private void CbPeriodicalExport_OnChecked(object sender, RoutedEventArgs e)
        {
            SaveParamValue(IsPeriodicalExportChecked,
                CbPeriodicalExport.IsChecked.ToString());
            TimePicker.IsEnabled = CbPeriodicalExport.IsChecked ?? false;
            if (CbPeriodicalExport.IsChecked == true)
            {
                timer.Start();
                dayChanged = true;
            }
            else timer.Stop();
        }

        private DateTime exportTime;
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentTime = DateTime.Now;
            if (!dayChanged && currentTime.Hour == 0)
                dayChanged = true;
                if ( exportTime.Hour >= currentTime.Hour &&
                 exportTime.Minute >= currentTime.Minute && dayChanged)
            {
                dayChanged = false;
                if (!exportIsActive)
                    button_Click(timer, new RoutedEventArgs());
            }
        }
    }
}