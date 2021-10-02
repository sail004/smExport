using System;
using System.Collections.Generic;
using System.IO;
using CommonObjects.Descriptors;
using CommonObjects.Model;
using OfficeOpenXml;

namespace CommonObjects.BL
{
    public class SimpleExcelDataExporter : BaseDataExporter
    {
        protected override ExportDescriptor ExportInner(List<ProductGroup> readProductGroups,
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

                    InnerFormat(readProducts, workBook);

                    package.Save();
                }

                outStream.Flush();
            }

            return new ExportDescriptor {FileName = fileName, TotalCount = readProducts.Count};
        }

        protected virtual void InnerFormat(List<Product> readProducts, ExcelWorkbook workBook)
        {
            var productSheet = workBook.Worksheets[1];
            for (var i = 0; i < readProducts.Count; i++)
            {
                productSheet.Cells[i + 1, 1].Value = readProducts[i].Id;
                productSheet.Cells[i + 1, 2].Value = readProducts[i].Name;
                productSheet.Cells[i + 1, 5].Value = readProducts[i].Amount;
                productSheet.Cells[i + 1, 7].Value = readProducts[i].Manufacturer;
                productSheet.Cells[i + 1, 9].Value = readProducts[i].Price;
                productSheet.Cells[i + 1, 12].Value = readProducts[i].GroupName;
            }
        }
    }
}