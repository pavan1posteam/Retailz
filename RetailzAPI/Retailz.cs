using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RetailzAPI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RetailzAPI
{
    class Retailz
    {
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        string DiffQty = ConfigurationManager.AppSettings["differentQty"];
        //string DiffStores = ConfigurationManager.AppSettings["differentstores"];
        string PriceQuantity = ConfigurationManager.AppSettings["PriceQuantity"];
        string StaticQuantity = ConfigurationManager.AppSettings["StaticQuantity"];
        string excludeCategories = ConfigurationManager.AppSettings["ExcludeCategories"] ;
        public Retailz(int storeid, decimal tax, string baseurl, string authkey, string token)
        {
            Console.WriteLine("Generating Product File For Store: "+storeid);
            ResposeToCSV(storeid, tax, baseurl, authkey, token);
        }

        public List<JArray> GetResponse(string baseurl, string authkey, string token, int storeid)
        {
            List<JArray> itemList = new List<JArray>();
            try
            {

                var client1 = new RestClient(baseurl + "/Item/?page=1&Size=500");
                var request1 = new RestRequest(Method.GET);

                request1.AddHeader("AuthKey", authkey);
                request1.AddHeader("Token", token);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client1.Execute(request1);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseContent = response.Content;
                    var pJson = JObject.Parse(responseContent);

                    // Assuming the API returns total item counts in "totalCounts"
                    var totalCountsToken = pJson["data"]?["totalCounts"];
                    int totalCounts = totalCountsToken != null ? (int)totalCountsToken : 0;

                    // Assuming each page returns 500 items
                    int itemsPerPage = 500;
                    int totalPages = (int)Math.Ceiling((double)totalCounts / itemsPerPage);

                    for (int page = 1; page <= totalPages; page++)
                    {
                        var client = new RestClient(baseurl + "/Item/?page=" + page +"&Size="+itemsPerPage);
                        var request = new RestRequest(Method.GET);

                        request.AddHeader("AuthKey", authkey);
                        request.AddHeader("Token", token);

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        IRestResponse pageResponse = client.Execute(request);

                        if (pageResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string pageResponseContent = pageResponse.Content;
                            //File.AppendAllText("12007.json", pageResponseContent);

                            var pageJson = JObject.Parse(pageResponseContent);
                            var dataToken = pageJson["data"];
                            var data = dataToken["data"];

                            if (data is JArray jArray)
                            {
                                itemList.Add(jArray);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("" + ex.Message);
                (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in RetailzAPI@" + storeid + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
            }
            return itemList;
        }
        public void ResposeToCSV(int storeid, decimal tax, string baseurl, string authkey, string token)
        {
            var productlist = GetResponse(baseurl, authkey, token, storeid);
            string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
            List<ProductModels> pf = new List<ProductModels>();
            List<FullName> fullname = new List<FullName>();

            ProductModels pdf;

            FullName fnf;
            try
            {
                foreach (var item in productlist)
                {
                    foreach (var itm in item)
                    {
                        pdf = new ProductModels();
                        fnf = new FullName();
                        pdf.StoreID = storeid;
                        pdf.sku = "#" + itm["sku"].ToString();
                        fnf.sku = "#" + itm["sku"].ToString();
                        pdf.upc = "#" + itm["item_Upc"].ToString();
                        fnf.upc = "#" + itm["item_Upc"].ToString();
                        pdf.uom = itm["sizeName"].ToString();
                        fnf.uom = itm["sizeName"].ToString();
                        // pdf.Tax = tax;
                        pdf.Tax = itm["tax"] != null && itm["tax"].Any() ? Convert.ToDecimal(itm["tax"].First()["persentage"]) / 100 : tax;
                        pdf.StoreProductName = itm["name"].ToString();
                        fnf.pname = itm["name"].ToString();
                        pdf.StoreDescription = itm["name"].ToString();
                        fnf.pdesc = itm["name"].ToString();
                        pdf.Start = "";
                        pdf.sprice = 0;
                        if (StaticQuantity.Contains(storeid.ToString()))
                        {
                            pdf.Qty = 999;
                        }
                        else
                        {
                            pdf.Qty = Convert.ToInt32(itm["storeQty"]) > 0 ? Convert.ToInt32(itm["storeQty"]) : 0;
                        }

                        pdf.Price = Convert.ToDecimal(itm["priceperUnit"]);
                        fnf.Price = Convert.ToDecimal(itm["priceperUnit"]);
                        pdf.pack = 1;
                        fnf.pack = 1;
                        pdf.End = "";
                        pdf.deposit = "";
                        pdf.altupc5 = "";
                        pdf.altupc4 = "";
                        pdf.altupc3 = "";
                        pdf.altupc2 = "";
                        pdf.altupc1 = "";
                        fnf.region = "";
                        fnf.pcat2 = "";
                        fnf.region = "";
                        fnf.country = "";

                        if (excludeCategories.Contains(storeid.ToString()))
                        {
                            string cat = itm["departmentName"].ToString().ToUpper();

                            if (cat.Contains("TOBACCO") ||
                                cat.Contains("E-CIGAR") ||
                                cat.Contains("CIGARETTE") ||
                                cat.Contains("CIG CTN") ||
                                cat.Contains("CIG PACK"))
                            {

                                continue;
                            }

                            else
                        {
                            fnf.pcat = itm["departmentName"].ToString();
                        }

                        }
                        else
                        {
                            fnf.pcat = itm["departmentName"].ToString();
                        }



                        if (PriceQuantity.Contains(storeid.ToString()))
                        {
                            if (pdf.Qty > 0 && pdf.Price > 0)
                            {
                                pf.Add(pdf);
                                fullname.Add(fnf);
                            }
                        }
                        else
                        {
                            if (pdf.Price > 0)
                            {
                                pf.Add(pdf);
                                fullname.Add(fnf);
                            }
                        }                      
                    }
                }

                if (storeid == 12007)
                {
                    authkey = "2dT0WBgbU+WHYUoo0ZhqbIMo4oEds3PwrxrwITMgmLw=";
                    token = "o+bMoXeNz10DCdSA+HIJLUSu8vR8t2bQsvcEUgFsTvXJUJ0APwCcboDj0MOVlFRQJc+6ffaR4lB1xgAnQvSO4Bw9mK1GEFzQ7wuNI2M9mtuNX+4CY9QQUX8RnD5KpEj19z5VAEKzqXbGqWLp2gREmkZm/8Ea6qxtsz4OXAfOFCw=";
                    var OtherStoreproductlist = GetResponse(baseurl, authkey, token, storeid);

                    List<ProductModels> pf1 = new List<ProductModels>();
                    List<FullName> fullname1 = new List<FullName>();

                    foreach (var items in OtherStoreproductlist)
                    {
                        foreach (var itms in items)
                        {
                            pdf = new ProductModels();
                            fnf = new FullName();

                            pdf.StoreID = storeid;
                            pdf.sku = "#" + itms["sku"].ToString();
                            fnf.sku = "#" + itms["sku"].ToString();
                            pdf.upc = "#" + itms["item_Upc"].ToString();
                            fnf.upc = "#" + itms["item_Upc"].ToString();
                            pdf.uom = itms["sizeName"].ToString();
                            fnf.uom = itms["sizeName"].ToString();
                            pdf.Tax = tax;
                            pdf.StoreProductName = itms["name"].ToString();
                            fnf.pname = itms["name"].ToString();
                            pdf.StoreDescription = itms["name"].ToString();
                            fnf.pdesc = itms["name"].ToString();
                            pdf.Start = "";
                            pdf.sprice = 0;
                            pdf.Qty = Convert.ToInt32(itms["storeQty"]) > 0 ? Convert.ToInt32(itms["storeQty"]) : 0;
                            pdf.Price = Convert.ToDecimal(itms["priceperUnit"]);
                            fnf.Price = Convert.ToDecimal(itms["priceperUnit"]);
                            pdf.pack = 1;
                            fnf.pack = 1;
                            pdf.End = "";
                            pdf.deposit = "";
                            pdf.altupc5 = "";
                            pdf.altupc4 = "";
                            pdf.altupc3 = "";
                            pdf.altupc2 = "";
                            pdf.altupc1 = "";
                            fnf.region = "";
                            fnf.pcat2 = "";
                            fnf.region = "";
                            fnf.country = "";
                            fnf.pcat = itms["departmentName"].ToString();


                            if (DiffQty.Contains(storeid.ToString()))
                            {
                                if (pdf.Qty > 0)
                                {
                                    pf.Add(pdf);
                                    fullname.Add(fnf);

                                }
                            }
                            else if (pdf.Price > 0)
                            {
                                pf.Add(pdf);
                                fullname.Add(fnf);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("" + ex.Message);
                (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in RetailzAPI@" + storeid + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
            }
            pf = pf.GroupBy(p => p.sku)
                                     .Select(g => g.First())
                                     .ToList();
            fullname = fullname.GroupBy(p => p.sku)
                                     .Select(g => g.First())
                                     .ToList();


            GenerateCSV.GenerateCSVFile(pf, "PRODUCT", storeid, BaseUrl);
            GenerateCSV.GenerateCSVFile(fullname, "FullName", storeid, BaseUrl);
            Console.WriteLine("Product file generated for RetailZAPI " + storeid);
            Console.WriteLine("FullName file generated for RetailZAPI " + storeid);
        }
    }
}
