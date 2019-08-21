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
    internal class Time : Google.Apis.Util.IClock
    {
        public DateTime Now { get { return DateTime.Now; } }
        public DateTime UtcNow { get { return DateTime.UtcNow; } }
    }

    /// <summary>
    /// Class <c>HomeController</c> hereda de Controller gestiona el controlador del MVC.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// <c>res</c> almacena la autorización de google, de haber fallado será null.
        /// </summary>
        static private AuthResult res;

        /// <summary>
        /// De haber fallado la autentificación lo rediccionará a RedirectUri,
        /// en cualquier otro caso listará los próximos 10 eventos desde el
        /// horario del sistema actual.
        /// </summary>
        /// <param name="cancellationToken">Cancela la tarea asíncrona</param>
        /// <returns></returns>
        public async Task<ActionResult> IndexAsync(CancellationToken cancellationToken)
        {
            /**
            *   AuthorizationCodeFlow nos pide una instancia de controller
            *   y otra de FlowMetadata, usamos AppFlowMetaData.
            */
            res = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata())
                .AuthorizeAsync(cancellationToken);

            /** 
            *   Checkea si las credenciales son válidas (!null).
            */
            if (res.Credential == null)
            {
                /**
                *   Si las credenciales son no válidas redirecciona a RedirectUri.
                */
                return new RedirectResult(res.RedirectUri);
            } else
            {
                /**
                *   service almacena el servicio del calendario, este 
                *   requiere de las credenciales previamente checkeadas 
                *   y el nombre de la App (el mismo de https://console.developer.google.com).
                */
                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = res.Credential,
                    ApplicationName = "Albor"
                });
                
                /**
                 *  service contiene todo a lo que le dimos acceso al 
                 *  client (la App). Si se tiene acceso a lectura se podrá
                 *  listar el calendario que gustemos, en este caso "primary".
                 */
                var list = service.Events.List("primary");
                //  Establecemos desde que momento deseamos listar.
                list.TimeMin = DateTime.Now;
                //  No mostramos los items eliminados.
                list.ShowDeleted = false;
                //  Mostramos elementos únicos.
                list.SingleEvents = true;
                //  Establecemos el límite de la lista.
                list.MaxResults = 10;
                
                //  Ordenamos por fecha.
                list.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                //  Ejecutamos el listado.
                Events events = list.Execute();

                //  Enviamos los eventos a la vista.
                return View(events);
            }
        }

        /// <summary>
        /// Respuesta para peticiones GET a /Home/AddEvent.
        /// Retorna la vista que contiene el formulario
        /// </summary>
        public ActionResult AddEvent() => View();
        

        /// <summary>
        /// Sirve para añadir un evento a un calendario.
        /// </summary>
        /// <param name="Summary">Nombre del evento</param>
        /// <param name="Start">Cuándo empieza el evento</param>
        /// <param name="End">Cuándo finaliza el evento</param>
        /// <param name="Location">Dónde se realiza el evento</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEvent(
                string Summary, string Start, string End, string Location
            )
        {
            if (res == null)
                return RedirectToAction("IndexAsync");
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            service.Events.Insert(new Event
            {
                Summary = Summary,
                Created = DateTime.Now,
                Start = new EventDateTime { DateTimeRaw = Start },
                End = new EventDateTime { DateTimeRaw = End },
                Location = Location
            }, "primary").Execute();
            return RedirectToAction("IndexAsync");
        }
        public ActionResult DeleteEvent() => View();

        [HttpPost]
        public ActionResult DeleteEvent(string calendarId, string eventId)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            service.Events.Delete(calendarId, eventId).Execute();
            return RedirectToAction("IndexAsync");
        }

    }
}