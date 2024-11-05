using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

using ConsoleUtils;
using MarcacionesDll;

namespace marcaciones
{
    class Program
    {
        static void Main(string[] args)
        {
            showMenu();
        }
        private static void showMenu()
        {
            var msg = @"
            -------- Menu ---------
            1. Mostrar Atrasos
            2. Corregir Atrasos
            3. Mostrar Marcaciones por día
            4. Insertar Marcación
            5. Actualizar en lote
            0. Salir

Opcion:?";
            cu.show(msg);
            try
            {
                var marcacionesMng = new MarcacionesManager(eSucursal.Pifo);
                marcacionesMng.eMsg += (me) => Console.Write(me);
                marcacionesMng.eError += (me) => Console.Write("Error:" + Environment.NewLine + me);
                switch (cu.getInt())
                {
                    case 0:
                        return; //salir
                    case 1:
                        cu.show("mes?:");
                        var mes = cu.getInt();
                        marcacionesMng.consultarAtrasos(mes);
                        break;
                    case 2:
                        marcacionesMng.CorregirAtraso();
                        break;
                    case 3:
                        var fecha = MarcacionesUtils.getFecha();
                        marcacionesMng.mostrarMarcacionesPorFecha(fecha);
                        break;
                    case 4:
                        marcacionesMng.insertarMarcacion();
                        break;
                    case 5:
                        marcacionesMng.CorregirAtraso(true);
                        break;
                    default:
                        cu.show("Opción Incorrecta !!");
                        break;
                }
                showMenu();
            }
            catch (Exception)
            {
                cu.show("Opción Incorrecta !!");
            }
        }
    }
}
