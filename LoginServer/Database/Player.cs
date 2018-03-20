using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace LoginServer.Database
{
    class Player
    {
        public enum RACE
        {
            KNIGHT = 1,
            ARCHER = 2,
            MAGE = 3
        }

        int UID;
        int PID;
        string Name;
        int Face;
        int Hair;
        int Str;
        int Hp;
        int Int;
        int Wis;
        int Agi;
        int Px;
        int Py;
        int Pz;
        int Class;
        int SubClass;
        int Lvl;
        int Exp;
        int curHealth;
        int curMana;
        int Rage;
        int hArmor;
        int gArmor;
        int cArmor;
        int sArmor;
        int bArmor;
        int lHand;
        int rHand;

        public int PlayerUID { get { return this.UID; } set { this.UID = value; } }
        public int PlayerPID { get { return this.PID; } set { this.PID = value; } }
        public string PlayerName { get { return this.Name; } set { this.Name = value; } }
        public int FaceType { get { return this.Face; } set { this.Face = value; } }
        public int HairType { get { return this.Hair; } set { this.Hair = value; } }
        public int Strength { get { return this.Str; } set { this.Str = value; } }
        public int Health { get { return this.Hp; } set { this.Hp = value; } }
        public int Intel { get { return this.Int; } set { this.Int = value; } }
        public int Wisdom { get { return this.Wis; } set { this.Wis = value; } }
        public int Agility { get { return this.Agi; } set { this.Agi = value; } }
        public int PosX { get { return this.Px; } set { this.Px = value; } }
        public int PosY { get { return this.Py; } set { this.Py = value; } }
        public int PosZ { get { return this.Pz; } set { this.Pz = value; } }
        public int Race { get { return this.Class; } set { this.Class = value; } }
        public int Job { get { return this.SubClass; } set { this.SubClass = value; } }
        public int Level { get { return this.Lvl; } set { this.Lvl = value; } }
        public int Experience { get { return this.Exp; } set { this.Exp = value; } }
        public int ActHealth { get { return this.curHealth; } set { this.curHealth = value; } }
        public int ActMana { get { return this.curMana; } set { this.curMana = value; } }
        public int ActRage { get { return this.Rage; } set { this.Rage = value; } }
        public int HeadArmor { get { return this.hArmor; } set { this.hArmor = value; } }
        public int GlovesArmor { get { return this.gArmor; } set { this.gArmor = value; } }
        public int ChestArmor { get { return this.cArmor; } set { this.cArmor = value; } }
        public int ShortsArmor { get { return this.sArmor; } set { this.sArmor = value; } }
        public int BootsArmor { get { return this.bArmor; } set { this.bArmor = value; } }
        public int LeftHand { get { return this.lHand; } set { this.lHand = value; } }
        public int RightHand { get { return this.rHand; } set { this.rHand = value; } }


        public static int DefStrength(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 8;
                case RACE.MAGE:
                    return 4;
                default:
                    return 0;
            }
        }

        public static int DefHealth(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 12;
                case RACE.ARCHER:
                    return 10;
                case RACE.MAGE:
                    return 8;
                default:
                    return 0;
            }
        }

        public static int DefInteligence(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 4;
                case RACE.ARCHER:
                    return 6;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefWisdom(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 8;
                case RACE.ARCHER:
                    return 8;
                case RACE.MAGE:
                    return 10;
                default:
                    return 0;
            }
        }

        public static int DefAgility(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 8;
                case RACE.ARCHER:
                    return 12;
                case RACE.MAGE:
                    return 6;
                default:
                    return 0;
            }
        }

        public static int DefPosX(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 15340;
                case RACE.ARCHER:
                    return 15280;
                case RACE.MAGE:
                    return 12400;
                default:
                    return 0;
            }
        }

        public static int DefPosY(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 15500;
                case RACE.ARCHER:
                    return 15300;
                case RACE.MAGE:
                    return 12000;
                default:
                    return 0;
            }
        }

        public static int DefPosZ(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 0;
                case RACE.ARCHER:
                    return 0;
                case RACE.MAGE:
                    return 0;
                default:
                    return 0;
            }
        }

        public static int DefLevel(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 1;
                case RACE.ARCHER:
                    return 1;
                case RACE.MAGE:
                    return 1;
                default:
                    return 0;
            }
        }

        public static int DefExperience(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 0;
                case RACE.ARCHER:
                    return 0;
                case RACE.MAGE:
                    return 0;
                default:
                    return 0;
            }
        }

        public static int DefActualHelath(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 1000;
                case RACE.ARCHER:
                    return 800;
                case RACE.MAGE:
                    return 600;
                default:
                    return 0;
            }
        }

        public static int DefActualMana(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 600;
                case RACE.ARCHER:
                    return 800;
                case RACE.MAGE:
                    return 1000;
                default:
                    return 0;
            }
        }

        public static int DefRage(RACE race)
        {
            switch (race)
            {
                case RACE.KNIGHT:
                    return 0;
                case RACE.ARCHER:
                    return 0;
                case RACE.MAGE:
                    return 0;
                default:
                    return 0;
            }
        }

        public static int DefHeadArmor(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefGlovesArmor(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefChestArmor(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefShortsArmor(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefBootsArmor(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefLeftHand(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }

        public static int DefRightHand(RACE race)
        {
            //item index
            switch (race)
            {
                case RACE.KNIGHT:
                    return 10;
                case RACE.ARCHER:
                    return 11;
                case RACE.MAGE:
                    return 12;
                default:
                    return 0;
            }
        }


    }
}
