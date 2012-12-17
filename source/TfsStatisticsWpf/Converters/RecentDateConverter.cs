using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TfsStatisticsWpf.Converters
{
    [ValueConversion(typeof(DateTime), typeof(string))]  
    public class RecentDateConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen Wert.
        /// </summary>
        /// <returns>
        /// Ein konvertierter Wert. Wenn die Methode null zurückgibt, wird der gültige NULL-Wert verwendet.
        /// </returns>
        /// <param name="value">Der von der Bindungsquelle erzeugte Wert.</param>
        /// <param name="targetType">Der Typ der Bindungsziel-Eigenschaft.</param>
        /// <param name="parameter">Der zu verwendende Konverterparameter.</param>
        /// <param name="culture">Die im Konverter zu verwendende Kultur.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(string) != targetType || !(value is DateTime))
                return value;

            DateTime date = (DateTime)value;

            if (date.Date != DateTime.Today.Date)
                return date.ToString(culture);

            var span = DateTime.Now - date;

            return span.TotalHours > 1
                ? string.Format(culture, "{0:0} hours ago", span.TotalHours)
                : string.Format(culture, "{0:0} minutes ago", span.TotalMinutes);
        }

        /// <summary>
        /// Konvertiert einen Wert.
        /// </summary>
        /// <returns>
        /// Ein konvertierter Wert. Wenn die Methode null zurückgibt, wird der gültige NULL-Wert verwendet.
        /// </returns>
        /// <param name="value">Der Wert, der vom Bindungsziel erzeugt wird.</param>
        /// <param name="targetType">Der Typ, in den konvertiert werden soll.</param>
        /// <param name="parameter">Der zu verwendende Konverterparameter.</param>
        /// <param name="culture">Die im Konverter zu verwendende Kultur.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typedValue = value as string;

            if (typeof(DateTime) != targetType || typedValue == null)
                return value;

            int parsed;
            if (typedValue.EndsWith(" hours ago") && int.TryParse(typedValue.Replace(" hours ago", string.Empty), out parsed))
                return DateTime.Now.AddHours(-1 * parsed);

            if (typedValue.EndsWith(" minutes ago") && int.TryParse(typedValue.Replace(" minutes ago", string.Empty), out parsed))
                return DateTime.Now.AddMinutes(-1 * parsed);

            DateTime result;
            if (DateTime.TryParse(typedValue, culture, DateTimeStyles.AssumeLocal, out result))
                return result;

            return value;
        }
    }
}
