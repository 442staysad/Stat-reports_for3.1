using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum DeadlineType
    {
        Monthly,
        Quarterly, // Каждые 3 месяца (последний день квартала)
        HalfYearly, // Раз в полгода (30 июня, 31 декабря)
        Yearly // Раз в год (30 марта следующего года)
    }
}