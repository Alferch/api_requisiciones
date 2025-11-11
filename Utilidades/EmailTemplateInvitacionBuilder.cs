
using RequisicionesApi.Dtos;
using System.Globalization;
using System.Net;
using System.Text;

namespace RequisicionesApi.Utilidades
{


    /// Paquete de salida por proveedor
    public sealed record EmailPackage(string Prov, string Subject, string Html);

    /// Clave fuerte para agrupar por (reqIdClave, reqdIdSoc)
    public readonly record struct ReqSocKey(string ReqIdClave, string? ReqdIdSoc);

    public static class EmailTemplateBuilder
    {
        /// Genera un HTML por proveedor (prov). No asume valores externos.
        public static List<EmailPackage> BuildCotizacionPapeleriaHtmlPerProveedor(
            IEnumerable<AutProvPosicionList> data,
            string empresa,
            string area,
            string remitenteNombre,
            string remitenteCargo,
            string remitenteTelefono,
            string remitenteCorreo)
        {
            ArgumentNullException.ThrowIfNull(data);
            var esMX = CultureInfo.GetCultureInfo("es-MX");
            var result = new List<EmailPackage>();

            // Agrupar por proveedor (case-insensitive)
            var porProveedor = data.GroupBy(x => x.prov ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in porProveedor)
            {
                var prov = grp.Key ?? string.Empty;

                // Ids y sociedades únicas en el grupo del proveedor
                var reqIds = grp.Select(x => x.reqIdClave)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Distinct(StringComparer.Ordinal)
                                .ToList();

                var socs = grp.Select(x => x.reqdIdSoc)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct(StringComparer.Ordinal)
                              .ToList();

                var subject = $"Invitación a cotizar –  {JoinOr(reqIds)} – {JoinOr(socs)}";

                var sb = new StringBuilder();
                sb.AppendLine("<!doctype html>");
                sb.AppendLine("<html lang=\"es\">");
                sb.AppendLine("<head>");
                sb.AppendLine("<meta charset=\"utf-8\"/>");
                sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
                sb.AppendLine("<title>Invitación a participar de la licitacion  </title>");
                sb.AppendLine("""
<style>
  body { font-family: Arial, Helvetica, sans-serif; color:#1b1b1b; margin:0; padding:0; }
  .container { max-width: 760px; margin: 0 auto; padding: 24px; }
  .card { border:1px solid #e5e7eb; border-radius:12px; padding:24px; }
  h1 { font-size: 20px; margin: 0 0 12px 0; }
  h2 { font-size: 16px; margin: 24px 0 8px 0; }
  p  { margin: 8px 0; line-height: 1.45; }
  .muted { color:#6b7280; font-size: 12px; }
  .chip { display:inline-block; background:#f3f4f6; border:1px solid #e5e7eb; border-radius:999px; padding:4px 10px; font-size:12px; margin-right:8px; }
  table { border-collapse: collapse; width: 100%; margin-top: 8px; }
  th, td { border:1px solid #e5e7eb; padding:8px; text-align:left; font-size:13px; vertical-align: top; }
  th { background:#f9fafb; }
  .footer { margin-top:24px; font-size: 13px; }
  .small { font-size: 12px; }
  .divider { height:1px; background:#e5e7eb; margin:24px 0; }
</style>
""");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("<div class=\"container\"><div class=\"card\">");

                sb.AppendLine("<h1>Invitación a cotizar – Requisición de papelería</h1>");
                sb.AppendLine($"<p>Estimado(a) {Enc(prov)}:</p>");
                sb.AppendLine($"<p>{Enc(empresa)} / {Enc(area)} le invita a presentar cotización para la requisición de papelería {Enc(string.Join(", ", reqIds))}.</p>");

                if (reqIds.Count > 0)
                    sb.AppendLine($"<p class=\"muted\">Requisición(es): {string.Join(" ", reqIds.Select(r => $"<span class=\"chip\">{Enc(r)}</span>"))}</p>");
                if (socs.Count > 0)
                    sb.AppendLine($"<p class=\"muted\">Sociedad(es): {string.Join(" ", socs.Select(s => $"<span class=\"chip\">{Enc(s ?? string.Empty)}</span>"))}</p>");

                // Agrupar por (ReqIdClave, ReqdIdSoc) – clave tipada
                var porReqSoc = grp.GroupBy(x => new ReqSocKey(x.reqIdClave ?? string.Empty, x.reqdIdSoc));

                foreach (var rs in porReqSoc)
                {
                    var headerReq = string.IsNullOrWhiteSpace(rs.Key.ReqIdClave) ? "(Sin ID Requisición)" : rs.Key.ReqIdClave;
                    var headerSoc = string.IsNullOrWhiteSpace(rs.Key.ReqdIdSoc) ? "(Sin Sociedad)" : rs.Key.ReqdIdSoc;

                    sb.AppendLine("<div class=\"divider\"></div>");
                    sb.AppendLine($"<h2>Partidas – Requisición {Enc(headerReq)} – Sociedad {Enc(headerSoc)}</h2>");

                    // Unimos todas las partidas de ese (req,soc)
                    var filas = rs.SelectMany(x => x.Modelos ?? Enumerable.Empty<AutProvRequisicionNot>())
                                  .OrderBy(m => m.reqidposNo)
                                  .ToList();

                    sb.AppendLine("<table role=\"table\" aria-label=\"Partidas de papelería\">");
                    sb.AppendLine("<thead><tr>");
                    sb.AppendLine("<th>Pos</th><th>Material</th><th>Descripción</th><th>Cantidad</th><th>U.M.</th><th>Fec. Entrega</th><th>Ciudad</th><th>Municipio</th>");
                    sb.AppendLine("</tr></thead><tbody>");

                    if (filas.Count == 0)
                    {
                        sb.AppendLine("<tr><td colspan=\"8\" class=\"small\">(Sin partidas)</td></tr>");
                    }
                    else
                    {
                        foreach (var m in filas)
                        {
                            var fec = m.reqdFecEntrega == default ? "" : m.reqdFecEntrega.ToString("yyyy-MM-dd", esMX);
                            sb.AppendLine("<tr>");
                            sb.AppendLine($"<td>{m.reqidposNo}</td>");
                            sb.AppendLine($"<td>{Enc(m.reqdpMatNo)}</td>");
                            sb.AppendLine($"<td>{Enc(m.reqdpMatDes)}</td>");
                            sb.AppendLine($"<td>{m.reqdCantidad}</td>");
                            sb.AppendLine($"<td>{Enc(m.reqdUnidadMed ?? string.Empty)}</td>");
                            sb.AppendLine($"<td>{fec}</td>");
                            sb.AppendLine($"<td>{Enc(m.reqdCiudad ?? string.Empty)}</td>");
                            sb.AppendLine($"<td>{Enc(m.reqdMunicipio ?? string.Empty)}</td>");
                            sb.AppendLine("</tr>");
                        }
                    }

                    sb.AppendLine("</tbody></table>");
                }

                // Nota + instrucciones de envío
                sb.AppendLine("<p class=\"small\" style=\"margin-top:16px;\">Se aceptan equivalentes con fichas técnicas y evidencia de equivalencia.</p>");
                var primerReq = reqIds.FirstOrDefault() ?? "ID Req";
                sb.AppendLine($"<p><strong>Entrega de cotización:</strong> Enviar por este medio a <a href=\"mailto:{Enc(remitenteCorreo)}\">{Enc(remitenteCorreo)}</a> con asunto:<br/>");
                sb.AppendLine("<p class=\"small\" style=\"margin-top:16px;\">si esta interesado en participar, favor de confirmar por este medio y le sera enviado un usuario y clave para ingresar a la plataforma y capturar su propuesta</p>");
                sb.AppendLine($"“Cotización   {Enc(primerReq)} – {Enc(prov)}”.</p>");

                // Firma
                sb.AppendLine("<div class=\"footer\">");
                sb.AppendLine("<p>Quedo atento(a) a cualquier duda.</p>");
                sb.AppendLine($"""
<p>Saludos cordiales,<br/>
<strong>{Enc(remitenteNombre)}</strong> – {Enc(remitenteCargo)}<br/>
{Enc(empresa)} | {Enc(remitenteTelefono)} | <a href="mailto:{Enc(remitenteCorreo)}">{Enc(remitenteCorreo)}</a></p>
""");
                sb.AppendLine("</div>");

                sb.AppendLine("</div></div></body></html>");

                result.Add(new EmailPackage(prov, subject, sb.ToString()));
            }

            return result;

            // Helpers locales
            static string Enc(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

            static string JoinOr(IEnumerable<string?> vals)
            {
                var list = vals.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal).ToList();
                if (list.Count == 0) return "(s/d)";
                if (list.Count == 1) return list[0]!;
                if (list.Count == 2) return $"{list[0]} y {list[1]}";
                var last = list[^1];
                return string.Join(", ", list.Take(list.Count - 1)) + " y " + last;
            }
        }
    }




}
