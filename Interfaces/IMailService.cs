using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IMailService
    {
        /// <summary>
        /// Envía un correo de adjudicación con los datos del proveedor y los productos adjudicados.
        /// </summary>
        /// <param name="destinatario">Correo electrónico del proveedor adjudicado.</param>
        /// <param name="reqIdClave">Clave de la requisición adjudicada.</param>
        /// <param name="productos">Lista de productos adjudicados.</param>
        /// <returns>Resultado del envío con estado y mensaje.</returns>
        Task<MailResponse> EnviarCorreoAdjudicacionAsync(ProductoAdjudicadoDTO productoadjudicadoDTO, string emailusuario);


        Task<MailResponse> EnviarCorreoUsuAutAsyncTask(ProductoAdjudicadoDTO productoadjudicado, List<RequisicionDetUsuAut> usuaarioDTO, string emailusuario);



        Task<MailResponse> EnviarCorreoInvitacionAsync(string prov, string subject, string html, string tocorreo);

        Task<MailResponse> EnviarCorreoUsuAutLiberar(ProductoAdjudicadoDTO productoadjudicado, List<RequisicionDetUsuAut> usuaarioDTO, string emailusuario);
    }

}
