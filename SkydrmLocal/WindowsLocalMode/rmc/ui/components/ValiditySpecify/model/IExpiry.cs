using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.components.ValiditySpecify.model
{
    public interface IExpiry
    {
        int GetOpetion();
    }

    public interface INeverExpire : IExpiry
    {

    }

    public interface IRelative : IExpiry
    {
        int GetYears();
        int GetMonths();
        int GetWeeks();
        int GetDays();
    }

    public interface IAbsolute : IExpiry
    {
        long EndDate();
    }

    public interface IRange : IExpiry
    {
        long StartDate();
        long EndDate();
    }
}
