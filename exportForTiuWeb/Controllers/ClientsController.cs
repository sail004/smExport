using System;
using System.Linq;
using System.Web.Mvc;
using exportForTiuWeb.Models;
using TiuLib.Model;

namespace exportForTiuWeb.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ExportForTiuWebContext db = new ExportForTiuWebContext();

        // GET: Clients
        public JsonResult Index()
        {
            var client =
                db.Clients.FirstOrDefault(r => r.IpAddress == Request.UserHostAddress.ToString());

            if (client == null)
            {
                client = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = (db.Clients.Count() + 1).ToString(),
                    IpAddress = Request.UserHostAddress,
                    Permit = true
                };
                db.Clients.Add(client);
            }
            db.EventLogs.Add(new EventLog
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = client.Id,
                LogMessage = "Request client",
                TimeStamp = DateTime.Now
            });
            db.SaveChanges();
            return Json(client, JsonRequestBehavior.AllowGet);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}