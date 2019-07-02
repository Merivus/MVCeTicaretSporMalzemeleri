using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVCeTicaret.Models;

namespace MVCeTicaret.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        public ActionResult Index()
        {
            return View();
        }

        Context db;
        public ProductController()
        {
            db = new Context();
        }

        public ActionResult Product(int id)
        {
            return View(db.Products.Where(x => x.SubCategoryID == id).ToList());
        }

        public ActionResult ProductDetail(int id)
        {
            ViewData["Reviews"] = db.Reviews.Where(x => x.ProductID == id && x.IsDeleted == false).ToList();
            return View(db.Products.Find(id));
        }

        [HttpPost]
        public ActionResult AddReview(int id, FormCollection frm)
        {
            Review review = new Review()
            {
                Comment = frm["review"],
                CustomerID = TemporaryUserData.UserID,
                DateTime = DateTime.Now,
                ProductID = id,
                Name = frm["name"] == "" ? "ABO!" : frm["name"],
                Rate = int.Parse(frm["rate"])
            };

            db.Reviews.Add(review);
            db.SaveChanges();

            return RedirectToAction("ProductDetail", new { id = id });
        }        
    }
}