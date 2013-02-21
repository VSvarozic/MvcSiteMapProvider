﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Routing;
using MvcSiteMapProvider.Web.Mvc;
using MvcSiteMapProvider;
using MvcSiteMapProvider.Web;
using MvcSiteMapProvider.Reflection;

namespace MvcSiteMapProvider.Security
{
    /// <summary>
    /// AuthorizeAttributeAclModule class
    /// </summary>
    public class AuthorizeAttributeAclModule
        : IAclModule
    {
        public AuthorizeAttributeAclModule(
            IHttpContextFactory httpContextFactory,
            IControllerTypeResolver controllerTypeResolver,
            IObjectCopier objectCopier
            )
        {
            if (httpContextFactory == null)
                throw new ArgumentNullException("httpContextFactory");
            if (controllerTypeResolver == null)
                throw new ArgumentNullException("controllerTypeResolver");
            if (objectCopier == null)
                throw new ArgumentNullException("objectCopier");

            this.httpContextFactory = httpContextFactory;
            this.controllerTypeResolver = controllerTypeResolver;
            this.objectCopier = objectCopier;
        }

        protected readonly IHttpContextFactory httpContextFactory;
        protected readonly IControllerTypeResolver controllerTypeResolver;
        protected readonly IObjectCopier objectCopier;

        #region IAclModule Members

        /// <summary>
        /// Determines whether node is accessible to user.
        /// </summary>
        /// <param name="siteMap">The site map.</param>
        /// <param name="node">The node.</param>
        /// <returns>
        /// 	<c>true</c> if accessible to user; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAccessibleToUser(ISiteMap siteMap, ISiteMapNode node)
        {
            // Is security trimming enabled?
            if (!siteMap.SecurityTrimmingEnabled)
            {
                return true;
            }

            // Is it an external node?
            var nodeUrl = node.Url;
            if (node.HasAbsoluteUrl())
            {
                return true;
            }

            // Clickable? Always accessible.
            if (node.Clickable == false)
            {
                return true;
            }

            // Time to delve into the AuthorizeAttribute defined on the node.
            // Let's start by getting all metadata for the controller...
            var controllerType = controllerTypeResolver.ResolveControllerType(node.Area, node.Controller);
            if (controllerType == null)
            {
                return false;
            }

            // Find routes for the sitemap node's url
            HttpContextBase httpContext = httpContextFactory.Create();
            string originalPath = httpContext.Request.Path;
            var originalRoutes = RouteTable.Routes.GetRouteData(httpContext);
            httpContext.RewritePath(nodeUrl, true);

            RouteData routes = node.GetRouteData(httpContext);
            if (routes == null)
            {
                return true; // Static URL's will have no route data, therefore return true.
            }
            foreach (var routeValue in node.RouteValues)
            {
                routes.Values[routeValue.Key] = routeValue.Value;
            }
            if (originalRoutes != null && (!routes.Route.Equals(originalRoutes.Route) || originalPath != nodeUrl || node.Area == String.Empty))
            {
                routes.DataTokens.Remove("area");
                //routes.DataTokens.Remove("Namespaces");
                //routes.Values.Remove("area");
            }
            //var requestContext = httpContextFactory.CreateRequestContext(routes);

            //// Create controller context
            //var controllerContext = new ControllerContext();
            //controllerContext.RequestContext = requestContext;

            //// Whether controller is built by the ControllerFactory (or otherwise by Activator)
            //bool factoryBuiltController = false;
            //try
            //{
            //    string controllerName = requestContext.RouteData.GetRequiredString("controller");
            //    controllerContext.Controller = ControllerBuilder.Current.GetControllerFactory().CreateController(requestContext, controllerName) as ControllerBase;
            //    factoryBuiltController = true;
            //}
            //catch
            //{
            //    try
            //    {
            //        controllerContext.Controller = Activator.CreateInstance(controllerType) as ControllerBase;
            //    }
            //    catch
            //    {
            //    }
            //}

            bool factoryBuiltController = false;
            var controllerContext = this.CreateControllerContext(routes, controllerType, out factoryBuiltController);

            //ControllerDescriptor controllerDescriptor = null;
            //if (typeof(IController).IsAssignableFrom(controllerType))
            //{
            //    controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            //}
            //else if (typeof(IAsyncController).IsAssignableFrom(controllerType))
            //{
            //    controllerDescriptor = new ReflectedAsyncControllerDescriptor(controllerType);
            //}

            var controllerDescriptor = this.GetControllerDescriptor(controllerType);


            //ActionDescriptor actionDescriptor = null;
            //try
            //{
            //    actionDescriptor = controllerDescriptor.FindAction(controllerContext, node.Action);
            //}
            //catch
            //{
            //}
            //if (actionDescriptor == null)
            //{
            //    actionDescriptor = controllerDescriptor.GetCanonicalActions().Where(a => a.ActionName == node.Action).FirstOrDefault();
            //}

            var actionDescriptor = this.GetActionDescriptor(node.Action, controllerDescriptor, controllerContext);

            // Verify security
            try
            {
                if (actionDescriptor != null)
                {
//#if NET35
//                    IEnumerable<AuthorizeAttribute> authorizeAttributesToCheck =
//                       actionDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType
//                           <AuthorizeAttribute>().ToList()
//                           .Union(
//                               controllerDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType
//                                   <AuthorizeAttribute>().ToList());
//#else
//                    IFilterProvider filterProvider = ResolveFilterProvider();
//                    IEnumerable<Filter> filters;

//                    // If depencency resolver has an IFilterProvider registered, use it
//                    if (filterProvider != null)
//                    {
//                        filters = filterProvider.GetFilters(controllerContext, actionDescriptor);
//                    }
//                    // Otherwise use FilterProviders.Providers
//                    else
//                    {
//                        filters = FilterProviders.Providers.GetFilters(controllerContext, actionDescriptor);
//                    }

//                    IEnumerable<AuthorizeAttribute> authorizeAttributesToCheck =
//                        filters
//                            .Where(f => typeof(AuthorizeAttribute).IsAssignableFrom(f.Instance.GetType()))
//                            .Select(f => f.Instance as AuthorizeAttribute);
//#endif
                    var authorizeAttributesToCheck = this.GetAuthorizeAttributes(controllerContext, actionDescriptor);

                    // Verify all attributes
                    foreach (var authorizeAttribute in authorizeAttributesToCheck)
                    {
                        try
                        {
                            //var currentAuthorizationAttributeType = authorizeAttribute.GetType();

                            //var builder = new AuthorizeAttributeBuilder();
                            //var subclassedAttribute =
                            //    currentAuthorizationAttributeType == typeof(AuthorizeAttribute) ?
                            //       new InternalAuthorize(authorizeAttribute) : // No need to use Reflection.Emit when ASP.NET MVC built-in attribute is used
                            //       (IAuthorizeAttribute)builder.Build(currentAuthorizationAttributeType).Invoke(null);

                            //// Copy all properties
                            //objectCopier.Copy(authorizeAttribute, subclassedAttribute);

                            //if (!subclassedAttribute.IsAuthorized(controllerContext.HttpContext))
                            //{
                            //    return false;
                            //}

                            var authorized = this.VerifyAuthorizeAttribute(authorizeAttribute, controllerContext);
                            if (!authorized)
                            {
                                return false;
                            }
                        }
                        catch
                        {
                            // do not allow on exception
                            return false;
                        }
                    }
                }

                // No objection.
                return true;
            }
            finally
            {
                // Restore HttpContext
                httpContext.RewritePath(originalPath, true);

                // Release controller
                if (factoryBuiltController)
                    ControllerBuilder.Current.GetControllerFactory().ReleaseController(controllerContext.Controller);
            }
        }

        #endregion



#if NET35
        protected virtual IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            return actionDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType
                           <AuthorizeAttribute>().ToList()
                           .Union(
                               controllerDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType
                                   <AuthorizeAttribute>().ToList());
        }
#else
        protected virtual IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            IFilterProvider filterProvider = ResolveFilterProvider();
            IEnumerable<Filter> filters;

            // If depencency resolver has an IFilterProvider registered, use it
            if (filterProvider != null)
            {
                filters = filterProvider.GetFilters(controllerContext, actionDescriptor);
            }
            // Otherwise use FilterProviders.Providers
            else
            {
                filters = FilterProviders.Providers.GetFilters(controllerContext, actionDescriptor);
            }

            return filters
                    .Where(f => typeof(AuthorizeAttribute).IsAssignableFrom(f.Instance.GetType()))
                    .Select(f => f.Instance as AuthorizeAttribute);
        }

        protected virtual IFilterProvider ResolveFilterProvider()
        {
            var key = "__MVCSITEMAP_F255D59E-D3E4-4BA9-8A5F-2AF0CAB282F4";
            var requestCache = httpContextFactory.GetRequestCache();
            var filterProvider = requestCache.GetValue<IFilterProvider>(key);
            if (filterProvider == null)
            {
                filterProvider = DependencyResolver.Current.GetService<IFilterProvider>();
                requestCache.SetValue<IFilterProvider>(key, filterProvider);
            }
            return filterProvider;
        }
#endif

        protected virtual bool VerifyAuthorizeAttribute(AuthorizeAttribute authorizeAttribute, ControllerContext controllerContext)
        {
            var currentAuthorizationAttributeType = authorizeAttribute.GetType();

            var builder = new AuthorizeAttributeBuilder();
            var subclassedAttribute =
                currentAuthorizationAttributeType == typeof(AuthorizeAttribute) ?
                   new InternalAuthorize(authorizeAttribute) : // No need to use Reflection.Emit when ASP.NET MVC built-in attribute is used
                   (IAuthorizeAttribute)builder.Build(currentAuthorizationAttributeType).Invoke(null);

            // Copy all properties
            objectCopier.Copy(authorizeAttribute, subclassedAttribute);

            if (!subclassedAttribute.IsAuthorized(controllerContext.HttpContext))
            {
                return false;
            }
            return true;
        }

        protected virtual ControllerDescriptor GetControllerDescriptor(Type controllerType)
        {
            ControllerDescriptor controllerDescriptor = null;
            if (typeof(IController).IsAssignableFrom(controllerType))
            {
                controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            }
            else if (typeof(IAsyncController).IsAssignableFrom(controllerType))
            {
                controllerDescriptor = new ReflectedAsyncControllerDescriptor(controllerType);
            }
            return controllerDescriptor;
        }

        protected virtual ActionDescriptor GetActionDescriptor(string action, ControllerDescriptor controllerDescriptor, ControllerContext controllerContext)
        {
            ActionDescriptor actionDescriptor = null;
            try
            {
                actionDescriptor = controllerDescriptor.FindAction(controllerContext, action);
            }
            catch
            {
                // TODO: Find out if there is a way to do this without throwing an exception / try to get the exeption info to throw in all
                // cases where it should.
            }
            if (actionDescriptor == null)
            {
                actionDescriptor = controllerDescriptor.GetCanonicalActions().Where(a => a.ActionName == action).FirstOrDefault();
            }
            return actionDescriptor;
        }

        protected virtual ControllerContext CreateControllerContext(RouteData routes, Type controllerType, out bool factoryBuiltController)
        {
            var requestContext = httpContextFactory.CreateRequestContext(routes);

            // Create controller context
            var controllerContext = new ControllerContext();
            controllerContext.RequestContext = requestContext;

            // Whether controller is built by the ControllerFactory (or otherwise by Activator)
            //bool factoryBuiltController = false;
            factoryBuiltController = false;
            try
            {
                string controllerName = requestContext.RouteData.GetRequiredString("controller");
                controllerContext.Controller = ControllerBuilder.Current.GetControllerFactory().CreateController(requestContext, controllerName) as ControllerBase;
                factoryBuiltController = true;
            }
            catch
            {
                // TODO: Try to prevent from swallowing real exceptions here

                try
                {
                    controllerContext.Controller = Activator.CreateInstance(controllerType) as ControllerBase;
                }
                catch
                {
                }
            }
            return controllerContext;
        }
    }
}
