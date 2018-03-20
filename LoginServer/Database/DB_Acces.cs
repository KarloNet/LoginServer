using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace LoginServer.Database
{
    static class DB_Acces
    {
        public static List<Player> PlayerList(int UID)
        {
            List<Player> pList = new List<Player>();
            Player p;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM PLAYER WHERE UID = @uid";
                cm.Parameters.Add("@uid", SqlDbType.Int).Value = UID;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    p = new Player();
                    p.PlayerUID = rdr.GetInt32(0);
                    p.PlayerPID = rdr.GetInt32(1);
                    p.PlayerName = rdr.GetString(2);
                    p.Strength = rdr.GetInt32(3);
                    p.Health = rdr.GetInt32(4);
                    p.Intel = rdr.GetInt32(5);
                    p.Wisdom = rdr.GetInt32(6);
                    p.Agility = rdr.GetInt32(7);
                    p.PosX = rdr.GetInt32(8);
                    p.PosY = rdr.GetInt32(9);
                    p.PosZ = rdr.GetInt32(10);
                    p.Race = rdr.GetInt32(11);
                    p.Job = rdr.GetInt32(12);
                    p.Level = rdr.GetInt32(13);
                    p.FaceType = rdr.GetInt32(14);
                    p.HairType = rdr.GetInt32(15);
                    p.Experience = rdr.GetInt32(16);
                    p.ActHealth = rdr.GetInt32(17);
                    p.ActMana = rdr.GetInt32(18);
                    p.ActRage = rdr.GetInt32(19);
                    p.HeadArmor = rdr.GetInt32(20);
                    p.GlovesArmor = rdr.GetInt32(21);
                    p.ShortsArmor = rdr.GetInt32(22);
                    p.BootsArmor = rdr.GetInt32(23);
                    p.LeftHand = rdr.GetInt32(24);
                    p.RightHand = rdr.GetInt32(25);
                    pList.Add(p);
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            return pList;
        }

        //on succes return UID on fail return -1 on error return -2 (doubled UID's - never should happend)
        public static int Login(byte[] ID, byte[] PW)
        {
            int userUID = -1;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM LOGIN WHERE Name = @id AND Password = @pw";
                cm.Parameters.Add("@id", SqlDbType.VarBinary, 50).Value = ID;
                cm.Parameters.Add("@pw", SqlDbType.VarBinary, 50).Value = PW;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    if (userUID == -1)
                    {
                        userUID = rdr.GetInt32(0);
                    }
                    else
                    {
                        userUID = -2;
                        break;
                    }
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            return userUID;
        }

        //on succes return UID on fail return -1 on error return -2 (doubled UID's - never should happend)
        public static int NewLogin(byte[] ID, byte[] PW)
        {
            object obj;
            int userUID = -1;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "INSERT INTO LOGIN (Name, Password) OUTPUT INSERTED.UID VALUES (@id, @pw)";
                // Note: -1 maps to the nvarchar(max) length, we use 50 as in stored in DB is nvarbinary(50);
                cm.Parameters.Add("@id", SqlDbType.VarBinary, 50).Value = ID;
                cm.Parameters.Add("@pw", SqlDbType.VarBinary, 50).Value = PW;
                cn.Open();
                try
                {
                    obj = cm.ExecuteScalar();
                    if (obj != null)
                    {
                        userUID = (int)obj;
                    }
                }
                catch (SqlException ex)
                {
                    // the exception alone won't tell you why it failed...
                    if (ex.Number == 2627) // <-- but this will that its volatile UNIQUE constrain
                    {
                        userUID = -2;
                    }
                    else
                    {
                        if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::NewLogin Ther was an error processing SQL query: " + ex.Message);
                        userUID = -3;
                    }
                }
                cn.Close();
                cm = null;
            }
            return userUID;
        }

        public static Inventory PlayerInventory(int PlayerID)
        {
            Item tempItem;
            ItemTemplate template;
            Inventory inv = new Inventory();
            // Load inventory from database
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM INVENTORY WHERE PlayerID = @id";
                cm.Parameters.Add("@id", SqlDbType.Int).Value = PlayerID;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    tempItem = new Item();
                    tempItem.DBID = rdr.GetInt32(0);
                    tempItem.Index = rdr.GetInt32(2);
                    tempItem.Count = rdr.GetInt32(3);
                    tempItem.Prefix = rdr.GetInt32(4);
                    tempItem.Info = rdr.GetInt32(5);
                    tempItem.MaxEndurance = rdr.GetInt32(6);
                    tempItem.CurrentEndurance = rdr.GetInt32(7);
                    tempItem.SetGem = rdr.GetInt32(8);
                    tempItem.AttackTalis = rdr.GetInt32(9);
                    tempItem.MagicTalis = rdr.GetInt32(10);
                    tempItem.Defense = rdr.GetInt32(11);
                    tempItem.OnTargetPoint = rdr.GetInt32(12);
                    tempItem.Dodge = rdr.GetInt32(13);
                    tempItem.Protect = rdr.GetInt32(14);
                    tempItem.EBLevel = rdr.GetInt32(15);
                    tempItem.EBRate = rdr.GetInt32(16);

                    //if this item is worn by player ( info == 1 )
                    if (tempItem.Info == 1)
                    {
                        template = TemplateManager.GetItemTemplate(tempItem.Index);
                        if (template.Class == ItemClass.Weapon) inv.Weapon = tempItem;
                        if (template.Class == ItemClass.Defense)
                        {
                            switch (template.Subclass)
                            {
                                case ItemSubclass.Chest:
                                    inv.Chest = tempItem;
                                    break;

                                case ItemSubclass.Helmet:
                                    inv.Helmet = tempItem;
                                    break;

                                case ItemSubclass.Gloves:
                                    inv.Gloves = tempItem;
                                    break;

                                case ItemSubclass.Boots:
                                    inv.Boots = tempItem;
                                    break;

                                case ItemSubclass.Shorts:
                                    inv.Shorts = tempItem;
                                    break;

                                case ItemSubclass.Shield:
                                    inv.Shield = tempItem;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        inv.AddToInventory(tempItem);
                    }
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            return inv;
        }

        public static bool DeletePlayer(int userID, int playerID)
        {
            bool fStatus = false;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "DELETE FROM PLAYER WHERE UID = @uid AND PID = @pid";
                cm.Parameters.Add("@uid", SqlDbType.Int).Value = userID;
                cm.Parameters.Add("@pid", SqlDbType.Int).Value = playerID;
                cn.Open();
                try
                {
                    cm.ExecuteScalar();
                    fStatus = true;
                }
                catch (SqlException ex)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::DeletePlayer Ther was an error processing SQL query: " + ex.Message);
                }
                cn.Close();
                cm = null;
            }
            if (fStatus)//succesfull deleted player - now delete all items belongs to this player
            {
                using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
                {
                    SqlCommand cm = cn.CreateCommand();
                    cm.CommandText = "DELETE FROM INVENTORY WHERE PlayerID = @pid";
                    cm.Parameters.Add("@pid", SqlDbType.Int).Value = playerID;
                    cn.Open();
                    try
                    {
                        cm.ExecuteScalar();
                    }
                    catch (SqlException ex)
                    {
                        if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::DeletePlayer Ther was an error processing SQL query: " + ex.Message);
                    }
                    cn.Close();
                    cm = null;
                }
            }
            return fStatus;
        }

        public static bool CreatePlayer(int userID, string playerName, int race, int face, int hair)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // YOU MUST ADD ROLBACK CHANGES IF ANY STEP WILL FAIL!
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            bool fStatus = false;
            bool iStatus = false;
            int newPlayerID = -1;
            int count = 0;
            int hArmor = 0;
            int gArmor = 0;
            int cArmor = 0;
            int sArmor = 0;
            int bArmor = 0;
            int lHand = 0;
            int rHand = 0;
            object obj;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT TOP 1 * FROM PLAYER WHERE Name = @name";
                cm.Parameters.Add("@name", SqlDbType.VarChar, 10).Value = playerName;
                cn.Open();
                //SqlDataReader rdr = null;
                try
                {
                    obj = cm.ExecuteScalar();
                    if (obj != null)
                    {
                        count = (int)(obj);
                    }
                    else
                    {
                        count = -1;
                    }
                    iStatus = true;
                    //rdr = cm.ExecuteReader();
                }
                catch (SqlException ex) 
                {
                    if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::CreatePlayer #001 Ther was an error processing SQL query: " + ex.Message);
                }
                cn.Close();
                cm = null;
            }
            if (count <= 0 && iStatus)
            {
                using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
                {
                    SqlCommand cm = cn.CreateCommand();
                    cm.CommandText = "INSERT INTO PLAYER (UID, Name, Strength, Health, Intel, Wisdom, Agi, PosX, PosY, PosZ, Class, SubClass, Level, Face, Hair, Exp, ActHealth, ActMana, Rage, HeadArmor, GlovesArmor, ChestArmor, ShortsArmor, BootsArmor, LeftHand, RightHand) OUTPUT INSERTED.PID VALUES (@uid, @name, @str, @hp, @int, @wis, @agi, @px, @py, @pz, @class, @job, @lvl, @face, @hair, @exp, @actHp, @actMana, @rage, @hArmor, @gArmor, @cArmor, @sArmor, @bArmor, @lHand, @rHand)";
                    // Note: -1 maps to the nvarchar(max) length, we use 50 as in stored in DB is nvarbinary(50);
                    cm.Parameters.Add("@uid", SqlDbType.Int).Value = userID;
                    cm.Parameters.Add("@name", SqlDbType.VarChar, 10).Value = playerName;
                    cm.Parameters.Add("@str", SqlDbType.Int).Value = Player.DefStrength((Player.RACE)race);
                    cm.Parameters.Add("@hp", SqlDbType.Int).Value = Player.DefHealth((Player.RACE)race);
                    cm.Parameters.Add("@int", SqlDbType.Int).Value = Player.DefInteligence((Player.RACE)race);
                    cm.Parameters.Add("@wis", SqlDbType.Int).Value = Player.DefWisdom((Player.RACE)race);
                    cm.Parameters.Add("@agi", SqlDbType.Int).Value = Player.DefAgility((Player.RACE)race);
                    cm.Parameters.Add("@px", SqlDbType.Int).Value = Player.DefPosX((Player.RACE)race);
                    cm.Parameters.Add("@py", SqlDbType.Int).Value = Player.DefPosY((Player.RACE)race);
                    cm.Parameters.Add("@pz", SqlDbType.Int).Value = Player.DefPosZ((Player.RACE)race);
                    cm.Parameters.Add("@class", SqlDbType.Int).Value = race;
                    cm.Parameters.Add("@job", SqlDbType.Int).Value = 1;
                    cm.Parameters.Add("@lvl", SqlDbType.Int).Value = Player.DefLevel((Player.RACE)race);
                    cm.Parameters.Add("@face", SqlDbType.Int).Value = face;
                    cm.Parameters.Add("@hair", SqlDbType.Int).Value = hair;
                    cm.Parameters.Add("@exp", SqlDbType.Int).Value = Player.DefExperience((Player.RACE)race);
                    cm.Parameters.Add("@actHp", SqlDbType.Int).Value = Player.DefActualHelath((Player.RACE)race);
                    cm.Parameters.Add("@actMana", SqlDbType.Int).Value = Player.DefActualMana((Player.RACE)race);
                    cm.Parameters.Add("@rage", SqlDbType.Int).Value = Player.DefRage((Player.RACE)race);
                    /*
                    cm.Parameters.Add("@hArmor", SqlDbType.Int).Value = Player.DefHeadArmor((Player.RACE)race);
                    cm.Parameters.Add("@gArmor", SqlDbType.Int).Value = Player.DefGlovesArmor((Player.RACE)race);
                    cm.Parameters.Add("@cArmor", SqlDbType.Int).Value = Player.DefChestArmor((Player.RACE)race);
                    cm.Parameters.Add("@sArmor", SqlDbType.Int).Value = Player.DefShortsArmor((Player.RACE)race);
                    cm.Parameters.Add("@bArmor", SqlDbType.Int).Value = Player.DefBootsArmor((Player.RACE)race);
                    cm.Parameters.Add("@lHand", SqlDbType.Int).Value = Player.DefLeftHand((Player.RACE)race);
                    cm.Parameters.Add("@rHand", SqlDbType.Int).Value = Player.DefRightHand((Player.RACE)race);
                    */
                    //after succesfull created player we will get his new PID
                    //then we create starting items for him usong his PID and update values in DB PLAYER
                    cm.Parameters.Add("@hArmor", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@gArmor", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@cArmor", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@sArmor", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@bArmor", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@lHand", SqlDbType.Int).Value = 0;
                    cm.Parameters.Add("@rHand", SqlDbType.Int).Value = 0;
                    cn.Open();
                    try
                    {
                        obj = cm.ExecuteScalar();
                        if (obj != null)
                        {
                            newPlayerID = (int)obj;
                            fStatus = true;
                        }
                    }
                    catch (SqlException ex)
                    {
                        if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::CreatePlayer #002 Ther was an error processing SQL query: " + ex.Message);
                    }
                    cn.Close();
                    cm = null;
                }
                if (fStatus)//succes create new player
                {
                    //create now starting items
                    for (int i = 0; i < 7; i++)
                    {
                        iStatus = false;
                        using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
                        {
                            SqlCommand cm = cn.CreateCommand();
                            cm.CommandText = "INSERT INTO INVENTORY (PlayerID, Item, Count, Prefix, Info, MaxEndurance, CurEndurance, SetGem, AddAttack, AddMagic, Defense, OTP, Dodge, Protect, EBGrade, EBRate) OUTPUT INSERTED.ItemID VALUES (@pid, @item, @count, @prefix, @info, @maxEndurance, @curEndurance, @setGem, @addAttack, @addMagic, @def, @otp, @dodge, @protect, @ebGrade, @ebRate)";
                            cm.Parameters.Add("@pid", SqlDbType.Int).Value = newPlayerID;
                            switch (i)
                            {
                                case 0:
                                    //Head armor
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefHeadArmor((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 1:
                                    //gloves armor
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefGlovesArmor((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 2:
                                    //chest armor
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefChestArmor((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 3:
                                    //Shorts armor
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefShortsArmor((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 4:
                                    //Boots armor
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefBootsArmor((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 5:
                                    //Left hand weapon
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefLeftHand((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                                case 6:
                                    //Right hand weapon
                                    cm.Parameters.Add("@item", SqlDbType.Int).Value = Player.DefRightHand((Player.RACE)race);
                                    cm.Parameters.Add("@count", SqlDbType.Int).Value = 1;
                                    cm.Parameters.Add("@prefix", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@info", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@maxEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@curEndurance", SqlDbType.Int).Value = 10;
                                    cm.Parameters.Add("@setGem", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addAttack", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@addMagic", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@def", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@otp", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@dodge", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@protect", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebGrade", SqlDbType.Int).Value = 0;
                                    cm.Parameters.Add("@ebRate", SqlDbType.Int).Value = 0;
                                    break;
                            }
                            cn.Open();
                            try
                            {
                                switch (i)
                                {
                                    case 0:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            hArmor = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 1:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            gArmor = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 2:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            cArmor = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 3:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            sArmor = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 4:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            bArmor = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 5:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            lHand = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                    case 6:
                                        obj = cm.ExecuteScalar();
                                        if (obj != null)
                                        {
                                            rHand = (int)obj;
                                            iStatus = true;
                                        }
                                        break;
                                }
                            }
                            catch (SqlException ex)
                            {
                                if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::CreatePlayer #003 Ther was an error processing SQL query: " + ex.Message);
                                iStatus = false;
                            }
                            cn.Close();
                            cm = null;
                        }
                        if (!iStatus)
                        {
                            return false;
                        }
                    }
                    //now update values in Player with new created item ID
                    if (fStatus && iStatus)
                    {
                        iStatus = false;
                        using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
                        {
                            SqlCommand cm = cn.CreateCommand();
                            cm.CommandText = "UPDATE PLAYER SET HeadArmor = @hArmor , GlovesArmor = @gArmor , ChestArmor = @cArmor , ShortsArmor = @sArmor , BootsArmor = @bArmor , LeftHand = @lHand , RightHand = @rHand WHERE PID = @pid";
                            cm.Parameters.Add("@hArmor", SqlDbType.Int).Value = hArmor;
                            cm.Parameters.Add("@gArmor", SqlDbType.Int).Value = gArmor;
                            cm.Parameters.Add("@cArmor", SqlDbType.Int).Value = cArmor;
                            cm.Parameters.Add("@sArmor", SqlDbType.Int).Value = sArmor;
                            cm.Parameters.Add("@bArmor", SqlDbType.Int).Value = bArmor;
                            cm.Parameters.Add("@lHand", SqlDbType.Int).Value = lHand;
                            cm.Parameters.Add("@rHand", SqlDbType.Int).Value = rHand;
                            cm.Parameters.Add("@pid", SqlDbType.Int).Value = newPlayerID;
                            cn.Open();
                            try
                            {
                                cm.ExecuteScalar();
                                iStatus = true;
                            }
                            catch (SqlException ex)
                            {
                                if (Program.DEBUG_recv) Output.WriteLine("DB_Acces::CreatePlayer #004 Ther was an error processing SQL query: " + ex.Message);
                            }
                            cn.Close();
                            cm = null;
                        }
                    }
                }
            }
            if (fStatus && iStatus)
            {
                return true;
            }
            else
            {
                //do rolback changes
                return false;
            }
        }

        public static void Monster()
        {

        }

        public static void Item()
        {

        }

    }
}
