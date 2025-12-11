
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;




namespace RequisicionesApi.Services
{
    public class CargaService : ICargaService
    {

        private readonly string _connString;
        private readonly ExcelOptions _excelOptions;

        public CargaService(IConfiguration config, IOptions<ExcelOptions> excelOptions)
        {
            _connString = config.GetConnectionString("DefaultConnection")!;
            _excelOptions = excelOptions.Value ?? new ExcelOptions();
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }





        public async Task<UploadResult> UploadExcelAsync(IFormFile file, int opcion)
        {
            var result = new UploadResult { Opcion = opcion };

            // Validación de archivo: solo .xlsx
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                result.Errores.Add(new RowError { RowNumber = 0, Message = "El archivo debe ser .xlsx." });
                result.Rechazados++;
                return result;
            }

            var maxBytes = _excelOptions.MaxSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"El archivo excede {_excelOptions.MaxSizeMB} MB." });
                result.Rechazados++;
                return result;
            }

            // Leer Excel (primera hoja, UseHeaderRow = true)
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

            // Cabeceras esperadas (SIN acentos) y orden exacto
            var expectedHeaders = opcion == 1 ? ExpectedHeadersProveedores() : ExpectedHeadersMaestroMaterial();

            //if (opcion == 1) { ExpectedHeadersProveedores(); } else if (opcion == 2) { ExpectedHeadersMaestroMaterial(); } else { }

                // Validar cabeceras presentes y orden exacto
                var actualHeaders = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName.Trim()).ToList();

            if (actualHeaders.Count != expectedHeaders.Count ||
                !actualHeaders.SequenceEqual(expectedHeaders, StringComparer.Ordinal))
            {
                result.Errores.Add(new RowError
                {
                    RowNumber = 0,
                    Message = $"Las cabeceras deben coincidir EXACTAMENTE y en el mismo orden. Esperado: {string.Join(", ", expectedHeaders)}. Actual: {string.Join(", ", actualHeaders)}"
                });
                result.Rechazados++;
                return result;
            }

            using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                int rowNumber = 1; // encabezado en fila 1; datos inician en 2
                foreach (DataRow row in table.Rows)
                {
                    rowNumber++;

                    if (opcion == 1)
                    {
                        var val = ValidateRowProveedores(row);
                        if (!val.valid)
                        {
                            result.Errores.Add(new RowError { RowNumber = rowNumber, Message = val.error! });
                            result.Rechazados++;
                            continue;
                        }

                        using var cmd = new SqlCommand(@"
INSERT INTO [dbo].[tblProveedores]
([provIdSoc],[provIdGrupoM],[provIdProv],[provNombre],[provRFC],[provNomVendedor],[provTeléfono],[provCorreo],[provIdioma],[ProvClasificacion])
VALUES (@provIdSoc,@provIdGrupoM,@provIdProv,@provNombre,@provRFC,@provNomVendedor,@provTelefono,@provCorreo,@provIdioma,@ProvClasificacion);", conn, tx);

                        // Parámetros (usamos header sin acentos; mapeamos a columna con acento en SQL)
                        AddNVarChar(cmd, "@provIdSoc", 10, row["provIdSoc"]);
                        AddNVarChar(cmd, "@provIdGrupoM", 10, row["provIdGrupoM"]);
                        AddNVarChar(cmd, "@provIdProv", 10, row["provIdProv"]);
                        AddNVarChar(cmd, "@provNombre", 100, row["provNombre"]);
                        AddNVarChar(cmd, "@provRFC", 20, row["provRFC"]);
                        AddNVarChar(cmd, "@provNomVendedor", 100, row["provNomVendedor"]);
                        AddNVarChar(cmd, "@provTelefono", 15, row["provTelefono"]); // header sin acento
                        AddNVarChar(cmd, "@provCorreo", 50, row["provCorreo"]);
                        AddNChar(cmd, "@provIdioma", 1, row["provIdioma"]); // puede ser null
                        AddNChar(cmd, "@ProvClasificacion", 1, row["ProvClasificacion"]); // puede ser null

                        try
                        {
                            await cmd.ExecuteNonQueryAsync();
                            result.Insertados++;
                        }
                        catch (SqlException sqlEx)
                        {
                            result.Errores.Add(new RowError { RowNumber = rowNumber, Message = $"SQL error: {sqlEx.Message}" });
                            result.Rechazados++;
                        }
                    }
                    else // opcion == 2
                    {
                        var val = ValidateRowMaestroMaterial(row);
                        if (!val.valid)
                        {
                            result.Errores.Add(new RowError { RowNumber = rowNumber, Message = val.error! });
                            result.Rechazados++;
                            continue;
                        }

                        using var cmd = new SqlCommand(@"
INSERT INTO [dbo].[tblMaestroMaterial]
([mmatIdClave],[mmatIdSoc],[mmatIdCompleto],[mmatDescripción],[mmatTipoM],[mmatIdGrupoM],[mmatUnidadMedida],[mmatMoneda],[mmatPrecioMM],[mmatExistencia],[mmatUltimopedido],[mmatFechaultpedido],[mmatEstado],[mmatEspecificaciones])
VALUES (@mmatIdClave,@mmatIdSoc,@mmatIdCompleto,@mmatDescripcion,@mmatTipoM,@mmatIdGrupoM,@mmatUnidadMedida,@mmatMoneda,@mmatPrecioMM,@mmatExistencia,@mmatUltimopedido,@mmatFechaultpedido,@mmatEstado,@mmatEspecificaciones);", conn, tx);

                        AddNVarChar(cmd, "@mmatIdClave", 18, row["mmatIdClave"]);
                        AddNVarChar(cmd, "@mmatIdSoc", 4, row["mmatIdSoc"]);
                        AddNVarChar(cmd, "@mmatIdCompleto", 18, row["mmatIdCompleto"]);
                        AddNVarChar(cmd, "@mmatDescripcion", 40, row["mmatDescripcion"]); // header sin acento
                        AddNVarChar(cmd, "@mmatTipoM", 4, row["mmatTipoM"]);
                        AddNVarChar(cmd, "@mmatIdGrupoM", 9, row["mmatIdGrupoM"]);
                        AddNVarChar(cmd, "@mmatUnidadMedida", 3, row["mmatUnidadMedida"]);
                        AddNVarChar(cmd, "@mmatMoneda", 3, row["mmatMoneda"]);
                        AddNVarChar(cmd, "@mmatPrecioMM", 14, row["mmatPrecioMM"]);
                        AddNVarCharNullable(cmd, "@mmatExistencia", 14, row["mmatExistencia"]);
                        AddNVarCharNullable(cmd, "@mmatUltimopedido", 10, row["mmatUltimopedido"]);

                        // Fecha obligatoria: formato dd/MM/yyyy o DateTime nativo
                        var dateObj = row["mmatFechaultpedido"];
                        if (!TryParseDateDdMmYyyy(dateObj, out var fecha))
                        {
                            result.Errores.Add(new RowError { RowNumber = rowNumber, Message = "mmatFechaultpedido con formato inválido (esperado dd/MM/yyyy)." });
                            result.Rechazados++;
                            continue;
                        }
                        cmd.Parameters.Add("@mmatFechaultpedido", SqlDbType.SmallDateTime).Value = fecha;

                        AddNVarChar(cmd, "@mmatEstado", 1, row["mmatEstado"]);
                        AddNVarCharNullable(cmd, "@mmatEspecificaciones", -1, row["mmatEspecificaciones"]); // nvarchar(max)

                        try
                        {
                            await cmd.ExecuteNonQueryAsync();
                            result.Insertados++;
                        }
                        catch (SqlException sqlEx)
                        {
                            result.Errores.Add(new RowError { RowNumber = rowNumber, Message = $"SQL error: {sqlEx.Message}" });
                            result.Rechazados++;
                        }
                    }
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                result.Errores.Add(new RowError { RowNumber = 0, Message = $"Error general: {ex.Message}" });
            }

            return result;
        }

        // Cabeceras esperadas (SIN acentos)
        private static List<string> ExpectedHeadersProveedores() => new()
    {
        "provIdSoc","provIdGrupoM","provIdProv","provNombre","provRFC","provNomVendedor",
        "provTelefono","provCorreo","provIdioma","ProvClasificacion"
    };

        private static List<string> ExpectedHeadersMaestroMaterial() => new()
    {
        "mmatIdClave","mmatIdSoc","mmatIdCompleto","mmatDescripcion","mmatTipoM","mmatIdGrupoM",
        "mmatUnidadMedida","mmatMoneda","mmatPrecioMM","mmatExistencia","mmatUltimopedido",
        "mmatFechaultpedido","mmatEstado","mmatEspecificaciones"
    };

        // Validaciones contra el esquema que diste
        private static (bool valid, string? error) ValidateRowProveedores(DataRow r)
        {
            return ValidateStrings(new()
        {
            ("provIdSoc", r["provIdSoc"], 10, false),
            ("provIdGrupoM", r["provIdGrupoM"], 10, false),
            ("provIdProv", r["provIdProv"], 10, false),
            ("provNombre", r["provNombre"], 100, false),
            ("provRFC", r["provRFC"], 20, false),
            ("provNomVendedor", r["provNomVendedor"], 100, false),
            ("provTelefono", r["provTelefono"], 15, false), // sin acento en header
            ("provCorreo", r["provCorreo"], 50, false),
            ("provIdioma", r["provIdioma"], 1, true),        // nchar(1) NULL
            ("ProvClasificacion", r["ProvClasificacion"], 1, true) // nchar(1) NULL
        });
        }

        private static (bool valid, string? error) ValidateRowMaestroMaterial(DataRow r)
        {
            var stringsValidation = ValidateStrings(new()
        {
            ("mmatIdClave", r["mmatIdClave"], 18, false),
            ("mmatIdSoc", r["mmatIdSoc"], 4, false),
            ("mmatIdCompleto", r["mmatIdCompleto"], 18, false),
            ("mmatDescripcion", r["mmatDescripcion"], 40, false),
            ("mmatTipoM", r["mmatTipoM"], 4, false),
            ("mmatIdGrupoM", r["mmatIdGrupoM"], 9, false),
            ("mmatUnidadMedida", r["mmatUnidadMedida"], 3, false),
            ("mmatMoneda", r["mmatMoneda"], 3, false),
            ("mmatPrecioMM", r["mmatPrecioMM"], 14, false),
            ("mmatExistencia", r["mmatExistencia"], 14, true),
            ("mmatUltimopedido", r["mmatUltimopedido"], 10, true),
            ("mmatEstado", r["mmatEstado"], 1, false),
            ("mmatEspecificaciones", r["mmatEspecificaciones"], int.MaxValue, true) // nvarchar(max)
        });
            if (!stringsValidation.valid) return stringsValidation;

            if (!TryParseDateDdMmYyyy(r["mmatFechaultpedido"], out _))
                return (false, "mmatFechaultpedido con formato inválido o vacío (dd/MM/yyyy).");

            return (true, null);
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

        private static string? ToStringOrNull(object o)
            => o == null || o == DBNull.Value ? null : o.ToString();

        /// <summary>
        /// Acepta DateTime nativo o cadena en formato exacto dd/MM/yyyy.
        /// </summary>
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

        // Helpers para parámetros
        private static void AddNVarChar(SqlCommand cmd, string name, int size, object value)
        {
            var s = ToStringOrNull(value)?.Trim();
            var p = cmd.Parameters.Add(name, SqlDbType.NVarChar, size);
            p.Value = s ?? throw new ArgumentNullException(name);
        }

        private static void AddNVarCharNullable(SqlCommand cmd, string name, int size, object value)
        {
            var s = ToStringOrNull(value)?.Trim();
            var p = cmd.Parameters.Add(name, SqlDbType.NVarChar, size == -1 ? -1 : size);
            p.Value = string.IsNullOrEmpty(s) ? DBNull.Value : s!;
        }

        private static void AddNChar(SqlCommand cmd, string name, int size, object value)
        {
            var s = ToStringOrNull(value)?.Trim();
            var p = cmd.Parameters.Add(name, SqlDbType.NChar, size);
            p.Value = string.IsNullOrEmpty(s) ? DBNull.Value : s!;
        }





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
                //Console.Write(rowNumber);
                // Validar según esquema
                var val = ValidateRowDirecciones(row);
                if (!val.valid)
                {
                    result.Errores.Add(new RowError { RowNumber = rowNumber, Message = val.error! });
                    result.Rechazados++;
                    Console.Write(result.Rechazados);
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
           // Console.Write("bulk...................................................................");
            // 6) Bulk insert con transacción
            using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            using var cmdd = new SqlCommand(@"delete  [dbo].[tbldirecciones]", conn, tx);
            await cmdd.ExecuteNonQueryAsync();

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
              //  Console.Write("bulk");
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
               // Console.Write("bulk commit /n");
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
                ("CveEstadoDir",      r["CveEstadoDir"],      3,   false),  // NOT NULL
                ("NombreEstadoDir",   r["NombreEstadoDir"],   100, false),  // NOT NULL
                ("UsuarioCreacionDir",r["UsuarioCreacionDir"],100, true)
            });
            if (!stringsValidation.valid) return stringsValidation;

            // Dígitos exactos para claves numéricas (si existen)
            if (!IsExactlyDigits(r["CodigoPostalDir"], 5, nullable: false))
                return (false, "CodigoPostalDir debe contener exactamente 5 dígitos (00000-99999).");

//            if (!IsExactlyDigits(r["CveEstadoDir"], 3, nullable: false))
//                return (false, "CveEstadoDir debe contener exactamente 3 dígitos (01-32).");

            if (!IsExactlyDigits(r["CveMunicipioDir"], 3, nullable: true))
                return (false, "CveMunicipioDir debe contener exactamente 3 dígitos si se especifica.");

            if (!IsExactlyDigits(r["CveLocalidadDir"], 2, nullable: true))
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
