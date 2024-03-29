﻿using Google.Apis.Auth.OAuth2.Mvc;
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
    class OptionalMonad<T>
    {
        private T value;
        public T Value { get
            {
                return Exists ? value : throw new InvalidOperationException();
            } }
        public bool Exists { get; private set; }
        public OptionalMonad(T value)
        {
            this.value = value;
            Exists = true;
        }
        public OptionalMonad()
        {
            Exists = false;
        }
        public static explicit operator T(OptionalMonad<T> right)
        {
            return right.Value;
        }
        public static implicit operator OptionalMonad<T>(T right)
        {
            return new OptionalMonad<T>(right);
        }
        public override bool Equals(object obj)
        {
            if(obj is OptionalMonad<T>)
                return Equals((OptionalMonad<T>)obj);
            return false;
        }
        public bool Equals(OptionalMonad<T> right)
        {
            return Exists && right.Exists ? 
                Equals(value, right.value) : false;
        }
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
                 *  service contiene todo a lo que le dimos acceso al 
                 *  client (la App). Si se tiene acceso a lectura se podrá
                 *  listar el calendario que gustemos, en este caso "primary".
                 */
                var service = new CalendarService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = res.Credential,
                        ApplicationName = "Albor"
                    });
                var calendars = service.CalendarList.List().Execute();
                List<SelectListItem> list = new List<SelectListItem>();
                
                return View(calendars);
            }
        }
        public ActionResult RenderCalendar(string Calendar = "primary")
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            var list = service.Events.List(Calendar);
            //  Establecemos desde que momento deseamos listar.
            list.TimeMin = DateTime.Now;
            //  No mostramos los items eliminados.
            list.ShowDeleted = false;
            //  Mostramos elementos únicos.
            list.SingleEvents = true;
            //  Establecemos el límite de la lista.
            list.MaxResults = 5;
            list.TimeZone = "GMT-03:00";

            //  Ordenamos por fecha.
            list.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            //  Ejecutamos el listado.
            Events events = list.Execute();
            return PartialView(events);
        }
        /// <summary>
        /// Respuesta para peticiones GET a /Home/AddEvent.
        /// Retorna la vista que contiene el formulario
        /// </summary>
        public ActionResult AddEvent() => View();
        /// <summary>
        /// Sirve para añadir un evento a un calendario.
        /// </summary>
        /// <param name="Calendar">Nombre del evento</param>
        /// <param name="Summary">Nombre del evento</param>
        /// <param name="Start">Cuándo empieza el evento</param>
        /// <param name="End">Cuándo finaliza el evento</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEvent(string Calendar, string Summary, string Start, string End)
        {
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
                //Location = Location
            }, (string)findCalendarBySummary(Calendar)).Execute();
            return RedirectToAction("IndexAsync");
        }
        public ActionResult DeleteEvent() => View();
        [HttpPost]
        public ActionResult DeleteEvent(string calendarSummary, string eventSummary)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            string calendarId = (string)findCalendarBySummary(calendarSummary);
            service.Events.Delete(calendarId, (string)findEventBySummary(calendarId, eventSummary)).Execute();
            return RedirectToAction("IndexAsync");
        }
        public ActionResult AddCalendar() => View();
        [HttpPost]
        public ActionResult AddCalendar(string Summary, string Description, string Location)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            service.Calendars.Insert(new Calendar
            {
                Summary = Summary,
                Description = Description ?? "",
                TimeZone = "GMT-03:00"
            }).Execute();
            return RedirectToAction("IndexAsync");
        }
        public ActionResult DeleteCalendar() => View();
        [HttpPost]
        public ActionResult DeleteCalendar(string calendarSummary)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            service.Calendars.Delete((string)findCalendarBySummary(calendarSummary)).Execute();
            return RedirectToAction("IndexAsync");
        }
        private OptionalMonad<string> findEventBySummary(string CalendarSummary, string Summary)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            foreach (var item in service.Events.List(CalendarSummary).Execute().Items)
                if (item.Summary == Summary) return item.Id;
            return new OptionalMonad<string>();
        }
        private OptionalMonad<string> findCalendarBySummary(string Summary)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = res.Credential,
                ApplicationName = "Albor"
            });
            foreach (var item in service.CalendarList.List().Execute().Items)
                if (item.Summary == Summary) return item.Id;
            return new OptionalMonad<string>();
        }
    }
}