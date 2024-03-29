﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVCeTicaret.Models;

namespace MVCeTicaret.Controllers
{
    public class ShoppingController : Controller
    {
        Context db;
        public ShoppingController()
        {
            db = new Context();
        }

        public ActionResult Cart()
        {
            return View(db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList());
        }

        public ActionResult RemoveFromCart(int id)
        {
            db.OrderDetails.Remove(db.OrderDetails.Find(id));
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult AddToWishListFromCart(int id)
        {
            int productID = db.OrderDetails.Find(id).ProductID;
            ControlWishlist(productID);

            db.OrderDetails.Remove(db.OrderDetails.Find(id));
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        private void ControlWishlist(int id)
        {
            Wishlist wishlist = db.Wishlists.FirstOrDefault(x => x.ProductID == id && x.CustomerID == TemporaryUserData.UserID && x.IsActive == true);

            if (wishlist == null)
            {
                wishlist = new Wishlist();
                wishlist.ProductID = id;
                wishlist.CustomerID = TemporaryUserData.UserID; // TODO: Dinamik hale getirilecek!..
                wishlist.IsActive = true;

                db.Wishlists.Add(wishlist);
                db.SaveChanges();
            }
        }

        public ActionResult GoToPayment()
        {
            List<OrderDetail> cart = db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList();

            foreach (var item in cart)
            {
                if (item.Product.UnitsInStock < item.Quantity)
                    return RedirectToAction("Cart", "Shopping");
            }

            ViewBag.OrderDetails = cart;
            ViewBag.PaymentTypes = db.PaymentTypes.ToList();

            return View(db.Customers.Find(TemporaryUserData.UserID));
        }

        [HttpPost]
        public ActionResult CompleteShopping(FormCollection frm)
        {
            Payment payment = new Payment()
            {
                Type = int.Parse(frm["paymentType"]),
                Balance = 20000,
                CreditAmount = 20000,
                PaymentDateTime = DateTime.Now
            };

            db.Payments.Add(payment);

            if (frm["update"] == "on") 
            {
                Customer customer = db.Customers.Find(TemporaryUserData.UserID);

                customer.FirstName = frm["FirstName"];
                customer.LastName = frm["LastName"];
                customer.Address = frm["Address"];
                customer.City = frm["City"];
            }

            ShippingDetail sp = new ShippingDetail()
            {
                FirstName = frm["FirstName"],
                LastName = frm["LastName"],
                Address = frm["Address"],
                City = frm["City"]
            };

            db.ShippingDetails.Add(sp);
            db.SaveChanges();

            List<OrderDetail> cart = db.OrderDetails.Where(x => x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).ToList();

            foreach (var item in cart)
            {
                item.IsCompleted = true;
                item.Product.UnitsInStock -= item.Quantity;

                Order order = new Order()
                {
                    PaymentID = payment.PaymentID,
                    ShippingID = sp.ShippingID,
                    OrderDetailID = item.OrderDetailID,
                    Discount = item.Discount,
                    TotalAmount = item.TotalAmount,
                    IsCompleted = true,
                    OrderDate = DateTime.Now,
                    Dispatched = false,
                    DispatchDate = DateTime.Now.AddDays(3),
                    Shipped = false,
                    ShippedDate = DateTime.Now.AddDays(4),
                    Deliver = false,
                    DeliveryDate = DateTime.Now.AddDays(5),
                    CancelOrder = false
                };

                db.Orders.Add(order);
            }
            db.SaveChanges();
            return RedirectToAction("FinishShopping","Shopping");
        }

        public ActionResult FinishShopping()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddToCart(int id, FormCollection frm)
        {

            if (Session["OnlineKullanici"] == null)
                return RedirectToAction("Login", "Login");

            int miktar = Convert.ToInt32(frm["quantity"]);
            // TODO: Bu action çalışır hale getirilecek... 
            ControlCart(id, miktar);


            return RedirectToAction("ProductDetail", "Product", new { id = id });
        }

        public ActionResult AddToWishList(int id)
        {
            if (Session["OnlineKullanici"] == null)
                return RedirectToAction("Login", "Login");

            ControlWishlist(id);

            return RedirectToAction("ProductDetail", "Product", new { id = id });
        }

        public ActionResult WishList()
        {
            return View(db.Wishlists.Where(x => x.CustomerID == TemporaryUserData.UserID && x.IsActive == true).ToList());
        }

        public ActionResult RemoveFromWishList(int id)
        {
            Wishlist wishlist = db.Wishlists.Find(id);
            wishlist.IsActive = false;
            db.SaveChanges();

            return RedirectToAction("Wishlist", "Shopping");
        }

        public ActionResult AddToCartFromWishList(int id)
        {
            int productId = db.Wishlists.Find(id).ProductID;
            ControlCart(productId);

            Wishlist wishlist = db.Wishlists.Find(id);
            wishlist.IsActive = false;
            db.SaveChanges();

            return RedirectToAction("WishList", "Shopping");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, FormCollection frm)
        {
            OrderDetail od = db.OrderDetails.Find(id);
            od.Quantity = int.Parse(frm["quantity"]);
            od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);

            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public void ControlCart(int id, int miktar = 1)
        {
            OrderDetail od = db.OrderDetails.Where(x => x.ProductID == id && x.IsCompleted == false && x.CustomerID == TemporaryUserData.UserID).FirstOrDefault();

            if (od == null) //yeni kayıt
            {
                od = new OrderDetail();
                od.ProductID = id;
                od.CustomerID = TemporaryUserData.UserID;
                od.IsCompleted = false;
                od.UnitPrice = db.Products.Find(id).UnitPrice;
                od.Discount = db.Products.Find(id).Discount;
                od.OrderDate = DateTime.Now;

                if (db.Products.Find(id).UnitsInStock >= miktar)
                    od.Quantity = miktar;
                else
                    od.Quantity = db.Products.Find(id).UnitsInStock;

                od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);
                db.OrderDetails.Add(od);

            }

            else // update
            {
                if (db.Products.Find(id).UnitsInStock > od.Quantity + miktar)
                {
                    od.Quantity += miktar;
                    od.TotalAmount = od.Quantity * od.UnitPrice * (1 - od.Discount);
                }
            }

            db.SaveChanges();
        }
    }
}