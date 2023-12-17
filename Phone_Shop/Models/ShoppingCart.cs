﻿using Microsoft.AspNetCore.Mvc;
using Phone_Shop.Data;
using System.Security.Cryptography;

namespace Phone_Shop.Models
{
    public class ShoppingCart
    {
        private readonly ApplicationDbContext _db;
        public string ShoppingCartId { get; set; }

        public const string CartSessionKey = "CartId";
        public ShoppingCart(ApplicationDbContext db)
        {
            _db = db;
        }
        public static ShoppingCart GetCart(HttpContext context, ApplicationDbContext db)
        {
            var cart = new ShoppingCart(db);
            cart.ShoppingCartId = cart.GetCartId(context);
            return cart;
        }
        public static ShoppingCart GetCart(Controller controller, ApplicationDbContext db)
        {
            return GetCart(controller.HttpContext, db);
        }
        public string GetCartId(HttpContext context)
        {
            if (context.Session.GetString(CartSessionKey) == null)
            {
                if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                {
                    context.Session.SetString(CartSessionKey, context.User.Identity.Name);
                }
                else
                {
                    Guid tempCartId = Guid.NewGuid();
                    context.Session.SetString(CartSessionKey, tempCartId.ToString());
                }
            }
            else
            {
                if(!string.IsNullOrWhiteSpace(context.User.Identity.Name)&&
                    context.Session.GetString(CartSessionKey)!= context.User.Identity.Name)
                {
                    MigrateCart(context.User.Identity.Name);
                    context.Session.SetString(CartSessionKey, context.User.Identity.Name);
                    ShoppingCartId = context.Session.GetString(CartSessionKey);
                }

            }
            return context.Session.GetString(CartSessionKey);
        }
        public void AddToCart(Product product)
        {
            var cartItem = _db.ShoppingCartItems.SingleOrDefault(
                c => c.CartId == ShoppingCartId
                && c.ProductId == product.Id);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = product.Id,
                    CartId = ShoppingCartId,
                    Quantity = 1,
                    DateCreated = DateTime.Now
                };
                _db.ShoppingCartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity++;
            }
            _db.SaveChanges();
        }
        public void EmptyCart()
        {
            var cartItems = _db.ShoppingCartItems.Where(
                cart => cart.CartId == ShoppingCartId);

            foreach (var cartItem in cartItems)
            {
                _db.ShoppingCartItems.Remove(cartItem);
            }
            _db.SaveChanges();
        }
        public int GetCount()
        {
            int? count = (from cartItems in _db.ShoppingCartItems
                          where cartItems.CartId == ShoppingCartId
                          select (int?)cartItems.Quantity).Sum();
            return count ?? 0;
        }
        public decimal GetTotal()
        {
            decimal? total = (from cartItems in _db.ShoppingCartItems
                              where cartItems.CartId == ShoppingCartId
                              select (int?)cartItems.Quantity *
                              cartItems.Product.Price).Sum();

            return total ?? decimal.Zero;
        }
        public int CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = GetCartItems();
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductID = item.ProductId,
                    OrderID = order.Id,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity
                };
                orderTotal += (item.Quantity * item.Product.Price);

                _db.OrderItem.Add(orderItem);

            }
            order.TotalPrice = orderTotal;

            _db.SaveChanges();
            EmptyCart();
            return order.Id;
        }
        public void MigrateCart(string userName)
        {
            var shoppingCart = _db.ShoppingCartItems.Where(
                c => c.CartId == ShoppingCartId);

            foreach (CartItem item in shoppingCart)
            {
                item.CartId = userName;
            }
            _db.SaveChanges();
        }
        public List<CartItem> GetCartItems()
        {
            List<CartItem> CartItems = _db.ShoppingCartItems.Where(
                cart => cart.CartId == ShoppingCartId).ToList();
            foreach (var item in CartItems)
            {
                item.Product = _db.Product.Single(x => x.Id == item.ProductId);
            }
            return CartItems;
        }
    }
}
