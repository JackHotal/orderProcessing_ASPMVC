using System;
using System.Collections.Generic;
using System.Web.Mvc;


namespace OPMVCNET.Models
{
    public class Products
    {
        public string Cid { get; set; }

        public List<string> products { get; set; }

        public MultiSelectList productlist { get; set; }

      
    }
}