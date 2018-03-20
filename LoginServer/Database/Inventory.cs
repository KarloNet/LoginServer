using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Database
{
    class Inventory
    {
        private Item _weapon = null;
        private Item _shield = null;
        private Item _chest = null;
        private Item _helmet = null;
        private Item _gloves = null;
        private Item _boots = null;
        private Item _shorts = null;
        private List<Item> _itemlist;

        public Item Weapon { get { return this._weapon; } set { this._weapon = value; } }
        public Item Shield { get { return this._shield; } set { this._shield = value; } }
        public Item Chest { get { return this._chest; } set { this._chest = value; } }
        public Item Helmet { get { return this._helmet; } set { this._helmet = value; } }
        public Item Gloves { get { return this._gloves; } set { this._gloves = value; } }
        public Item Boots { get { return this._boots; } set { this._boots = value; } }
        public Item Shorts { get { return this._shorts; } set { this._shorts = value; } }
        public int ItemCount { get { return this._itemlist.Count; } }

        public Inventory()
        {
            _itemlist = new List<Item>();
        }

        public void AddToInventory(Item itm)
        {
            _itemlist.Add(itm);
        }

        public List<Item> GetInventory()
        {
            return _itemlist;
        }
    }
}
