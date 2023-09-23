using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CUS.Models
{
    public partial class Paciente
    {

        public void ConvertirAMayusculas()
        {
            CURP = CURP.ToUpper();
            Nombre = Nombre.ToUpper();
            PrimerApellido = PrimerApellido.ToUpper();
            SegundoApellido = SegundoApellido.ToUpper();
            Nacionalidad = Nacionalidad.ToUpper();
            EntidadNacimiento = EntidadNacimiento.ToUpper();
            Nombre = Nombre.ToUpper();
            Dir_Calle = Dir_Calle.ToUpper();
            Dir_Colonia = Dir_Colonia.ToUpper();
            Dir_Municipio = Dir_Municipio.ToUpper();
            Dir_EntidadFed = Dir_EntidadFed.ToUpper();
            RFC = RFC.ToUpper();
            CE_Nombre = CE_Nombre.ToUpper();
            CE_Calle = CE_Calle.ToUpper();
            CE_Colonia = CE_Colonia.ToUpper();
            CE_Municipio = CE_Municipio.ToUpper();
            CE_EntidadFed = CE_EntidadFed.ToUpper();
            //Nombre_Tutor = Nombre_Tutor.ToUpper();
            //PrimerApellido_Tutor = PrimerApellido_Tutor.ToUpper();
            //SegundoApellido_Tutor = SegundoApellido_Tutor.ToUpper();
        }

    }
}