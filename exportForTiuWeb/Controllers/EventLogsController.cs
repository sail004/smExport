using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using exportForTiuWeb.Models;
using TiuLib.Model;

namespace exportForTiuWeb.Controllers
{
    public class EventLogsController : Controller
    {
        private readonly ExportForTiuWebContext db = new ExportForTiuWebContext();

        // POST: EventLogs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public string Create(string clientId,string logMessage)
        {
            var eventLog = new EventLog
            {
                Id = Guid.NewGuid().ToString(),
                TimeStamp = DateTime.Now,
                ClientId = clientId,
                LogMessage = logMessage
            };
            db.EventLogs.Add(eventLog);
            db.SaveChanges();
            return "Ok";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
