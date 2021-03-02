using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    /// <summary>
    /// DNS system aware host sorting
    /// </summary>
    class HostnameSorter : IComparer<string>
    {
        private readonly IComparer<string> _baseComparer;
        private readonly DomainParseService _domainParser;
        private readonly Dictionary<string, string> _tldCache;

        public HostnameSorter(DomainParseService domainParser)
        {
            _baseComparer = StringComparer.CurrentCulture;
            _domainParser = domainParser;
            _tldCache = new Dictionary<string, string>();
        }

        private string? GetTldCache(string domain)
        {
            if (_tldCache.TryGetValue(domain, out var value)) {
                return value;
            }
            try
            {
                value = _domainParser.GetTLD(domain);
                _tldCache.Add(domain, value);
            } 
            catch
            {
                value = ".error";
                _tldCache.Add(domain, value);
            }
            return value;
        }

        public int Compare(string? x, string? y)
        {
            // Handle null value according to framework default
            if (x == null || y == null)
            {
                return _baseComparer.Compare(x, y);
            }

            // Do not crash on wildcard domains
            var xtrim = x.Replace("*.", "");
            var ytrim = y.Replace("*.", "");

            // Determine TLD
            var xparts = xtrim.Split(".").ToList();
            var yparts = ytrim.Split(".").ToList();
            var xtld = GetTldCache(xtrim) ?? xparts.Last();
            var ytld = GetTldCache(ytrim) ?? yparts.Last();
          
            xparts = xparts.Take(xparts.Count - xtld.Split(".").Length).ToList();
            yparts = yparts.Take(yparts.Count - ytld.Split(".").Length).ToList();

            // Compare the main domain (sans TLD) to keep together
            // e.g. example.com, example.co.uk and example.net 
            var mainDomain = _baseComparer.Compare(xparts.LastOrDefault(), yparts.LastOrDefault());
            if (mainDomain != 0)
            {
                return mainDomain;
            }

            // Sort by TLD secondly
            var tldResult = _baseComparer.Compare(xtld, ytld);
            if (tldResult != 0)
            {
                return tldResult;
            }

            // Now look at sub domains, keep *.a.example.com above *.b.example.com, 
            // working our way down through the levels recursively
            var minParts = Math.Min(xparts.Count - 1, yparts.Count - 1);
            var xpartsCompare = xparts.Reverse<string>().Skip(1).Take(minParts).ToList();
            var ypartsCompare = yparts.Reverse<string>().Skip(1).Take(minParts).ToList();
            for (var i = minParts - 1; i >= 0; i--)
            {
                var partResult = _baseComparer.Compare(xpartsCompare[i], ypartsCompare[i]);
                if (partResult != 0)
                {
                    return partResult;
                }
            }

            // Prefer least number of items, keeping example.com on top 
            // of a.example.com, which in turn should be on top of *.a.example.com, etc.
            var parts = xparts.Count - yparts.Count;
            if (parts != 0)
            {
                return parts;
            }
            // Finally sort by length
            // so that *.example.com ends
            // up behind example.com
            return x.Length - y.Length;
        }
    }
}
