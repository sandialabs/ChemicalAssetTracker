using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;


namespace Common
{

    public class ImportData
    {
        public string Institution { get; set; }
        public string Site { get; set; }
        public List<string> Locations { get; set; }
        public List<string> Owners { get; set; }
        public List<string> Groups { get; set; }
        public List<ImportCASData> CASData { get; set; }
        public List<ImportInventoryItem> Items { get; set; }

        public static ImportData FromFile(string filename)
        {
             string json = System.IO.File.ReadAllText(filename);
             ImportData result = JsonConvert.DeserializeObject<ImportData>(json);
             return result;
        }
    }


    public class ImportCASData
    {
        public string CASNumber { get; set; }
        public string ChemicalName { get; set; }
        public char CWCFlag { get; set; }
        public char TheftFlag { get; set; }
        public char CarcinogenFlag { get; set; }
    }

    public class ImportInventoryItem
    {
        public string Barcode { get; set; }
        public string ChemicalName { get; set; }
        public string CASNumber { get; set; }
        public string Location { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }
        public string DateIn { get; set; }
        public string ExpirationDate { get; set; }
        public double? ContainerSize { get; set; }
        public double? RemainingQuantity { get; set; }
        public string Units { get; set; }
        public string State { get; set; }
        public string Flags { get; set; }
        public string MSDS { get; set; }
        public string Notes { get; set; }
    }

}