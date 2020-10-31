﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameConsuleWebStore.Data;
using GameConsuleWebStore.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore.Internal;

namespace GameConsuleWebStore.Controllers
{
    public class OrdersController : Controller
    {
        private readonly GameConsuleWebStoreContext _context;


        public OrdersController(GameConsuleWebStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ShowOrders(int id)
        {
            List<Order> result = new List<Order>();
            if(HttpContext.Session.GetString("UserId")!=null)
            {
                result = _context.Order.Where(p => p.User.Id == id).ToList();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
            return View(result);
        }



        // GET: Orders
        public async Task<IActionResult> Index()
        {
            return View(await _context.Order.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {


            if (id == null)
            {
                return NotFound();
            }

            //Taking the order with all includes -> ProductOrders+ Product + Items for Order
            var order = await _context.Order.Include(po => po.ProductOrders).Include(po => po.ItemsPerOrder).ThenInclude(o => o.Product).FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            //Taking the items per order for the view
            List<Item> ListOfItemsPerOrder = new List<Item>();
            foreach (Item t in order.ItemsPerOrder)
            {
                ListOfItemsPerOrder.Add(t);
            }
            ViewBag.ListOfItemsPerOrderID = ListOfItemsPerOrder;



            return View(order);
        }



        public IActionResult Create()
        {
            List<Item> itt = GameConsuleWebStore.Controllers.ShoppingCartController.cartTemp;
            List<SelectListItem> items = new List<SelectListItem>();

            ViewBag.itemsForOrder = itt;

            //Showing the prodcuts from the cart to the Order-Create-View
            foreach (Item it in itt)
            {
                items.Add(new SelectListItem { Text = it.Product.Name, Value = it.Product.ProductId.ToString(), Selected = true });
            }

            ViewBag.cartItemsData = items;
            ViewData["ProductId"] = items;


            return View();
        }



        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DateOrder")] Order order,int[] ProductId)
        {

            if (ModelState.IsValid)
            {
                //--------Connect The user to the order------
                //MUST LOGIN FIRST
                if(HttpContext.Session.GetString("UserId")!=null)
                {
                    order.User = _context.User.First(x => x.Id.ToString().Equals(HttpContext.Session.GetString("UserId")));
                }
                else
                {
                    return RedirectToAction("Login","Users");
                }
                //Adding the "many to many" function -> The OrderCntroller Creating a new Order with many products.
                order.DateOrder = DateTime.Now;
                order.ProductOrders = new List<ProductOrder>();
                foreach(var id in ProductId){
                    order.ProductOrders.Add(new ProductOrder() { ProductId = id, OrderId = order.Id });
                }

                _context.Add(order);
                await _context.SaveChangesAsync();

                List<Item> itt = GameConsuleWebStore.Controllers.ShoppingCartController.cartTemp;

                order.ItemsPerOrder = itt;
                await _context.SaveChangesAsync();

                //ERASE THE SESSION And Empty the static List cart
                GameConsuleWebStore.Controllers.ShoppingCartController.cartTemp = new List<Item>();
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("CartNumOfItems");



                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DateOrder")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
