using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace api.Controllers
{
    [Route("api/seatholders")]
    [ApiController]
    public class SeatholderController : ControllerBase
    {
        //In memory db, lazy init
        private List<Seatholder> seatholders = null;

        public SeatholderController()
        {
        }

        [HttpGet]
        public List<Seatholder> Get()
        {
            if(seatholders == null)
            {
                LoadSeatHolders();
            }

            return seatholders;
        }

        [Route(("{city}"))]
        public IActionResult GetAmountOfSeatholders(string city)
        {
            if(seatholders == null)
            {
                LoadSeatHolders();
            }

            //Is this an area code?
            try
            {
                int areaCode = Convert.ToInt32(city);
                int areaCodes = seatholders.Count(x => x.AreaCode == areaCode);
                if(areaCodes > 0)
                {
                    return Ok(areaCodes);
                }
            }
            catch {}
            

            //Then maybe it's the name of a city?
            int cities = seatholders.Count(x => x.City.ToLower() == city.ToLower());
            if(cities > 0)
            {
                return Ok(cities);
            }

            //Out of ideas at this point
            return NotFound();
        }

        private void LoadSeatHolders()
        {
            var raw = new List<RawSeatholder>();

            using (var reader = new StreamReader("assets/abonnees_genk.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                raw = csv.GetRecords<RawSeatholder>().ToList();
            }

            seatholders = new List<Seatholder>();
            foreach(var r in raw)
            {
                Seatholder seat = new Seatholder();
                seat.ConvertFromRaw(r);
                if(seat.AreaCode > 0)
                {
                    seatholders.Add(seat);
                }
            }
        }
    }
}
