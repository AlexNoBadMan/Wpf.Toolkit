using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Wpf.Toolkit.Demo
{
    public class DemoViewModel : PropertyChangedBase
    {
        private ShopItem _selectedShop;
        private ShopItem _selectedArea;
        private string _addSelectedOperation;

        public Data Data { get; }

        public List<ShopItem> Shops => Data.Shops.ToList();

        public ShopItem SelectedShop { get => _selectedShop; set => Set(ref _selectedShop, value); }

        public ShopItem SelectedArea { get => _selectedArea; set => Set(ref _selectedArea, value); }

        public List<string> OperationsSource => Data.Operations.ToList();

        public ObservableCollection<Operation> Operations { get; }

        public string AddSelectedOperation 
        { 
            get => _addSelectedOperation; 
            set 
            {
                if (value != null)
                { 
                    _addSelectedOperation = value;
                    Operations.Add(new Operation() { Number = $"{Operations.Count + 1}", Name = _addSelectedOperation });
                }
            }
        }

        public DemoViewModel()
        {
            var categoryStream = GetType().Assembly.GetManifestResourceStream("Wpf.Toolkit.Demo.Data.xml");
            Data = (Data)new XmlSerializer(typeof(Data)).Deserialize(categoryStream);
            Operations = new ObservableCollection<Operation>()
            {
                new Operation() { Number = "1", Name = OperationsSource.First() },
                new Operation() { Number = "2", Name = OperationsSource.Last() },
            };

        }
    }
}
