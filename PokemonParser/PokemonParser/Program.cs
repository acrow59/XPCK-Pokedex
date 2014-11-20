using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;
using System.IO;

namespace PokemonParser
{
    class Program
    {
        static void Main(string[] args)
        {

            //var client = new WebClient();
            //client.DefaultRequestHeaders.Add("User-Agent", "Fake");

            //var results = client.DownloadString("http://bulbapedia.bulbagarden.net/wiki/List_of_Pok%C3%A9mon_by_National_Pok%C3%A9dex_number");
            var doc = new HtmlDocument();
            //doc.LoadHtml(results);

            StreamReader reader = new StreamReader(WebRequest.Create("http://bulbapedia.bulbagarden.net/wiki/List_of_Pok%C3%A9mon_by_National_Pok%C3%A9dex_number").GetResponse().GetResponseStream(), Encoding.UTF8); //put your encoding            
            doc.Load(reader);

            var h3s = from h3 in doc.DocumentNode.Descendants("h3") where h3.InnerText.Contains("Generation ") select h3;
            String errors = "";
            try
            {

                //Pass the filepath and filename to the StreamWriter Constructor
                //StreamWriter sw = new StreamWriter("C:\\Users\\Alec\\Documents\\LocalItemsFile.xml");
                //sw.Write("<items>");

                foreach (var h3 in h3s)
                {
                    StreamWriter sw = new StreamWriter("C:\\Users\\Alec\\Documents\\" + h3.InnerText + ".xml");
                    sw.Write("<items>");

                    Console.Write("\n" + h3.InnerText + "\n");
                    var table = h3.NextSibling;
                    while (table.Name != "table")
                    {
                        if (table.NextSibling == null)
                            break;

                        table = table.NextSibling;
                    }

                    var trs = from tr in table.Descendants("tr") select tr;


                    string prevpoke = "";
                    int pokeForm = 0;
                    foreach (var tr in trs)
                    {
                        var tds = from td in tr.Descendants("td") select td;

                        string title = null, subtitle = "", description = "", image = "", group = h3.InnerText;
                        int count = 0;
                        foreach (var td in tds)
                        {
                            if (count == 1)
                            {
                                subtitle = td.InnerText.Replace("\n", "").Trim();
                                Console.Write("Dex #: " + td.InnerText);
                            }
                            if (count == 3)
                            {
                                title = td.InnerText.Replace("\n", "").Trim();
                                if (subtitle == prevpoke) pokeForm++;
                                else pokeForm = 0;
                                prevpoke = subtitle;
                                Console.Write("Name: " + td.InnerText);

                                //Pulls data from Bulbapedia.
                                //var PokemonResults = client.DownloadString("http://bulbapedia.bulbagarden.net/wiki/" + title);
                                var PokeDoc = new HtmlDocument();
                                //PokeDoc.LoadHtml(PokemonResults);

                                StreamReader PokeReader = new StreamReader(WebRequest.Create("http://bulbapedia.bulbagarden.net/wiki/" + title).GetResponse().GetResponseStream(), Encoding.UTF8); //put your encoding            
                                PokeDoc.Load(PokeReader);

                                HtmlNodeCollection links = PokeDoc.DocumentNode.SelectNodes("//*[@href]");
                                foreach (HtmlNode link in links)
                                {

                                    if (link.Attributes["href"] != null && link.Attributes["href"].Value.StartsWith("/wiki/"))
                                        link.Attributes["href"].Value = "http://bulbapedia.bulbagarden.net" + link.Attributes["href"].Value;
                                }


                                try
                                {
                                    var info = PokeDoc.DocumentNode.SelectSingleNode("//div[@id='mw-content-text']");
                                    //description = info.OuterHtml;

                                    //Gets sidebar information
                                    var roundy = PokeDoc.DocumentNode.SelectSingleNode("//table[@class='roundy']");
                                    description = roundy.OuterHtml;

                                    //Gets short description
                                    var bio = roundy.NextSibling.NextSibling;
                                    description += "<font color=\"white\">";
                                    while (bio.Name == "p")
                                    {
                                        description += bio.OuterHtml;
                                        bio = bio.NextSibling;
                                    }
                                    description += "</font>";

                                    try
                                    {
                                        //Gets Base Stats
                                        //Since some Pokemon have different forms, the webpage is setup differently preventing a top-down approach. Instead, the code starts inside the table and works its way out.
                                        var baseID = PokeDoc.DocumentNode.SelectSingleNode("//span[@id='Stats']").ParentNode;
                                        var statsTable = baseID.NextSibling;
                                        while (statsTable.Name != "table")
                                        {
                                            if (statsTable.NextSibling == null)
                                                break;

                                            statsTable = statsTable.NextSibling;

                                            try
                                            {
                                                if (statsTable.HasAttributes && statsTable.Attributes["class"].Value == "collapsible")
                                                {
                                                    statsTable = statsTable.SelectNodes("//table[@align='left']")[pokeForm];
                                                }
                                            }
                                            catch { }
                                        }
                                        statsTable.Attributes["align"].Value = "center";
                                        description += statsTable.OuterHtml;
                                    }
                                    catch (Exception e)
                                    {
                                        errors += title + ":base stats, ";
                                    }

                                    try
                                    {
                                        //Gets Move Learnset
                                        var multiforms = PokeDoc.DocumentNode.SelectNodes("//table[@class='collapsible']//table[@class='roundy']");
                                        var learnset = PokeDoc.DocumentNode.SelectSingleNode("//span[@id='Learnset']").ParentNode;
                                        var learnsetTable = learnset.NextSibling;
                                        if (multiforms == null)
                                        {
                                            while (learnsetTable.Name != "table")
                                            {
                                                if (learnsetTable.NextSibling == null)
                                                    break;
                                                learnsetTable = learnsetTable.NextSibling;

                                            }
                                        }
                                        else
                                        {
                                            learnsetTable = multiforms[pokeForm * 2];
                                        }
                                        learnsetTable.Attributes["style"].Value = learnsetTable.Attributes["style"].Value + " margin-top: 15px;";
                                        description += learnsetTable.OuterHtml;

                                        //var img = info.SelectSingleNode("//img[@alt='" + title + "']");
                                    }
                                    catch
                                    {
                                        errors += title + ":moves, ";
                                    }
                                    try
                                    {
                                        //var img = roundy.SelectNodes("//table[@class='roundy']//img[contains(@alt,'" + WebUtility.HtmlEncode(title) + "')]")[pokeForm];
                                        var imgs = roundy.SelectNodes("//table[@class='roundy']//img[contains(@alt,'" + WebUtility.HtmlEncode(title) + "') or contains(@alt,'" + title.Replace("'", "") + "')]");
                                        if (imgs.Count > 1)
                                        {
                                            if (subtitle != "#351")
                                                image = imgs[pokeForm].GetAttributeValue("src", "").Replace("110px", "250px");
                                            else
                                                image = imgs[pokeForm].GetAttributeValue("src", "");
                                        }
                                        else
                                            image = imgs[0].GetAttributeValue("src", "");
                                    }
                                    catch (Exception e)
                                    {
                                        errors += title + ":image, ";
                                    }
                                }
                                catch (Exception e)
                                {
                                    errors += title + ":bio, ";
                                }

                            }

                            count++;
                        }


                        if (title != null)
                        {
                            sw.Write("<item>\n" +
                                "<title>" + title + "</title>" +
                                "<subtitle>" + subtitle + "</subtitle>" +
                                "<description><![CDATA[" + description + "]]></description>" +
                                "<image>" + image + "</image>" +
                                "<group>" + h3.InnerText + "</group>" +
                            "</item>");
                        }
                    }

                    sw.Write("</items>");
                    //Close the file
                    sw.Close();
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }


            Console.Write("Had errors pulling data for " + errors);
            int i = 0;
        }

    }
}
