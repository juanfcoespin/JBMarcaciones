using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarcacionesDll;
using TechTools.Logs;
using ConsoleUtils;

namespace EnvioMarcacionesSAP
{
    class Program
    {
        static void Main(string[] args)
        {
            EnviarMarcacionesPorSucursal(eSucursal.Pifo);
            EnviarMarcacionesPorSucursal(eSucursal.Puembo);
        }
        private static void EnviarMarcacionesPorSucursal(eSucursal sucursal) {
            var log = new LogUtils();
            log.AddLogAndShow("Iniciando Proceso de envio de marcaciones a SAP...");
            var marcacionesMng = new MarcacionesManager(sucursal);
            marcacionesMng.eMsg += (me) => log.AddLogAndShow(me);
            marcacionesMng.eError += (me) => log.AddError(me);
            marcacionesMng.SendMarcacionesToSAP(DateTime.Now); //envia las marcaciones de hoy y las que no hayan sido registradas
            log.AddLogAndShow("Fin Proceso");
        }
    }
}
