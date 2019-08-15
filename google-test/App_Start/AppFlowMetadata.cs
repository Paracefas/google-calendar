using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;

namespace google_test.App_Start
{
    public class AppFlowMetadata : FlowMetadata
    {
        private static readonly IAuthorizationCodeFlow flow =
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = "984367389979-il05jae586af16e1p6lahkaelfjc7fcl.apps.googleusercontent.com",
                        ClientSecret = "OmyAwqHVWDwliAfOrfR_SpW-"
                    },
                    Scopes = new [] { CalendarService.Scope.Calendar },
                    DataStore = new FileDataStore("Calendar.Api.Auth.Store")
                });
        public override string GetUserId(Controller controller)
        {
            var user = controller.Session["user"];
            if(user == null)
            {
                user = Guid.NewGuid();
                controller.Session["user"] = user;
            }
            return user.ToString();
        }
        public override IAuthorizationCodeFlow Flow 
        {
            get { return flow; }
        }
    }
}