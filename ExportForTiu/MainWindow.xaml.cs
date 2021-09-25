using System;
using System.Configuration;
using System.Deployment.Application;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using CommonObjects.BL;


namespace ExportForTiu
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private const string ExportDate = "exportDate";

		public MainWindow()
		{
			InitializeComponent();
			Title =
				$"Tiu file exporter {(ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion : Assembly.GetExecutingAssembly().GetName().Version)}";
		}

		private async void button_Click(object sender, RoutedEventArgs e)
		{
			var dataExporter = new TiuDataExporter();
			try
			{
				var exportDateTime = DateTime.Now;
				ExportButton.IsEnabled = false;
				var resultDescriptor = await dataExporter.DoExportAsync(TextBox.Text, DatePicker.SelectedDate,
					CbExportFotos.IsChecked);
				Clipboard.SetData("Text", resultDescriptor.FileName);
				MessageBox.Show(
					$"Данные выгружены в файл {resultDescriptor.FileName}, имя файла скопировано в буфер.{(CbExportFotos.IsChecked == true ? "\r\nСейчас будет произведена загрузка фотографий." : string.Empty)}");
				await ServerInteraction.WriteLogAsync($"Exported {resultDescriptor.TotalCount} products");
				if (CbExportFotos.IsChecked == true)
				{
					ProgressBar.Visibility = Visibility.Visible;
					var uploads =
						await dataExporter.ExportFotosAsync(new Progress<double>(value => ProgressBar.Value = value));
					var messageText = $"Загружено {uploads.SuccesProcessedCount} из {uploads.TotalCount} изображений.";

					MessageBox.Show(messageText);
					UploadResultText.Text = uploads.Description;
					UploadResultText.Visibility = Visibility.Visible;
					await ServerInteraction.WriteLogAsync(messageText);
					await ServerInteraction.WriteLogAsync(uploads.Description);
				}
				SaveParamValue(ExportDate, exportDateTime.ToString(CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Произошла ошибка: {ex.Message}");
				await ServerInteraction.WriteLogAsync(ex.Message + ex.StackTrace);
			}
			finally
			{
				ExportButton.IsEnabled = true;
				ProgressBar.Visibility = Visibility.Hidden;
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
				ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
					.AppSettings.Settings.AllKeys.Contains(paramName)
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
			var fotosConnectionString = ConfigurationManager.ConnectionStrings["Fotos"]?.ConnectionString;
			if (string.IsNullOrEmpty(fotosConnectionString))
			{
				MessageBox.Show("Не настроено подключение к базе данных(Fotos) изображений в файле app.config", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				CbExportFotos.IsEnabled = false;
			}
			var strDate = LoadParamValue(ExportDate);
			if (strDate != null)
				DatePicker.SelectedDate = DateTime.ParseExact(strDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
			await ServerInteraction.InitAsync();
			if (!ServerInteraction.IsOnline || !ServerInteraction.IsAuthorized)
			{
				MessageBox.Show("Сервер обработки изображений не доступен. Невозможно выгрузить изображения.", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				CbExportFotos.IsEnabled = false;
				CbExportFotos.IsChecked = false;
			}
		}
	}
}