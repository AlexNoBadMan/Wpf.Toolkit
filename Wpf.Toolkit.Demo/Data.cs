using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Toolkit.Demo
{
    public class Data
    {
        private readonly List<ShopItem> _shops = new List<ShopItem>();
        private readonly List<string> _operations = new List<string>();

        public List<ShopItem> Shops => _shops;
        public List<string> Operations => _operations;
        public Data()
        {

        }
    }

    public class ShopItem
    {
        public string ParentId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public List<ShopItem> Areas { get; set; } = new List<ShopItem>();
        public ShopItem()
        {

        }
    }
}
