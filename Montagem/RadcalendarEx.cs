using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;

namespace Montagem
{
    public class RadCalendarEx : RadCalendar
    {

        public RadCalendarEx()
            : base()
        {
        }

        static RadCalendarEx()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(RadCalendar).TypeHandle);

            RadCalendar.ColumnsProperty.OverrideMetadata(typeof(RadCalendarEx),
                new System.Windows.PropertyMetadata((object)1, new System.Windows.PropertyChangedCallback(RadCalendarEx.OnColumnsChangedEx),
                    new CoerceValueCallback(RadCalendarEx.CoerceColumns)));

        }

        private void OnColumnsChangedEx()
        {
            //Call private handler in the RadCalendar class.
            MethodInfo mi = typeof(RadCalendar).GetMethod("OnColumnsChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null)
            {
                mi.Invoke(this, null);
            }
        }

        private static void OnColumnsChangedEx(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((RadCalendarEx)sender).OnColumnsChangedEx();
        }

        private static object CoerceColumns(DependencyObject sender, object value)
        {
            //int num = Math.Min(4, (int)value);
            //return Math.Max(1, num);

            return value;
        }
    }
}
