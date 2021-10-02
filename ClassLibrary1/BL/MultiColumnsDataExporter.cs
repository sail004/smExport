using System.Collections.Generic;
using System.Linq;
using CommonObjects.Model;
using OfficeOpenXml;

namespace CommonObjects.BL
{
    public class MultiColumnsDataExporter : SimpleExcelDataExporter
    {
        protected override void InnerFormat(List<Product> readProducts, ExcelWorkbook workBook)
        {
            var productSheet = workBook.Worksheets[1];
            var storeNames = readProducts.Select((product, _) => product.NameStore).Distinct().ToList();
            for (int j = 0; j < storeNames.Count; j++)
            {
                productSheet.Cells[1, 13 + j * 2].Value = storeNames[j];
            }

            var i = 1;
            var index = 0;
            var currArtucul = "";
            while (index < readProducts.Count)
            {
                if (currArtucul != readProducts[index].Id)
                {
                    i++;
                    currArtucul = readProducts[index].Id;
                    productSheet.Cells[i, 1].Value = readProducts[index].Id;
                    productSheet.Cells[i, 2].Value = readProducts[index].Name;
                    productSheet.Cells[i, 7].Value = readProducts[index].Manufacturer;
                    productSheet.Cells[i, 12].Value = readProducts[index].GroupName;
                }

                
                for (int j = 0; j < storeNames.Count; j++)
                {
                    if (storeNames[j] == readProducts[index].NameStore)
                    {
                        productSheet.Cells[i, 13 + j * 2].Value = readProducts[index].Price;
                        productSheet.Cells[i, 13 + j * 2 + 1].Value = readProducts[index].Rest;
                    }
                }

                index++;
            }
        }
    }
}