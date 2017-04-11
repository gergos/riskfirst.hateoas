﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace RiskFirst.Hateoas
{
    public class DefaultRouteMap : IRouteMap
    {
        private readonly IActionContextAccessor contextAccessor;
        private readonly ILogger<DefaultRouteMap> logger;
        private IDictionary<string, RouteInfo> RouteMap { get; } = new Dictionary<string, RouteInfo>();
        public DefaultRouteMap(IActionContextAccessor contextAccessor, ILogger<DefaultRouteMap> logger)
        {
            this.contextAccessor = contextAccessor;
            this.logger = logger;

            var asm = Assembly.GetEntryAssembly();
           
            var controllers = asm.GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type));

            var controllerMethods = controllers.SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                                 .Where(m => m.IsDefined(typeof(HttpMethodAttribute)))
                                                                 .Select(m => new {
                                                                     Controller = c,
                                                                     Method = m
                                                                 }));

            var attributes = controllerMethods.Select(m => new {
                Controller = m.Controller,
                Method = m.Method,
                HttpAttribute = m.Method.GetCustomAttribute<HttpMethodAttribute>()
            });

            foreach (var attr in attributes.Where(a => !String.IsNullOrWhiteSpace(a.HttpAttribute.Name)))
            {
                var method = ParseMethod(attr.HttpAttribute.HttpMethods);
                RouteMap[attr.HttpAttribute.Name] = new RouteInfo(attr.HttpAttribute.Name, method, new ReflectionControllerMethodInfo(attr.Method));
            }
        }
                
        public RouteInfo GetRoute(string name)
        {
            if (!RouteMap.ContainsKey(name))
            {
                return null;
            }
            return RouteMap[name];
        }

        public RouteInfo GetCurrentRoute()
        {
            var action = this.contextAccessor?.ActionContext?.ActionDescriptor as ControllerActionDescriptor;
            if (action == null)
                throw new InvalidOperationException($"Invalid action descriptor in route map");
            var attr = action.MethodInfo.GetCustomAttribute<HttpMethodAttribute>();
            var method = ParseMethod(attr.HttpMethods);
            return new RouteInfo(attr.Name, method, new ReflectionControllerMethodInfo(action.MethodInfo));
        }

        private HttpMethod ParseMethod(IEnumerable<string> methods)
        {
            HttpMethod method = HttpMethod.Get;
            if (methods != null && methods.Any())
            {
                method = new HttpMethod(methods.First());
            }
            return method;
        }

    }
}
