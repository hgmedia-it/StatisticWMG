using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatisticWMG
{
    public class Artists
    {
        public List<Items> items { get; set; }
    }
    public class Items
    {
        public List<string> genres { get; set; }
        public string id { get; set; }
    }
}
