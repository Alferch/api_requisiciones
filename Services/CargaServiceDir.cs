using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using System.Data;
using System.Text;


 
using System.Data.SqlClient;
using System.Globalization;
 
 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
 
using System.Threading.Tasks;

namespace RequisicionesApi.Services
{

    public   class CargaServiceDir : ICargaServiceDir
    {

        private readonly string _connString;
        private readonly ExcelOptions _excelOptions;

        public CargaServiceDir(IConfiguration config, IOptions<ExcelOptions> excelOptions)
        {
            _connString = config.GetConnectionString("DefaultConnection")!;
            _excelOptions = excelOptions.Value ?? new ExcelOptions();
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static bool TryParseDateDdMmYyyy(object dateObj, out DateTime value)
        {
            value = default;

            if (dateObj == null || dateObj == DBNull.Value) return false;

            if (dateObj is DateTime dt)
            {
                value = dt;
                return true;
            }

            var s = dateObj.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;

            // Formato exacto dd/MM/yyyy
            if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private static string? ToStringOrNull(object o)
    => o == null || o == DBNull.Value ? null : o.ToString();

        /// <summary>
        /// Carga Excel a dbo.tbldirecciones usando SqlBulkCopy.
        /// Valida: extensión, tamaño, headers EXACTOS y orden, longitudes, obligatoriedad, dígitos y fecha dd/MM/yyyy.
        /// </summary>
        public async Task<UploadResult> UploadExcelDireccionesBulkAsync(IFormFile file)
        {
            var result = new UploadResult { Opcion = 3 }; // usa el número que te convenga

            // 1) Validación de archivo: solo .xlsx
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                result.Errores.Add(new RowError { RowNumber = 0, Message = "El archivo debe ser .xlsx." });
                result.Rechazados++;
                return result;
            }

            // 2) Validación de tamaño máximo
            var maxBytes = _excelOptions.MaxSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"El archivo excede {_excelOptions.MaxSizeMB} MB." });
                result.Rechazados++;
                return result;
            }

            // 3) Leer Excel (primera hoja, UseHeaderRow = true)
            DataTable table;
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                if (ds.Tables.Count == 0)
                    throw new InvalidOperationException("El Excel no contiene hojas.");

                table = ds.Tables[0]; // primera hoja
            }
            catch (Exception ex)
            {
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"No se pudo leer el Excel: {ex.Message}" });
                result.Rechazados++;
                return result;
            }

            // 4) Validar cabeceras presentes y orden exacto (EXCLUIMOS IdDir por ser IDENTITY)
            var expectedHeaders = ExpectedHeadersDirecciones();
            var actualHeaders = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName.Trim()).ToList();

            if (actualHeaders.Count != expectedHeaders.Count ||
                !actualHeaders.SequenceEqual(expectedHeaders, StringComparer.Ordinal))
            {
                result.Errores.Add(new RowError
                {
                    RowNumber = 0,
                    Message = $"Las cabeceras deben coincidir EXACTAMENTE y en el mismo orden. " +
                              $"Esperado: {string.Join(", ", expectedHeaders)}. " +
                              $"Actual: {string.Join(", ", actualHeaders)}"
                });
                result.Rechazados++;
                return result;
            }

            // 5) Construir DataTable para bulk insert (solo filas válidas)
            var dtBulk = BuildDireccionesBulkDataTable();

            int rowNumber = 1; // encabezado en fila 1; datos inician en 2
            foreach (DataRow row in table.Rows)
            {
                rowNumber++;

                // Validar según esquema
                var val = ValidateRowDirecciones(row);
                if (!val.valid)
                {
                    result.Errores.Add(new RowError { RowNumber = rowNumber, Message = val.error! });
                    result.Rechazados++;
                    continue;
                }

                // Parseo de fecha
                var dateObj = row["FechaCreacionDir"];
                if (!TryParseDateDdMmYyyy(dateObj, out var fecha))
                {
                    result.Errores.Add(new RowError { RowNumber = rowNumber, Message = "FechaCreacionDir con formato inválido (esperado dd/MM/yyyy) o vacía." });
                    result.Rechazados++;
                    continue;
                }

                // Agregar fila válida al DataTable de bulk
                dtBulk.Rows.Add(
                    ToStringOrNull(row["CodigoPostalDir"])?.Trim(),
                    ToStringOrNull(row["CveColoniaDir"])?.Trim(),
                    ToStringOrNull(row["NombreColoniaDir"])?.Trim(),
                    ToStringOrNull(row["CveMunicipioDir"])?.Trim(),
                    ToStringOrNull(row["NombreMunicipioDir"])?.Trim(),
                    ToStringOrNull(row["CveLocalidadDir"])?.Trim(),
                    ToStringOrNull(row["NombreLocalidadDir"])?.Trim(),
                    ToStringOrNull(row["CveEstadoDir"])?.Trim(),
                    ToStringOrNull(row["NombreEstadoDir"])?.Trim(),
                    fecha,
                    ToStringOrNull(row["UsuarioCreacionDir"])?.Trim()
                );
            }

            if (dtBulk.Rows.Count == 0)
            {
                // No hay filas válidas
                return result;
            }

            // 6) Bulk insert con transacción
            using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                using var bulk = new SqlBulkCopy(conn,
                    SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.KeepNulls, tx)
                {
                    DestinationTableName = "dbo.tbldirecciones",
                    BatchSize = 5000,
                    BulkCopyTimeout = 0,   // sin límite; ajusta si quieres (segundos)
                    EnableStreaming = true
                };

                // Mapeo de columnas (nombre a nombre)
                bulk.ColumnMappings.Add("CodigoPostalDir", "CodigoPostalDir");
                bulk.ColumnMappings.Add("CveColoniaDir", "CveColoniaDir");
                bulk.ColumnMappings.Add("NombreColoniaDir", "NombreColoniaDir");
                bulk.ColumnMappings.Add("CveMunicipioDir", "CveMunicipioDir");
                bulk.ColumnMappings.Add("NombreMunicipioDir", "NombreMunicipioDir");
                bulk.ColumnMappings.Add("CveLocalidadDir", "CveLocalidadDir");
                bulk.ColumnMappings.Add("NombreLocalidadDir", "NombreLocalidadDir");
                bulk.ColumnMappings.Add("CveEstadoDir", "CveEstadoDir");
                bulk.ColumnMappings.Add("NombreEstadoDir", "NombreEstadoDir");
                bulk.ColumnMappings.Add("FechaCreacionDir", "FechaCreacionDir");
                bulk.ColumnMappings.Add("UsuarioCreacionDir", "UsuarioCreacionDir");

                await bulk.WriteToServerAsync(dtBulk);

                tx.Commit();

                result.Insertados += dtBulk.Rows.Count;
            }
            catch (SqlException sqlEx)
            {
                tx.Rollback();
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"SQL Bulk error: {sqlEx.Message}" });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"Error general: {ex.Message}" });
            }

            return result;
        }

        /// <summary>
        /// Cabeceras esperadas EXACTAS y en orden para tbldirecciones (sin IdDir porque es IDENTITY).
        /// </summary>
        private static List<string> ExpectedHeadersDirecciones() => new()
        {
            "CodigoPostalDir",
            "CveColoniaDir",
            "NombreColoniaDir",
            "CveMunicipioDir",
            "NombreMunicipioDir",
            "CveLocalidadDir",
            "NombreLocalidadDir",
            "CveEstadoDir",
            "NombreEstadoDir",
            "FechaCreacionDir",
            "UsuarioCreacionDir"
        };

        /// <summary>
        /// DataTable con el esquema para Bulk Copy (tipos acordes al destino).
        /// </summary>
        private static DataTable BuildDireccionesBulkDataTable()
        {
            var dt = new DataTable("tbldirecciones");
            dt.Columns.Add("CodigoPostalDir", typeof(string));
            dt.Columns.Add("CveColoniaDir", typeof(string));
            dt.Columns.Add("NombreColoniaDir", typeof(string));
            dt.Columns.Add("CveMunicipioDir", typeof(string));
            dt.Columns.Add("NombreMunicipioDir", typeof(string));
            dt.Columns.Add("CveLocalidadDir", typeof(string));
            dt.Columns.Add("NombreLocalidadDir", typeof(string));
            dt.Columns.Add("CveEstadoDir", typeof(string));
            dt.Columns.Add("NombreEstadoDir", typeof(string));
            dt.Columns.Add("FechaCreacionDir", typeof(DateTime)); // DateTime2 en SQL
            dt.Columns.Add("UsuarioCreacionDir", typeof(string));
            return dt;
        }


        private static (bool valid, string? error) ValidateStrings(List<(string name, object value, int maxLen, bool nullable)> fields)
        {
            foreach (var (name, value, maxLen, nullable) in fields)
            {
                var s = ToStringOrNull(value)?.Trim();

                if (string.IsNullOrEmpty(s))
                {
                    if (!nullable)
                        return (false, $"{name} es requerido.");
                    else
                        continue;
                }

                if (maxLen != int.MaxValue && s.Length > maxLen)
                    return (false, $"{name} excede longitud máxima ({maxLen}).");
            }
            return (true, null);
        }

        /// <summary>
        /// Validaciones de tamaño, obligatoriedad y dígitos exactos según el esquema.
        /// </summary>
        private static (bool valid, string? error) ValidateRowDirecciones(DataRow r)
        {
            // Validaciones de longitud y nulls
            var stringsValidation = ValidateStrings(new()
            {
                ("CodigoPostalDir",   r["CodigoPostalDir"],   5,   false),  // NOT NULL
                ("CveColoniaDir",     r["CveColoniaDir"],     10,  true),
                ("NombreColoniaDir",  r["NombreColoniaDir"],  150, true),
                ("CveMunicipioDir",   r["CveMunicipioDir"],   3,   true),
                ("NombreMunicipioDir",r["NombreMunicipioDir"],150, true),
                ("CveLocalidadDir",   r["CveLocalidadDir"],   4,   true),
                ("NombreLocalidadDir",r["NombreLocalidadDir"],150, true),
                ("CveEstadoDir",      r["CveEstadoDir"],      2,   false),  // NOT NULL
                ("NombreEstadoDir",   r["NombreEstadoDir"],   100, false),  // NOT NULL
                ("UsuarioCreacionDir",r["UsuarioCreacionDir"],100, true)
            });
            if (!stringsValidation.valid) return stringsValidation;

            // Dígitos exactos para claves numéricas (si existen)
            if (!IsExactlyDigits(r["CodigoPostalDir"], 5, nullable: false))
                return (false, "CodigoPostalDir debe contener exactamente 5 dígitos (00000-99999).");

            if (!IsExactlyDigits(r["CveEstadoDir"], 2, nullable: false))
                return (false, "CveEstadoDir debe contener exactamente 2 dígitos (01-32).");

            if (!IsExactlyDigits(r["CveMunicipioDir"], 3, nullable: true))
                return (false, "CveMunicipioDir debe contener exactamente 3 dígitos si se especifica.");

            if (!IsExactlyDigits(r["CveLocalidadDir"], 4, nullable: true))
                return (false, "CveLocalidadDir debe contener exactamente 4 dígitos si se especifica.");

            // FechaCreacionDir: se valida después con TryParseDateDdMmYyyy
            return (true, null);
        }

        private static bool IsExactlyDigits(object value, int len, bool nullable)
        {
            var s = ToStringOrNull(value)?.Trim();
            if (string.IsNullOrEmpty(s)) return nullable;
            if (s.Length != len) return false;
            for (int i = 0; i < s.Length; i++)
                if (!char.IsDigit(s[i])) return false;
            return true;
        }
    }

}
