using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Extensions
{
    public static class DateExtensions
    {
        public static string ToUserString(this DateTime date)
        {
            return date.ToString(Properties.Settings.Default.FileDateFormat);
        }
    }
}
