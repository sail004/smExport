using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using CommonObjects.Descriptors;
using CommonObjects.Model;
using OfficeOpenXml;

namespace CommonObjects.BL
{
    public class TiuDataExporter:BaseDataExporter
    {
        public TiuDataExporter()
        {
            ftpManager = new FtpManager();
        }
        private readonly FtpManager ftpManager;
        public Action<int, int> NotifyClient { get; set; }

     

        public async Task<UploadDescriptor> ExportFotosAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
                {
                    var fotosDirName = Path.Combine(ExpDir, "Fotos");
                    if (Directory.Exists(fotosDirName))
                        Directory.Delete(fotosDirName, true);
                    Directory.CreateDirectory(fotosDirName);
	                ftpManager.EnsureFtpDirectory();

					return FbManager.SaveFotos(fotosDirName,
                        ConfigurationManager.ConnectionStrings["Fotos"].ConnectionString, progress,
                        file => ftpManager.UploadFile(file));
                    
                }
            );
        }

        protected override ExportDescriptor SaveExcelFile(List<ProductGroup> readProductGroups,
            List<Product> readProducts, string exportDir)
        {

            EnsureExportDir(exportDir);
            var fileName = Path.Combine(exportDir, GetFileName());
                using (var outStream = new FileStream(fileName, FileMode.Create))
                {
                    var templateStream = File.OpenRead("xls-blank-short.xlsx");
                    using (var package = new ExcelPackage(outStream, templateStream))
                    {
                        var workBook = package.Workbook;

                        var productSheet = workBook.Worksheets[1];
                        for (var i = 0; i < readProducts.Count; i++)
                        {
                            productSheet.Cells[i + 2, 1].Value = readProducts[i].Comment;
                            productSheet.Cells[i + 2, 2].Value = readProducts[i].Name;
                            //productSheet.Cells[i + 2, 4].Value = readProducts[i].Comment;
                            productSheet.Cells[i + 2, 5].Value = "r";
                            productSheet.Cells[i + 2, 6].Value = readProducts[i].Price;
                            productSheet.Cells[i + 2, 7].Value = "RUB";
                            productSheet.Cells[i + 2, 9].Value = "шт.";
                            productSheet.Cells[i + 2, 10].Value = FileNamesToUrls(readProducts[i].Fotos);
                            productSheet.Cells[i + 2, 11].Value = readProducts[i].Rest;
                            productSheet.Cells[i + 2, 12].Value = readProducts[i].Id;
                            productSheet.Cells[i + 2, 13].Value = Convert.ToInt32(readProducts[i].GroupId);
                        }
                        var groupSheet = workBook.Worksheets[2];
                        for (var i = 0; i < readProductGroups.Count; i++)
                        {
                            groupSheet.Cells[i + 2, 1].Value = readProductGroups[i].Id;
                            groupSheet.Cells[i + 2, 2].Value = readProductGroups[i].Name;
                            groupSheet.Cells[i + 2, 3].Value = Convert.ToInt32(readProductGroups[i].Id);
                            groupSheet.Cells[i + 2, 4].Value = readProductGroups[i].ParentId;
                            if (readProductGroups[i].ParentId == string.Empty)
                                groupSheet.Cells[i + 2, 5].Value = string.Empty;
                            else
                                groupSheet.Cells[i + 2, 5].Value = Convert.ToInt32(readProductGroups[i].ParentId);
                        }
                        package.Save();
                    }
                    outStream.Flush();
                }

                return new ExportDescriptor {FileName = fileName, TotalCount = readProducts.Count};
        }

        
    }
}