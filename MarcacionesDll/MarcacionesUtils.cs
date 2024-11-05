using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacionesDll
{
    public class MarcacionesUtils
    {
        public static CustomDate getFecha()
        {
            var fecha = new CustomDate();
            fecha.Year = DateTime.Now.Year;
            cu.show("Mes?:");
            fecha.Mes = cu.getInt();
            cu.show("Dia?:");
            fecha.Dia = cu.getInt();
            return fecha;
        }
        public static int getInt(string me)
        {
            try
            {
                return Convert.ToInt32(me);
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fecha"></param>
        /// <param name="tipo">mañana (m)/ tarde (t)</param>
        /// <returns></returns>
        public static string getCheckTime(CustomDate fecha, string tipo = null)
        {

            if (tipo == null)
            {
                cu.show("mañana (m)/ tarde (t)?");
                tipo = Console.ReadLine();
            }
            var ms = string.Format("#{0}/{1}/{2} ", fecha.Mes, fecha.Dia, fecha.Year);
            var hour = string.Empty;
            switch (tipo)
            {
                case "m":
                    hour = string.Format("7:{0}:{1}#", GetRandomNumber(28, 34), GetRandomNumber(10, 58));
                    break;
                case "t":
                    hour = string.Format("16:{0}:{1}#", GetRandomNumber(30, 59), GetRandomNumber(10, 58));
                    break;
            }
            if (string.IsNullOrEmpty(hour))
            {
                cu.show("Opcion Incorrecta!!");
                return null;
            }
            ms += hour;
            return ms;
        }
        public static int GetRandomNumber(int min, int max)
        {
            return new Random().Next(min, max);
        }
    }
    public class cu
    {
        public static void show(string msg) => Console.WriteLine(msg);

        public static int getInt()
        {
            try
            {
                return Convert.ToInt32(Console.ReadLine());
            }
            catch
            {
                return -1;
            }
        }
    }
}
