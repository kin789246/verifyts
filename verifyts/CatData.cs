using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace verifyts
{
    class CatData
    {
        private string catName;
        private string catCategory;
        private string osSupport;

        public string CatName { get => catName; set => catName = value; }
        public string CatCategory { get => catCategory; set => catCategory = value; }
        public string OsSupport { get => osSupport; set => osSupport = value; }

        public CatData() 
        {
            catName = string.Empty;
            catCategory = string.Empty;
            osSupport = string.Empty;
        }

        public override string ToString()
        {
            return "=====================\n" +
                CatName + "\n" +
                catCategory + "\n" +
                osSupport + "\n" +
                "=====================\n\n";
        }
    }
}
