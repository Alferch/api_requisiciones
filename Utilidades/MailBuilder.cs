using RequisicionesApi.Models;
using System.Text;

namespace RequisicionesApi.Utilidades
{
    public static class MailBuilder
    {
        public static string GenerarCorreoAdjudicacion(ProductoAdjudicadoDTO dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; background-color: #f9f9f9; color: #333; padding: 20px; }");
            sb.AppendLine(".container { background-color: #ffffff; border: 1px solid #ddd; border-radius: 8px; padding: 30px; max-width: 800px; margin: auto; }");
            sb.AppendLine("h2 { color: #004080; margin-bottom: 10px; }");
            sb.AppendLine("p { font-size: 15px; line-height: 1.6; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 14px; }");
            sb.AppendLine("th { background-color: #004080; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { border: 1px solid #ccc; padding: 8px; }");
            sb.AppendLine(".footer { margin-top: 30px; font-size: 13px; color: #666; }");
            sb.AppendLine(".firma { margin-top: 20px; }");
            sb.AppendLine("</style></head><body><div class='container'>");

            sb.AppendLine($"<h2>📢 Adjudicación – Requisición {dto.RequisicionId}</h2>");
            sb.AppendLine($"<p>Estimado/a <strong>{dto.NombreProveedor}</strong>,</p>");
            sb.AppendLine("<p>Nos complace informarle que ha sido seleccionado como proveedor adjudicado en el siguiente proceso. A continuación, se detallan los productos adjudicados:</p>");

            sb.AppendLine("<table><thead><tr>");
            sb.AppendLine("<th>Posición</th><th>Código</th><th>Descripción</th><th>Unidad</th><th>Cantidad</th><th>Precio</th><th>Cond. Pago</th><th>Entrega</th><th>Cargo Ext.</th><th>Moneda</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var p in dto.Productos)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{p.Posicion}</td>");
                sb.AppendLine($"<td>{p.CodigoMaterial}</td>");
                sb.AppendLine($"<td>{p.Descripcion}</td>");
                sb.AppendLine($"<td>{p.Unidad}</td>");
                sb.AppendLine($"<td>{p.Cantidad}</td>");
                sb.AppendLine($"<td>{p.PrecioUnitario}</td>");
                sb.AppendLine($"<td>{p.CondicionPago}</td>");
                sb.AppendLine($"<td>{p.FechaEntrega}</td>");
                sb.AppendLine($"<td>{p.CargoExterno}</td>");
                sb.AppendLine($"<td>{p.Moneda}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<p class='firma'>En los próximos días recibirá el contrato marco y los detalles logísticos correspondientes. Agradecemos su participación y quedamos atentos a cualquier consulta.</p>");

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Atentamente,</p>");
            sb.AppendLine("<p><strong>Francisco</strong><br>Coordinador de Licitaciones<br>Tu Empresa S.A.<br>📞 +52 55 1234 5678<br>✉️ francisco@tuempresa.com</p>");
            sb.AppendLine("</div></div></body></html>");

            return sb.ToString();
        }


        public static string GenerarCorreoUsuarioAut(ProductoAdjudicadoDTO dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; background-color: #f9f9f9; color: #333; padding: 20px; }");
            sb.AppendLine(".container { background-color: #ffffff; border: 1px solid #ddd; border-radius: 8px; padding: 30px; max-width: 800px; margin: auto; }");
            sb.AppendLine("h2 { color: #004080; margin-bottom: 10px; }");
            sb.AppendLine("p { font-size: 15px; line-height: 1.6; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 14px; }");
            sb.AppendLine("th { background-color: #004080; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { border: 1px solid #ccc; padding: 8px; }");
            sb.AppendLine(".footer { margin-top: 30px; font-size: 13px; color: #666; }");
            sb.AppendLine(".firma { margin-top: 20px; }");
            sb.AppendLine("</style></head><body><div class='container'>");

            sb.AppendLine($"<h2>📢 Autorizar – Requisición {dto .RequisicionId}</h2>");
            sb.AppendLine($"<p>Estimado/a usuario<strong>{dto .NombreProveedor}</strong>,</p>");
            sb.AppendLine("<p>Nos complace informarle que ha sido seleccionado como Autorizador de la Requisicion que a continuación se detallan los productos Solicitados:</p>");

            sb.AppendLine("<table><thead><tr>");
            sb.AppendLine("<th>Posición</th><th>Código</th><th>Descripción</th><th>Unidad</th><th>Cantidad</th><th>Precio</th><th>Cond. Pago</th><th>Entrega</th><th>Cargo Ext.</th><th>Moneda</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var p in dto.Productos)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{p.Posicion}</td>");
                sb.AppendLine($"<td>{p.CodigoMaterial}</td>");
                sb.AppendLine($"<td>{p.Descripcion}</td>");
                sb.AppendLine($"<td>{p.Unidad}</td>");
                sb.AppendLine($"<td>{p.Cantidad}</td>");
                sb.AppendLine($"<td>{p.PrecioUnitario}</td>");
                sb.AppendLine($"<td>{p.CondicionPago}</td>");
                sb.AppendLine($"<td>{p.FechaEntrega}</td>");
                sb.AppendLine($"<td>{p.CargoExterno}</td>");
                sb.AppendLine($"<td>{p.Moneda}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<p class='firma'>le solicitamos este pendiente de acuerdo a su nivel. Agradecemos su participación y quedamos atentos a cualquier consulta.</p>");

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Atentamente,</p>");
            sb.AppendLine("<p><strong>Francisco</strong><br>Coordinador de Licitaciones<br>Tu Empresa S.A.<br>📞 +52 55 1234 5678<br>✉️ francisco@tuempresa.com</p>");
            sb.AppendLine("</div></div></body></html>");

            return sb.ToString();
        }



        public static string GenerarCorreoUsuarioAutNivel(ProductoAdjudicadoDTO dto, string nombre)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; background-color: #f9f9f9; color: #333; padding: 20px; }");
            sb.AppendLine(".container { background-color: #ffffff; border: 1px solid #ddd; border-radius: 8px; padding: 30px; max-width: 800px; margin: auto; }");
            sb.AppendLine("h2 { color: #004080; margin-bottom: 10px; }");
            sb.AppendLine("p { font-size: 15px; line-height: 1.6; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 14px; }");
            sb.AppendLine("th { background-color: #004080; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { border: 1px solid #ccc; padding: 8px; }");
            sb.AppendLine(".footer { margin-top: 30px; font-size: 13px; color: #666; }");
            sb.AppendLine(".firma { margin-top: 20px; }");
            sb.AppendLine("</style></head><body><div class='container'>");

            sb.AppendLine($"<h2>📢 Autorizar – Requisición {dto.RequisicionId}</h2>");
            sb.AppendLine($"<p>Estimado/a usuario<strong>{nombre}</strong>,</p>");
            sb.AppendLine("<p>Nos complace informarle que ha sido seleccionado como Autorizador de la Requisicion que a continuación se detallan los productos Solicitados:</p>");

            sb.AppendLine("<table><thead><tr>");
            sb.AppendLine("<th>Posición</th><th>Código</th><th>Descripción</th><th>Unidad</th><th>Cantidad</th><th>Precio</th><th>Cond. Pago</th><th>Entrega</th><th>Cargo Ext.</th><th>Moneda</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var p in dto.Productos)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{p.Posicion}</td>");
                sb.AppendLine($"<td>{p.CodigoMaterial}</td>");
                sb.AppendLine($"<td>{p.Descripcion}</td>");
                sb.AppendLine($"<td>{p.Unidad}</td>");
                sb.AppendLine($"<td>{p.Cantidad}</td>");
                sb.AppendLine($"<td>{p.PrecioUnitario}</td>");
                sb.AppendLine($"<td>{p.CondicionPago}</td>");
                sb.AppendLine($"<td>{p.FechaEntrega}</td>");
                sb.AppendLine($"<td>{p.CargoExterno}</td>");
                sb.AppendLine($"<td>{p.Moneda}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<p class='firma'>le solicitamos este pendiente de acuerdo a su nivel. Agradecemos su participación y quedamos atentos a cualquier consulta.</p>");

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Atentamente,</p>");
            sb.AppendLine("<p><strong>Francisco</strong><br>Coordinador de Licitaciones<br>Tu Empresa S.A.<br>📞 +52 55 1234 5678<br>✉️ francisco@tuempresa.com</p>");
            sb.AppendLine("</div></div></body></html>");

            return sb.ToString();
        }



    }
}
