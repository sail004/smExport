using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        protected readonly FbManager FbManager;
        
        protected string ExpDir;

      
        public async Task<ExportDescriptor> DoExportAsync(string exportDir, DateTime? datePickerSelectedDate, bool? isChecked)
        {
            return await Task.Run(() =>
            {
                ExpDir = exportDir;

                FbManager.Init(ConfigurationManager.ConnectionStrings["Smdk"].ConnectionString, datePickerSelectedDate);

                return
                    ExportInner(FbManager.ReadProductGroups(), FbManager.ReadProducts(), exportDir);
            });
        }

        protected abstract ExportDescriptor ExportInner(List<ProductGroup> readProductGroups,
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