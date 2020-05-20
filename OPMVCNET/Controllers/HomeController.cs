using System.Collections.Generic;
using System.Web.Mvc;
using opdblib_ado;
using OPMVCNET.Models;

namespace OPMVCNET.Controllers
{
    public class HomeController : Controller
    {
        private static Order db = new Order("ism6236", "ism6236bo");

        //T
        public ViewResult Index()
        {
            return View("CustomerInputView");
        }
        [Route("~/GetCustomerData")]
        public ViewResult GetCustomerData(Customer c)
        {
            string cid = c.Id;
            string name = db.getCustomer(cid);
            c.Name = name;
            //Customer newc = new Customer { Id = cid, Name = name };

            List<string> oids = db.getCustomerOrders(cid);
            List<string> cods = new List<string>();
            foreach (string s in oids)
            {
                List<string> ods = db.getOrderDetails(s);
                cods.AddRange(ods);
            }

            
            ViewBag.Orders= cods;
            ViewBag.Customer= c;



            return View("CustomerOrderView",c);
        }

        [Route("~/GetProductView")]
        public ViewResult GetProductView(Customer c)
        {
            
            List<string> pids = db.getProductIds();
            List<string> pods = new List<string>();
            Products p = new Products();
            p.Cid = c.Id;
            foreach (string s in pids)
            {
                pods.Add(string.Format("{0},{1}",s,db.getProductDetail(s)));
            }

           p.products = pods; // new List<SelectListItem>();
            p.productlist = new MultiSelectList(p.products, null);
    
            return View("ProductView",p);
        }

        [Route("~/Purchase")]
        public ViewResult Purchase(Products p)
        {
            
            List<string> od = p.products;
            List<string> odq = new List<string>();

            //The last item in each string is "onhand". I need to replcae it with 1 (order quantity)
            foreach (string s in od)
            {
                int j = s.LastIndexOf(',');
                string x = s.Substring(0, j - 1);
                odq.Add(string.Format("{0},{1}", x, 1));
            }
            int n = db.Purchase(p.Cid, odq);
            return View("CustomerInputView");
        }
        
    }
}