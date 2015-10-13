using HomeReadingManager.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
//using System.Web.Routing;

namespace HomeReadingManager.MyClasses
{
    public static class HelperClasses
    {
        public static IHtmlString DisplayColumnNameFor<TModel, TClass, TProperty>(this HtmlHelper<TModel> helper, IEnumerable<TClass> model, Expression<Func<TClass, TProperty>> expression)
        {
            var name = ExpressionHelper.GetExpressionText(expression);
            name = helper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            var metadata = ModelMetadataProviders.Current.GetMetadataForProperty(() => Activator.CreateInstance<TClass>(), typeof(TClass), name);

            return new MvcHtmlString(metadata.DisplayName);
        }

        

        
        //public static ExpandoObject ToExpando(this object anonymousObject)
        //{
        //    IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
        //    IDictionary<string, object> expando = new ExpandoObject();
        //    foreach (var item in anonymousDictionary)
        //        expando.Add(item);
        //    return (ExpandoObject)expando;
        //}

        public static MvcHtmlString NoEncodeActionLink(this HtmlHelper htmlHelper,
                                             string text, string title, string action,
                                             string controller,
                                             object routeValues = null,
                                             object htmlAttributes = null)
        {
            UrlHelper urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            TagBuilder builder = new TagBuilder("a");
            builder.InnerHtml = text;
            builder.Attributes["title"] = title;
            builder.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            builder.MergeAttributes(new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));

            return MvcHtmlString.Create(builder.ToString());
        }

        
    }

    
}