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

        public HostnameSorter(DomainParseService domainParser)
        {
            _baseComparer = StringComparer.CurrentCulture;
            _domainParser = domainParser;
        }

        public int Compare(string? x, string? y)
        {
            // Handle null value according to framework default
            if (x == null || y == null)
            {
                return _baseComparer.Compare(x, y);
            }

            // Determine TLD
            var xtld = _domainParser.GetTLD(x);
            var ytld = _domainParser.GetTLD(y);

            var xparts = x.Split(".").ToList();
            xparts = xparts.Take(xparts.Count - xtld.Split(".").Length).ToList();
            var yparts = y.Split(".").ToList();
            yparts = yparts.Take(yparts.Count - ytld.Split(".").Length).ToList();

            // Compare the main domain (sans TLD) to keep together
            // e.g. example.com, example.co.uk and example.net 
            var mainDomain = _baseComparer.Compare(xparts.Last(), yparts.Last());
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
            return xparts.Count - yparts.Count;
        }
    }
}
