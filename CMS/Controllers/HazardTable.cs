using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Controllers
{

    ///----------------------------------------------------------------
    ///
    /// Class:          HazardTable
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Class that combines basic GHS and COC hazard information
    /// </summary>
    ///
    ///----------------------------------------------------------------
    public class HazardTable
    {
        public string CASNumber { get; set; }
        public string DataURL { get; set; }

        public bool COC_CWC { get; set; }
        public bool COC_CFATS { get; set; }
        public bool COC_EU { get; set; }
        public bool COC_AG { get; set; }
        public bool COC_WMD { get; set; }
        public bool COC_OTHER { get; set; }

        public bool GHS_HEALTHHAZARD { get; set; }
        public bool GHS_IRRITANT { get; set; }
        public bool GHS_ACUTETOXICITY { get; set; }
        public bool GHS_CORROSIVE { get; set; }
        public bool GHS_ENVIRONMENT { get; set; }
        public bool GHS_EXPLOSIVE { get; set; }
        public bool GHS_FLAMABLE { get; set; }
        public bool GHS_OXIDIZER { get; set; }
        public bool GHS_COMPRESSEDGAS { get; set; }

        public HazardTable(string cas_number, string data_url = null)
        {
            CASNumber = cas_number;
            DataURL = data_url;
        }

        public HazardTable SetValues(ChemicalOfConcern coc)
        {
            COC_CWC = coc.CWC;
            COC_CFATS = coc.CFATS;
            COC_EU = coc.EU;
            COC_AG = coc.AG;
            COC_WMD = coc.WMD;
            COC_OTHER = coc.OTHER;
            return this;
        }

        public HazardTable SetValues(CASData casdata)
        {
            // we do not have this data yet, so use random values
            Random rnd = new Random();

            GHS_EXPLOSIVE = casdata.Pictograms.Contains("GHS01");
            GHS_FLAMABLE = casdata.Pictograms.Contains("GHS02");
            GHS_OXIDIZER = casdata.Pictograms.Contains("GHS03");
            GHS_COMPRESSEDGAS = casdata.Pictograms.Contains("GHS04");
            GHS_CORROSIVE = casdata.Pictograms.Contains("GHS05");
            GHS_ACUTETOXICITY = casdata.Pictograms.Contains("GHS06");
            GHS_IRRITANT = casdata.Pictograms.Contains("GHS07");
            GHS_HEALTHHAZARD = casdata.Pictograms.Contains("GHS08");
            GHS_ENVIRONMENT = casdata.Pictograms.Contains("GHS09");
            return this;
        }
    }
}
