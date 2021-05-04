﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Liquid.Core.Configuration;
using Liquid.Core.Localization.Entities;
using Liquid.Core.Utils;

namespace Liquid.Core.Localization
{
    /// <summary>
    /// Resource file catalog class.
    /// </summary>
    /// <seealso cref="ILocalization" />
    public class JsonFileLocalization : ILocalization
    {
        private readonly IDictionary<CultureInfo, LocalizationCollection> _localizationItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFileLocalization" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public JsonFileLocalization(ILightConfiguration<CultureSettings> configuration)
        {
            _localizationItems = JsonFileLocalization.ReadLocalizationFiles(configuration);
        }
        
        /// <summary>
        /// Gets the specified string according to culture.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>
        /// The string associated with resource key.
        /// </returns>
        /// <exception cref="ArgumentNullException">key</exception>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return Get(key, Thread.CurrentThread.CurrentCulture);
        }

        /// <summary>
        /// Gets the specified string according to culture.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>
        /// The string associated with resource key.
        /// </returns>
        /// <exception cref="ArgumentNullException">key</exception>
        public string Get(string key, string channel)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return Get(key, Thread.CurrentThread.CurrentCulture, channel);
        }
        
        /// <summary>
        /// Gets the specified string according to culture.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>
        /// The string associated with resource key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// key
        /// or
        /// culture
        /// </exception>
        /// <exception cref="LocalizationException"></exception>
        public string Get(string key, CultureInfo culture, string channel = null)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            
            try
            {
                if (_localizationItems.TryGetValue(culture, out var localizationItem))
                {
                    var item = localizationItem.Items.GetResourceItem(key);
                    var itemValue = item?.GetResourceValue(channel);
                    if (itemValue != null) { return itemValue.Value; }
                }
                return key;
            }
            catch (Exception ex)
            {
                throw new LocalizationException(key, ex);
            }
        }

        /// <summary>
        /// Reads the resource collection from file.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="LocalizationReaderException"></exception>
        private static IDictionary<CultureInfo, LocalizationCollection> ReadLocalizationFiles(ILightConfiguration<CultureSettings> configuration)
        {
            var items = new ConcurrentDictionary<CultureInfo, LocalizationCollection>();
            try
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var files = Directory.EnumerateFiles(path, "localization*.json", SearchOption.AllDirectories);

                foreach (var fileName in files)
                {
                    var fileInfo = new FileInfo(fileName);
                    var filenameParts = fileInfo.Name.Split('.');
                    var culture = filenameParts.Length == 2 ? configuration.Settings.DefaultCulture : filenameParts[1];

                    using var fileReader = new StreamReader(fileName);
                    var json = fileReader.ReadToEnd();
                    var localizationItems = json.ParseJson<LocalizationCollection>();
                    items.TryAdd(new CultureInfo(culture), localizationItems);
                }
            }
            catch (Exception exception)
            {
                throw new LocalizationReaderException(exception);
            }
            return items;
        }
    }
}