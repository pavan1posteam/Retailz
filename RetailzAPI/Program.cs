using RetailzAPI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailzAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
            try
            {
                POSsettings pOSSettings = new POSsettings();
                pOSSettings.IntializeStoreSettings();
                foreach (POSSetting current in pOSSettings.PosDetails)
                {
                    try
                    {

                        if (current.PosName.ToUpper() == "RETAILZPOS")
                        {
                            if (current.StoreSettings.StoreId == 12785)
                            {
                                Console.WriteLine("Fetching the storeid " + current.StoreSettings.StoreId);
                                Retailz retailz = new Retailz(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.AuthKey, current.StoreSettings.POSSettings.Token);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {

            }
        }
    }
}
