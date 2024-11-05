using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using TechTools.Core.Access;
using TechTools.Utils;

namespace MarcacionesDll
{
    public enum eSucursal
    {
        Pifo,
        Puembo
    }
    public class MarcacionesManager
    {
        
        public delegate void dMsg(string msg);
        public event dMsg eMsg;
        public event dMsg eError;
        public int UserId;
        public string SerialNumber;
        private eSucursal _Sucursal;
        private string _PrefijoCodEmpleado;

        public MarcacionesManager(eSucursal sucursal) {
            this._Sucursal = sucursal;
            switch (sucursal) {
                case eSucursal.Pifo:
                    _PrefijoCodEmpleado = "PF";
                    break;
                case eSucursal.Puembo:
                    _PrefijoCodEmpleado = "PU";
                    break;
            }
            if (sucursal == eSucursal.Pifo)
            {
                this.UserId = 3050;
                this.SerialNumber = "6422153300172";
            }
        }

            /// <summary>
        /// 
        /// </summary>
        /// <param name="enLote">cuando se quiera actualizar en lote todos los atrasos a partir del 07 de nov del 2019</param>
        public  void CorregirAtraso(bool enLote = false)
        {
            var dtAtrasos = getDtAtrasos(enLote);
            if (dtAtrasos != null && dtAtrasos.Rows.Count > 0)
            {
                foreach (DataRow dr in dtAtrasos.Rows)
                {
                    var fecha = new CustomDate();
                    fecha.Year = MarcacionesUtils.getInt(dr["anio"].ToString());
                    fecha.Mes = MarcacionesUtils.getInt(dr["mes"].ToString());
                    fecha.Dia = MarcacionesUtils.getInt(dr["dia"].ToString());
                    //cuando descargan las marcaciones se registra otra vez el atraso ya corregido
                    var dtMarcacionesMañana = GetMarcacionesMañana(fecha);
                    if (dtMarcacionesMañana.Rows.Count > 1)
                        DejarSoloMarcacionMasTemprana(dtMarcacionesMañana, fecha);
                    //Si no hay atrasos, la siguiente función no hace nada
                    ActualizarAtraso(fecha);
                    mostrarMarcacionesPorFecha(fecha);
                }
            }
        }
        /// <summary>
        /// return dataTable with columns: CHECKTIME, year,mes,dia
        /// </summary>
        /// <param name="enLote"></param>
        /// <returns></returns>
        private DataTable getDtAtrasos(bool enLote)
        {
            try
            {
                if (!enLote)
                {//pide el mes y el dia del que se quiere traer el atraso
                    var fecha = MarcacionesUtils.getFecha();
                    var dt = new DataTable();
                    dt.Columns.Add("CHECKTIME", typeof(string));
                    dt.Columns.Add("anio", typeof(string));
                    dt.Columns.Add("mes", typeof(string));
                    dt.Columns.Add("dia", typeof(string));
                    var dr = dt.NewRow();
                    dr["anio"] = DateTime.Now.Year;
                    dr["mes"] = fecha.Mes;
                    dr["dia"] = fecha.Dia;
                    dt.Rows.Add(dr);
                    return dt;
                }
                else
                {
                    //atrasos desde el 8 de nov del 2019
                    var sql = string.Format(@"
                        SELECT
                         CHECKTIME,
                         Year(CHECKTIME) as anio,
                         Month(CHECKTIME) as mes,
                         Day(CHECKTIME) as dia
                        from 
                        CHECKINOUT
                        where 
                        userid={0}
                        and CHECKTIME>=#11/08/2019#
                        and Format(CHECKTIME,'yyyy/mm/dd') not in ({1})
                        and {2}
                    ", this.UserId,conf.Default.omitirFechas, conf.Default.condicionAtraso);
                    var dt = GetConection().GetDataTableByQuery(sql);
                    return dt;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        private DataTable GetMarcacionesMañana(CustomDate fecha)
        {
            var sql = string.Format(@"
                select
                    Minute(CHECKTIME) as Minutos
                from
                    CHECKINOUT
                where                
                    userid={0}
                    and Year(CHECKTIME)={1}
                    and Month(CHECKTIME)={2}
                    and Day(CHECKTIME)={3}
                    and Hour(CHECKTIME) in (7,8)
                    
            ", this.UserId, DateTime.Now.Year, fecha.Mes, fecha.Dia);
            var ms = GetConection().GetDataTableByQuery(sql);
            return ms;
        }
        private void DejarSoloMarcacionMasTemprana(DataTable dtMarcacionesMañana, CustomDate mesDia)
        {
            var minutoMasTemprano = 100;
            foreach (DataRow dr in dtMarcacionesMañana.Rows)
            {
                var currentMinute = MarcacionesUtils.getInt(dr["Minutos"].ToString());
                if (currentMinute > 0 && currentMinute < minutoMasTemprano) //da cero cuando hay un error de conversión
                    minutoMasTemprano = currentMinute;
            }
            if (minutoMasTemprano != 100 && minutoMasTemprano > 0)
            {
                //se borran todas las marcaciones que tengan el minuto mas temprano
                var sql = string.Format(@"
                delete from CHECKINOUT
                where                
                    userid={0}
                    and Year(CHECKTIME)={1}
                    and Month(CHECKTIME)={2}
                    and Day(CHECKTIME)={3}
                    and Hour(CHECKTIME)=7
                    and Minute(CHECKTIME)<>{4}
            ", this.UserId, DateTime.Now.Year, mesDia.Mes, mesDia.Dia, minutoMasTemprano);
                GetConection().Execute(sql);
            }
        }
        private  void ActualizarAtraso(CustomDate fecha)
        {
            // en access mes/dia/año
            var checkTime = MarcacionesUtils.getCheckTime(fecha, "m");
            var sql = string.Format(@"
                update CHECKINOUT
                    SET CHECKTIME = {0}
                where                
                    userid={1}
                    and Year(CHECKTIME)={2}
                    and Month(CHECKTIME)={3}
                    and Day(CHECKTIME)={4}
                    and {5}
            ", checkTime,this.UserId, fecha.Year, fecha.Mes, fecha.Dia, conf.Default.condicionAtraso);
            try
            {
                GetConection().Execute(sql);
                eMsg?.Invoke("Actualizado Correctamente!!");

            }
            catch (Exception e)
            {
                eError?.Invoke(e.Message);
            }
        }
        public void mostrarMarcacionesPorFecha(CustomDate fecha)
        {
            var sql = string.Format(@"
            SELECT USERID, CHECKTIME
                FROM CHECKINOUT
            WHERE
                userid={0}
                and Year(CHECKTIME)={1}
                and Month(CHECKTIME)={2}
                and Day(CHECKTIME)={3}", this.UserId, fecha.Year, fecha.Mes, fecha.Dia);
            showRecords(sql);
        }
        public  void showRecords(string sql)
        {
            try
            {
                var bdd = GetConection();
                var dt = bdd.GetDataTableByQuery(sql);
                var msg = Environment.NewLine;
                msg+="UserID, FechaMarcacion"+Environment.NewLine;
                msg+="----------------------" + Environment.NewLine;
                foreach (DataRow dr in dt.Rows)
                {
                    msg+=string.Format("{0}, {1}",
                        dr["userId"], dr["checkTime"]) + Environment.NewLine;
                }
                eMsg?.Invoke(msg);
            }
            catch (Exception e)
            {
                eError?.Invoke(e.Message);
            }
        }
        public  void insertarMarcacion()
        {
            var fecha = MarcacionesUtils.getFecha();
            // en access mes/dia/año
            var checkTime = MarcacionesUtils.getCheckTime(fecha);
            var sql = string.Format(@"
                insert into checkinout(
                    userid, checktime, checktype,
                    verifycode, sensorId, workcode,
                    sn, userExtFmt)
                values(
                    {0}, {1},'O',
                    1,1, 0,
                    {2},1)
            ", this.UserId, checkTime, this.SerialNumber);
            try
            {
                GetConection().Execute(sql);
                eMsg?.Invoke("insertado Correctamente!!");
                mostrarMarcacionesPorFecha(fecha);
            }
            catch (Exception e)
            {
                eError?.Invoke(e.Message);
            }
        }
        public  void consultarAtrasos(int mes)
        {
            var sql = string.Format(@"
            SELECT USERID, CHECKTIME
            FROM CHECKINOUT
            WHERE
            userid={0}
            and Year(CHECKTIME)=Year(Now())
            and Month(CHECKTIME)={1}
            and {2}",this.UserId, mes, conf.Default.condicionAtraso);
            showRecords(sql);
        }
        /// <summary>
        /// Trae las macaciones del dia pasado como parámetro
        /// </summary>
        /// <param name="me">dia pasado como parámetro</param>
        /// <returns></returns>
        private DataTable GetMarcacionesOfCurrentYear() {
            var sql = string.Format(@"
                SELECT
                 userId as tarjeta,
                 Switch(
                        Hour(CHECKTIME) in (6,7,8), 'E', 
                        Hour(CHECKTIME) in (9,10,11), 'P', 
                        Hour(CHECKTIME) in (12,13,14), 'A',
                        Hour(CHECKTIME) >= 15, 'S'
                 ) AS es,
                 Format(CHECKTIME,'dd/mm/yyyy') as Fecha,
                 Format(CHECKTIME,'HHMM') as Hora,
                 '' as Tipo,
                 Format(CHECKTIME,'dd/mm/yyyy HH:MM:ss') as fechaHora
                from 
                 CHECKINOUT
                WHERE
                 enviadoA_SAP=0
                 and year(CHECKTIME)={0}
            ", DateTime.Now.Year);
            var dt = GetConection().GetDataTableByQuery(sql);
            dt = PonerPrefijoSucursalYceroALaIzqSiAplica(dt);
            return dt;
        }
        private DataTable PonerPrefijoSucursalYceroALaIzqSiAplica(DataTable dt)
        {
            /*
              Por defecto los codigos de tarjeta se generan de la siguiente forma:
                tarjeta;es;Fecha;Hora;Tipo
                2940;E;07/02/2020;0729;;
                490;E;07/02/2020;0730;;

                Lo que hace el algoritmo es los siguiente en el campo tarjeta:
                f(490, pifo) -> 'PI0490'
                f(490, puembo) -> 'PU0490'
                f(2940, puembo) -> 'PU2940'
             */
            /*
            Se añade un campo adicional para registrar el nuevo codigo del empleado
            ya que en la base de datos el campo 'Tarjeta' es del tipo Int
            */
            var nombreCampo = "CodEmpleado";
            dt.Columns.Add(nombreCampo, typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++) {
                
                var codEmpleado = dt.Rows[i]["tarjeta"].ToString();
                codEmpleado = StringUtils.PonerCerosIzquierda(4, codEmpleado);
                codEmpleado = this._PrefijoCodEmpleado + codEmpleado;
                dt.Rows[i][nombreCampo] = codEmpleado;
            }
            return dt;
        }
        private TechTools.Core.Access.BaseCore GetConection() {
            switch (this._Sucursal) {
                case eSucursal.Pifo:
                    return new BaseCore(conf.Default.bddPifoPath);
                case eSucursal.Puembo:
                    return new BaseCore(conf.Default.bddPuemboPath);
            }
            return null;
        }
        private bool CreateFileToSAP(DataTable dtMarcaciones, DateTime fileDate) {
            try
            {
                //var cabecera = "tarjeta;es;Fecha;Hora;Tipo";
                var data = string.Empty;
                if (dtMarcaciones != null && dtMarcaciones.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtMarcaciones.Rows)
                    {
                        data = string.Format("{0}{1};{2};{3};{4};{5};{6}",
                            data,
                            dr["CodEmpleado"].ToString(),
                            dr["es"].ToString(),
                            dr["Fecha"].ToString(),
                            dr["Hora"].ToString(),
                            dr["Tipo"].ToString(),
                            Environment.NewLine);
                    }
                }
                if (string.IsNullOrEmpty(data))
                {
                    eMsg?.Invoke("No hay marcaciones para enviar");
                    return false; 
                }
                data = string.Format("{0}{1}", data, Environment.NewLine);
                string fileName = GetSapFileName(fileDate);
                FileUtils.AddDataToFile(fileName, data);
                eMsg?.Invoke("Generado archivo: " + fileName);
                return true;
            }
            catch (Exception e)
            {
                eError?.Invoke(e.Message);
                return false;
            }
        }
        public void SendMarcacionesToSAP(DateTime fileDate) {
            //if (this._Sucursal == eSucursal.Pifo)
            //    CorregirAtraso(true);
            var dt = GetMarcacionesOfCurrentYear();
            if (CreateFileToSAP(dt,fileDate)) 
                SetMarcacionesAsSended(dt);
        }
        private bool SetMarcacionesAsSended(DataTable dtMarcaciones)
        {
            try
            {
                foreach (DataRow dr in dtMarcaciones.Rows)
                {
                    var sql = string.Format(@"
                        update CHECKINOUT
                         set enviadoA_SAP=1
                        where
                         userId={0}
                         and Format(CHECKTIME,'dd/mm/yyyy HH:MM:ss')='{1}'
                        ", dr["tarjeta"].ToString(),
                        dr["fechaHora"].ToString());
                    GetConection().Execute(sql);
                    eMsg?.Invoke(string.Format("Registrado usuario {0}", dr["CodEmpleado"].ToString()));
                }
                eMsg?.Invoke(string.Format("Registradas marcaciones {0} en la bdd SAP",dtMarcaciones.Rows.Count));
                return true;
            }
            catch (Exception e)
            {
                eError?.Invoke(e.Message);
                return false;
            }
            
        }
        private  string GetSapFileName(DateTime dateFile)
        {
            return String.Format(@"{0}\MarcacionesSAP_{1}_{2}{3}{4}_{5}{6}.txt",
                conf.Default.SapFolder,
                this._Sucursal.ToString(),
                dateFile.Year,
                StringUtils.getTwoDigitNumber(dateFile.Month),
                StringUtils.getTwoDigitNumber(dateFile.Day),
                StringUtils.getTwoDigitNumber(dateFile.Hour),
                StringUtils.getTwoDigitNumber(dateFile.Minute)
            ) ;
        }
    }
}
