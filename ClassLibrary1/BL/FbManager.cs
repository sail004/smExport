using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using CommonObjects.Descriptors;
using CommonObjects.Model;
using FirebirdSql.Data.FirebirdClient;

namespace CommonObjects.BL
{
    public class FbManager
    {
        private FbConnection FbConnection { get; set; }
        private DateTime LastExportDate { get; set; }

        public void Init(string connectionString, DateTime? datePickerSelectedDate)
        {
            FbConnection = new FbConnection(connectionString);
            FbConnection.Open();
            LastExportDate = datePickerSelectedDate ?? DateTime.MinValue;
        }

        public List<Product> ReadProducts()
        {
            var fbCommand =
                new FbCommand(File.ReadAllText("mainQuery.Sql").Replace("@date", $"{LastExportDate:dd.MM.yyyy}"))
                {
                    Connection = FbConnection
                };

            var products = new List<Product>();

            var reader = fbCommand.ExecuteReader();
            while (reader.Read())
                products.Add(new Product
                {
                    Id = reader["id"].ToString(),
                    Name = reader["name"].ToString(),
                    CountryName = reader["name_country"].ToString(),
                    GroupName = reader["name_group"].ToString(),
                    GroupId = reader["classif"].ToString(),
                    NameStore = reader["name_store"].ToString(),
                    Rest = reader["rest"] != Convert.DBNull
                        ? Math.Round(Convert.ToDecimal(reader["rest"]), 3).ToString()
                        : string.Empty,
                    Manufacturer = reader["name_manufacturer"].ToString(),
                    Price = Convert.ToDecimal(reader["price"])
                });
            return products;
        }

        public List<ProductGroup> ReadProductGroups()
        {
            var fbCommand =
                new FbCommand(
                    "select c.id_classif,c.name_classif,null,case when c.parent_classif =-1 then null else c.parent_classif end as parent_classif, '' " +
                    " from classif c " +
                    " where c.type_classif = 0 " +
                    " order by coalesce(case when c.parent_classif = -1 then null else c.parent_classif end, 0), 2")
                {
                    Connection = FbConnection
                };


            var productGroups = new List<ProductGroup>();
            var reader = fbCommand.ExecuteReader();
            while (reader.Read())
                productGroups.Add(new ProductGroup
                {
                    Id = reader["id_classif"].ToString(),
                    Name = reader["name_classif"].ToString(),
                    ParentId = reader["parent_classif"].ToString()
                });
            return productGroups;
        }

        public IEnumerable<string> GetFotoFileNames()
        {
            return new List<string> {@"c:\temp\yes.png"};
        }

        public void SaveFotos(string dirName, string fotosConnectionString)
        {
        }

        public UploadDescriptor SaveFotos(string dirName, string fotosConnectionString, IProgress<double> progress,
            Func<string, string> uploadFunc = null)
        {
            var resultDescription = new UploadDescriptor();
            var descriptorMessageBuilder = new StringBuilder();
            var fotoFbConnection = new FbConnection(fotosConnectionString);
            fotoFbConnection.Open();

            var fbCommand =
                new FbCommand(
                    "select c.articul as id, f.id as id_foto, f.name_foto" +
                    " from cardscla c left join bar b on b.articul = c.articul left join country o on o.id_country = c.country left join mesuriment m on m.id_mesuriment = c.mesuriment " +
                    " left join cardparam_strong s on s.articul = c.articul left join cardparams cpa on cpa.paramtype = 2 and cpa.articul = c.articul left join disccard d on d.articul = c.articul and d.price_kind = 0 join cardfoto f on c.articul=f.articul" +
                    " where  c.articul != '<CLASSIF>' and c.mesuriment > 0 and c.classif > 0 and d.price_rub>0 and d.price_rub is not null" +
                    $" group by c.articul, f.id,f.name_foto,s.cash_moddate having max(s.cash_moddate)>'{LastExportDate:dd.MM.yyyy}'")
                {
                    Connection = FbConnection
                };


            var fbDataAdapter = new FbDataAdapter(fbCommand);
            var dataTable = new DataTable();
            var totalCount = fbDataAdapter.Fill(dataTable);
            var errorCount = 0;
            for (var i = 0; i < dataTable.Rows.Count; i++)
                try
                {
                    var id = dataTable.Rows[i]["id_foto"].ToString();
                    var name = dataTable.Rows[i]["id_foto"] + ".jpg";
                    var fbFotoCommand = new FbCommand($"select data_foto from cardfoto where id={id}")
                        {Connection = fotoFbConnection};
                    var data = (byte[]) fbFotoCommand.ExecuteScalar();
                    var fileName = Path.Combine(dirName, name);
                    progress?.Report(i / (double) totalCount * 100);

                    var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                    fs.Write(data, 0, data.Length);
                    fs.Close();


                    var uploadResult = uploadFunc?.Invoke(fileName);
                    if (!string.IsNullOrEmpty(uploadResult))
                        descriptorMessageBuilder.Append(uploadResult);
                }
                catch (Exception e)
                {
                    descriptorMessageBuilder.Append(
                        $"Ошибка артикул {dataTable.Rows[i]["id"]} {e.Message}{Environment.NewLine}");
                    errorCount++;
                }

            resultDescription.Description = descriptorMessageBuilder.ToString();
            resultDescription.TotalCount = totalCount;
            resultDescription.SuccesProcessedCount = totalCount - errorCount;
            return resultDescription;
        }
    }
}