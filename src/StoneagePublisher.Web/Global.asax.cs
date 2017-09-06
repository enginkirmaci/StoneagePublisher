using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using log4net;

namespace StoneagePublisher.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private ILog logger;
        private ILog Logger => logger ?? (logger = LogManager.GetLogger(GetType()));

        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();
            Logger.Info("******************** APPLICATION START *****************");
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

        }
    }
}
