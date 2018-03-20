using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Database
{
    class Item
    {
        private int _dbid;
        private int _index;
        private int _count;
        private int _prefix;
        private int _info;
        private int _maxendurance;
        private int _curendurance;
        private int _setgem;
        private int _attack;
        private int _magic;
        private int _defense;
        private int _otp;
        private int _dodge;
        private int _protect;
        private int _EBlevel;
        private int _EBrate;

        public int DBID { get { return this._dbid; } set { this._dbid = value; } }
        public int Index { get { return this._index; } set { this._index = value; } }
        public int Prefix { get { return this._prefix; } set { this._prefix = value; } }
        public int Info { get { return this._info; } set { this._info = value; } }
        public int Count { get { return this._count; } set { this._count = value; } }
        public int MaxEndurance { get { return this._maxendurance; } set { this._maxendurance = value; } }
        public int CurrentEndurance { get { return this._curendurance; } set { this._curendurance = value; } }
        public int SetGem { get { return this._setgem; } set { this._setgem = value; } }
        public int AttackTalis { get { return this._attack; } set { this._attack = value; } }
        public int MagicTalis { get { return this._magic; } set { this._magic = value; } }
        public int Defense { get { return this._defense; } set { this._defense = value; } }
        public int OnTargetPoint { get { return this._otp; } set { this._otp = value; } }
        public int Dodge { get { return this._dodge; } set { this._dodge = value; } }
        public int Protect { get { return this._protect; } set { this._protect = value; } }
        public int EBLevel { get { return this._EBlevel; } set { this._EBlevel = value; } }
        public int EBRate { get { return this._EBrate; } set { this._EBrate = value; } }
    }
}
