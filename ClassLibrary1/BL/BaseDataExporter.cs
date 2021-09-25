using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonObjects.Descriptors;
using CommonObjects.Model;

namespace CommonObjects.BL
{
    public abstract class BaseDataExporter
    {
        protected BaseDataExporter()
        {
            FbManager = new FbManager();
        }

        protected const string BaseUrl = "http://tiuexport.doc-e.ru/";
        protected readonly FbManager FbManager;
        
        protected string ExpDir;

        protected string FileNamesToUrls(string fileNames)
        {
            if (string.IsNullOrEmpty(fileNames))
                return fileNames;
            var fotosFolder = $"fotos/{ServerInteraction.Client?.Name ?? string.Empty}/";

            var targetUrl = BaseUrl + fotosFolder;
            return string.Join(", ",
                       fileNames.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => string.Concat(targetUrl, s))) + ".jpg";
        }
        public async Task<ExportDescriptor> DoExportAsync(string exportDir, DateTime? datePickerSelectedDate, bool? isChecked)
        {
            return await Task.Run(() =>
            {
                ExpDir = exportDir;

                FbManager.Init(ConfigurationManager.ConnectionStrings["Smdk"].ConnectionString, datePickerSelectedDate);

                return
                    SaveExcelFile(FbManager.ReadProductGroups(), FbManager.ReadProducts(), exportDir);
            });
        }

        protected abstract ExportDescriptor SaveExcelFile(List<ProductGroup> readProductGroups,
            List<Product> readProducts, string exportDir);
        
        protected string GetFileName()
        {
            return "sm_export" + DateTime.Now.ToString("yyyyMdhhmmss") + ".xlsx";
        }
        protected void EnsureExportDir(string exportDir)
        {
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);
        }
    }
}