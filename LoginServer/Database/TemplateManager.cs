using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace LoginServer.Database
{
    class TemplateManager
    {
        private static Dictionary<int, ItemTemplate> itemTemplates;

        static TemplateManager()
        {
            itemTemplates = new Dictionary<int, ItemTemplate>();
            LoadItemTemplates();
        }

        public static ItemTemplate GetItemTemplate(int itemIndex)
        {
            try
            {
                ItemTemplate itemTemplate = itemTemplates[itemIndex];
                return itemTemplate;
            }
            catch (KeyNotFoundException)
            {
                Output.WriteLine("TemplateManager::ItemTeplate An item whose key doesn't exist got requested");
            }
            return null;
        }

        private static void RegisterItemTemplate(ItemTemplate itemTemplate)
        {
            try
            {
                itemTemplates.Add(itemTemplate.Index, itemTemplate);
            }
            catch (ArgumentException)
            {
                Output.WriteLine("TemplateManager::RegisterItemTeplate Duplicate item index detected");
            }
        }

        private static void LoadItemTemplates()
        {
            Output.WriteLine("TemplateManager::LoadItemTemplates Loading item-templates");

            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM ITEMS";
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    ItemTemplate template = new ItemTemplate();
                    template.Index = rdr.GetInt32(0);
                    template.Class = (ItemClass)rdr.GetInt32(1);
                    template.Subclass = (ItemSubclass)rdr.GetInt32(2);
                    template.MinLevel = rdr.GetInt32(3);
                    RegisterItemTemplate(template);
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            Output.WriteLine("TemplateManager::LoadItemTemplates Item-templates loaded");
        }
    }
}
