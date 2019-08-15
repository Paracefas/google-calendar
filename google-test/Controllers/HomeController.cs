using Google.Apis.Auth.OAuth2.Mvc;
using google_test.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Calendar.v3.Data;
using System.Web.Routing;
using static Google.Apis.Auth.OAuth2.Web.AuthorizationCodeWebApp;
using WebMatrix.WebData;

namespace google_test.Controllers
{
    public class HomeController : Controller
    {
        private AuthResult res; 
        public async Task<ActionResult> IndexAsync(CancellationToken cancellationToken)
        {
            res = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata())
                .AuthorizeAsync(cancellationToken);
            if (res.Credential == null)
            {
                return new RedirectResult(res.RedirectUri);
            } else
            {
                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = res.Credential,
                    ApplicationName = "Albor"
                });
                var list = service.Events.List("primary");
                list.TimeMin = DateTime.Now;
                list.ShowDeleted = false;
                list.SingleEvents = true;
                list.MaxResults = 10;
                list.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Events events = list.Execute();
                
                return View(events);
            }
        }
        public ActionResult LogOut()
        {
            WebSecurity.Logout();
            Session.Remove("user");
            return RedirectToAction("IndexAsync");
        }

    }
}