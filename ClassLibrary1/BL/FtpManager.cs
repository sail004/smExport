using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CommonObjects.Descriptors;

namespace CommonObjects.BL
{
	public class FtpManager
	{
		private const string TiuServerName = "ftp://tiuexport.doc-e.ru/";
		private const string FotosDirName = "fotos/";
		private const string FtpUser = "tiuexport.doc-e.ru|ftp_user";
		private const string FtpPassword = "ftp_user";

		public bool EnsureFtpDirectory()
		{
			try
			{
				var request = (FtpWebRequest) WebRequest.Create(TiuServerName + FotosDirName + ServerInteraction.Client.Name );
				request.Credentials = new NetworkCredential(FtpUser, FtpPassword);
				request.Method = WebRequestMethods.Ftp.MakeDirectory;
				request.UseBinary = true;
				request.UsePassive = false;
				var response = (FtpWebResponse) request.GetResponse();
			}
			catch (WebException ex)
			{
				var response = (FtpWebResponse) ex.Response;
				if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
					return false;
				return true;
			}
			return true;
		}

		public string UploadFile(string filePath)
		{
			var result = string.Empty;
			var fileName = Path.GetFileName(filePath);


			var uriString = TiuServerName + FotosDirName + ServerInteraction.Client.Name + "/" + fileName;

			var serverUri = new Uri(uriString);
			var request = (FtpWebRequest) WebRequest.Create(serverUri);
			request.Method = WebRequestMethods.Ftp.UploadFile;


			request.Credentials = new NetworkCredential(FtpUser, FtpPassword);
			var fileInfo = new FileInfo(filePath);
			request.UseBinary = true;
			request.UsePassive = false;
			request.ContentLength = fileInfo.Length;
			const int buffLength = 2048;
			var buff = new byte[buffLength];

			var fs = fileInfo.OpenRead();
			try
			{
				using (var strm = request.GetRequestStream())
				{
					var contentLen = fs.Read(buff, 0, buffLength);

					while (contentLen != 0)
					{
						strm.Write(buff, 0, contentLen);
						contentLen = fs.Read(buff, 0, buffLength);
					}
					strm.Close();
					fs.Close();
				}
			}
			catch (Exception ex)
			{
				result = ex.Message;
			}
			File.Delete(fileInfo.FullName);

			return result;
		}

		public UploadDescriptor UploadDir(string localDirName, IProgress<double> progress)
		{
			var dirInf = new DirectoryInfo(localDirName);
			var fi = dirInf.GetFiles("*.*");
			var counter = 0;
			var descriptorStringBuilder = new StringBuilder();
			var resultDescriptor = new UploadDescriptor();


			if (fi.Any())
				foreach (var fileInfo in fi)
				{
					var uriString = "ftp://tiuexport.doc-e.ru/fotos/" + fileInfo.Name;
					descriptorStringBuilder.Append($"Выгрузка файла {fileInfo.FullName} на ftp...{uriString}");

					var serverUri = new Uri(uriString);
					var request = (FtpWebRequest) WebRequest.Create(serverUri);
					request.Method = WebRequestMethods.Ftp.UploadFile;

					request.Credentials = new NetworkCredential("tiuexport.doc-e.ru|ftp_user", "ftp_user");

					request.UseBinary = true;
					request.UsePassive = false;
					request.ContentLength = fileInfo.Length;
					const int buffLength = 2048;
					var buff = new byte[buffLength];

					var fs = fileInfo.OpenRead();
					try
					{
						var strm = request.GetRequestStream();

						var contentLen = fs.Read(buff, 0, buffLength);

						while (contentLen != 0)
						{
							strm.Write(buff, 0, contentLen);
							contentLen = fs.Read(buff, 0, buffLength);
						}
						descriptorStringBuilder.Append("Загружено на  ftp " + fileInfo.Length + " байт");
						strm.Close();
						fs.Close();
						progress?.Report(counter++ / (double) fi.Length * 100);
					}
					catch (Exception ex)
					{
						descriptorStringBuilder.Append(ex.Message);
					}
					File.Delete(fileInfo.FullName);
				}
			resultDescriptor.TotalCount = fi.Length;
			resultDescriptor.Description = descriptorStringBuilder.ToString();
			resultDescriptor.SuccesProcessedCount = counter;
			return resultDescriptor;
		}
	}
}