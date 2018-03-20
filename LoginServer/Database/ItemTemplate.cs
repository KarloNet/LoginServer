using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Database
{
    public enum ItemClass
    {
        Weapon = 1,
        Defense = 2,
        General = 4,
        Ornament = 8,
        Quest = 16
    }

    public enum ItemSubclass
    {
        //Weapon
        Sword = 1,
        Stick = 2,
        Bow = 4,
        //Defense
        Chest = 1,
        Helmet = 2,
        Gloves = 4,
        Boots = 8,
        Shorts = 16,
        Shield = 32
    }

    class ItemTemplate
    {
        private int _index;
        private ItemClass _class;
        private ItemSubclass _subclass;
        private int _minlevel;

        public int Index { get { return this._index; } set { this._index = value; } }
        public ItemClass Class { get { return this._class; } set { this._class = value; } }
        public ItemSubclass Subclass { get { return this._subclass; } set { this._subclass = value; } }
        public int MinLevel { get { return this._minlevel; } set { this._minlevel = value; } }
    }
}
