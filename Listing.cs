using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace EbayListings
{
    public class Listing
    {
        public DateTime EndTime { get; private set; }
        public Double Price { get; private set; }
        public string Postage { get; private set; }
        public string Url { get; private set; }
        public Boolean Status { get; private set; }
        public DateTime LastChecked { get; private set; }
        public string Name { get; private set; }
        public Boolean Use { get; private set; }
        public Boolean endingSoon {get; private set;}
        public Boolean changedPrice { get; set; }

        public Listing(string url)
        {
            this.Url = url;
            this.CreateListing(this);
        }

        [Newtonsoft.Json.JsonConstructor]
        public Listing(string endTime, double price, string postage, string url, bool status, string lastChecked, string name)
        {
            this.Url = url;
            this.Price = price;
            this.Postage = postage;
            this.Status = status;
            this.LastChecked = DateTime.Parse(lastChecked);
            this.Name = name;
            this.EndTime = DateTime.Parse(endTime);
            this.changedPrice = false;
        }

        private void CreateListing(Listing l)
        {
            this.Use = true;
            string html = GetHtmlString(l.Url);

            this.LastChecked = DateTime.Now;

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            string listingName = htmlDocument.DocumentNode.SelectNodes("//h1")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("x-item-title__mainTitle")).ToList()[0].InnerText;

            this.Name = listingName;

            try
            {
                string siteStatus = htmlDocument.DocumentNode.SelectNodes("//img")
                    .Where(node => node.GetAttributeValue("alt", "")
                    .Equals("Item Ended")).ToList()[0].InnerText;
                this.Status = false;
                this.Postage = "Not Needed";
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                this.Status = true;
            }

            if (this.Status)
            {
                string BidPrice = htmlDocument.DocumentNode.SelectNodes("//span")
                   .Where(node => node.GetAttributeValue("id", "")
                   .Equals("prcIsum_bidPrice")).ToList()[0].InnerText;
                BidPrice = BidPrice.Substring(1);
                l.Price = Convert.ToDouble(BidPrice);

                string Postage = htmlDocument.DocumentNode.SelectNodes("//div")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("vim d-shipping-minview")).ToList()[0].InnerText;

                if (Regex.IsMatch(Postage, "Free collection in person"))
                {
                    Postage = "Collection";
                }
                else if (Regex.IsMatch(Postage, "Free"))
                {
                    Postage = "Free Delivery";
                }
                else if (Regex.IsMatch(Postage, "£[0-9]+[.][0-9]{2}"))
                {
                    Postage = Regex.Matches(Postage, "£[0-9]+[.][0-9]{2}")[0].Value.Substring(1);
                }
                l.Postage = Postage;

                string EndTime = htmlDocument.DocumentNode.SelectNodes("//span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("vi-tm-left")).ToList()[0].InnerText;

                MatchCollection Date = Regex.Matches(EndTime.ToLower(), "[0-9]{2} [a-z]{3}, [0-9]{4}");
                MatchCollection Time = Regex.Matches(EndTime.ToLower(), "[0-9]{2}:[0-9]{2}:[0-9]{2}");

                string DateAndTime = Date[0].Value + " " + Time[0].Value;
                DateTime Datum = DateTime.Parse(DateAndTime);
                TimeSpan remainingTime = Datum.Subtract(DateTime.Now);
                if (remainingTime <= TimeSpan.FromMinutes(40)) {
                    this.endingSoon = true;
                }
                l.EndTime = Datum;
            }
            else
            {
                string BidPrice = htmlDocument.DocumentNode.SelectNodes("//span")
                   .Where(node => node.GetAttributeValue("class", "")
                   .Equals("notranslate vi-VR-cvipPrice")).ToList()[0].InnerText;
                BidPrice = BidPrice.Substring(1);
                l.Price = Convert.ToDouble(BidPrice);
            }

            this.Use = false;
        }

        public void UpdateListing()
        {
            this.Use = true;
            string html = GetHtmlString(this.Url);

            this.LastChecked = DateTime.Now;

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            try
            {
                string siteStatus = htmlDocument.DocumentNode.SelectNodes("//img")
                    .Where(node => node.GetAttributeValue("alt", "")
                    .Equals("Item Ended")).ToList()[0].InnerText;
                this.Status = false;
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                this.Status = true;
            }

            if (this.Status)
            {
                string BidPrice = htmlDocument.DocumentNode.SelectNodes("//span")
                   .Where(node => node.GetAttributeValue("id", "")
                   .Equals("prcIsum_bidPrice")).ToList()[0].InnerText;
                BidPrice = BidPrice.Substring(1);
                if (this.Price != Convert.ToDouble(BidPrice)) {
                    this.changedPrice = true;
                }
                this.Price = Convert.ToDouble(BidPrice);

                string EndTime = htmlDocument.DocumentNode.SelectNodes("//span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("vi-tm-left")).ToList()[0].InnerText;

                MatchCollection Date = Regex.Matches(EndTime.ToLower(), "[0-9]{2} [a-z]{3}, [0-9]{4}");
                MatchCollection Time = Regex.Matches(EndTime.ToLower(), "[0-9]{2}:[0-9]{2}:[0-9]{2}");

                string DateAndTime = Date[0].Value + " " + Time[0].Value;
                DateTime Datum = DateTime.Parse(DateAndTime);
                TimeSpan remainingTime = Datum.Subtract(DateTime.Now);
                if (remainingTime <= TimeSpan.FromMinutes(40)) {
                    this.endingSoon = true;
                }
                this.EndTime = Datum;
            }
            else
            {
                string BidPrice = htmlDocument.DocumentNode.SelectNodes("//span")
                   .Where(node => node.GetAttributeValue("class", "")
                   .Equals("notranslate vi-VR-cvipPrice")).ToList()[0].InnerText;
                BidPrice = BidPrice.Substring(1);
                if (this.Price != Convert.ToDouble(BidPrice)) {
                    this.changedPrice = true;
                }
                this.Price = Convert.ToDouble(BidPrice);
            }

            this.Use = false;
        }

        private String GetHtmlString(string url)
        {
            String HtmlString = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                HtmlString = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }
            return HtmlString;
        }

    }
}
