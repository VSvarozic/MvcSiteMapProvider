﻿using System;
using System.Web.Mvc;
using MvcSiteMapProvider.Web.Mvc;
using System.Web;

namespace MvcSiteMapProvider.Web.Html
{
    /// <summary>
    /// MvcSiteMapHtmlHelper class
    /// </summary>
    public class MvcSiteMapHtmlHelper
    {
        /// <summary>
        /// Gets or sets the HTML helper.
        /// </summary>
        /// <value>The HTML helper.</value>
        public HtmlHelper HtmlHelper { get; protected set; }

        /// <summary>
        /// Gets or sets the sitemap.
        /// </summary>
        /// <value>The sitemap.</value>
        public ISiteMap SiteMap { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcSiteMapHtmlHelper"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="provider">The sitemap.</param>
        public MvcSiteMapHtmlHelper(HtmlHelper htmlHelper, ISiteMap siteMap)
            : this(htmlHelper, siteMap, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcSiteMapHtmlHelper"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="provider">The sitemap.</param>
        internal MvcSiteMapHtmlHelper(HtmlHelper htmlHelper, ISiteMap siteMap, bool useViewEngine)
        {
            if (htmlHelper == null)
                throw new ArgumentNullException("htmlHelper");
            if (siteMap == null)
                throw new ArgumentNullException("siteMap");

            HtmlHelper = htmlHelper;
            SiteMap = siteMap;

            if (useViewEngine) 
                MvcSiteMapProviderViewEngine.Register();
        }

        /// <summary>
        /// Creates the HTML helper for model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public HtmlHelper<TModel> CreateHtmlHelperForModel<TModel>(TModel model)
        {
            return new HtmlHelper<TModel>(HtmlHelper.ViewContext, new ViewDataContainer<TModel>(model));
        }
    }
}
