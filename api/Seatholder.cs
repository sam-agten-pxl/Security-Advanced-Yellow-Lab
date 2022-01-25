using System;
using CsvHelper.Configuration.Attributes;

namespace api
{
    public class Seatholder 
    {
        public int AreaCode {get;set;}
        public string BuyerType {get;set;}
        public string City {get;set;}
        public string Section {get;set;}

        public void ConvertFromRaw(RawSeatholder raw)
        {
            try
            {
                AreaCode = Convert.ToInt32(raw.AreaCode);
            }
            catch(Exception)
            {
                AreaCode = 0;
            }

            this.BuyerType = raw.BuyerType;
            this.City = raw.City;
            this.Section = raw.Section;
        }
    }

    public class RawSeatholder
    {
        [Name("Postcode")]
        public string AreaCode {get;set;}

        [Name("Buyertype")]
        public string BuyerType {get;set;}

        [Name("City")]
        public string City {get;set;}

        [Name("Section")]
        public string Section {get;set;}
    }
}