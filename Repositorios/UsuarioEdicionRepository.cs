using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Utilidades;
using System.Data;

public class UsuarioEdicionRepository : IUsuarioEdicionRepository
{
    private readonly IConfiguration _config;
    public UsuarioEdicionRepository(IConfiguration config) => _config = config;
    private SqlConnection CreateConnection() => new(_config.GetConnectionString("DefaultConnection"));

    public async Task<UsuarioEdicionDto> GetAsync(string usrIdClave)
    {
        var result = new UsuarioEdicionDto();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        // Consultar tblUsuario
        using var cmdUsr = new SqlCommand("SELECT usrIdClave, usrIdSoc, usrApellidoP, usrApellidoM,   usrPuesto, usrCorreo, usrNombre, PasswordHash FROM tblUsuario WHERE usrIdClave = @clave", conn);
        cmdUsr.Parameters.AddWithValue("@clave", usrIdClave);
        using var reader = await cmdUsr.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result.Usuario = new UsuarioDto
            {
                UsrIdClave = reader.GetString(0),
                UsrIdSoc = reader.GetString(1),
                UsrApellidoP = reader.GetString(2),
                UsrApellidoM = reader.IsDBNull(3) ? null : reader.GetString(3),
 
                UsrPuesto = reader.GetString(4),
                UsrCorreo = reader.GetString(5),
                UsrNombre = reader.GetString(6),
                PasswordHash = reader.IsDBNull(7) ? null :  reader.GetString(7)
            };
        }
        reader.Close();

        // Consultar tblUsrRolPer + tblRoles
        using var cmdRol = new SqlCommand(@"SELECT p.usrIdRol, r.rolNombre, p.usrPerAlta, p.usrPerUpdate, p.usrPerDel,
            p.usrPerAut, p.usrPerLic, p.usrPerReq, p.usrPercambio, p.usrPerVoBo, p.usrPerCompara
            FROM tblUsrRolPer p
            JOIN tblRoles r ON r.rolIdRol = p.usrIdRol
            WHERE p.usrIdClave = @clave", conn);
        cmdRol.Parameters.AddWithValue("@clave", usrIdClave);
        result.Usuario.RolesPermisos = new List<UsuarioRolPermisosDto>();
        //RolesPermisos = new List<UsuarioRolPermisosDto>()
        using var readerRol = await cmdRol.ExecuteReaderAsync();
        while (await readerRol.ReadAsync())
        {
            var rol =new UsuarioRolPermisosDto
            {
                UsrIdRol = readerRol.GetString(0),
                RolNombre = readerRol.GetString(1),
                UsrPerAlta = readerRol.GetString(2),
                UsrPerUpdate = readerRol.GetString(3),
                UsrPerDel = readerRol.GetString(4),
                UsrPerAut = readerRol.GetString(5),
                UsrPerLic = readerRol.GetString(6),
                UsrPerReq = readerRol.GetString(7),
                Configuracion = readerRol.GetString(8),
                UsrPerVoBo = readerRol.GetString(9),
                UsrPerCompara = readerRol.GetString(10)
            };
            result.Usuario.RolesPermisos.Add(rol);
        }
        readerRol.Close();


        foreach (var rol in result.Usuario.RolesPermisos)
        {


            // Consultar tblUsrImputacion + subárea y área
            using var cmdImp = new SqlCommand(@"SELECT i.usrIdImp, i.usrIdRol, iv.impIdSArea, sa.arNombre,
            sa.sarIdArea, a.arNombre, i.usrNivel
            FROM tblUsrImputacion i
            LEFT JOIN tblImpVal iv ON iv.impvValor = i.usrIdImp
            LEFT JOIN tblSubArea sa ON sa.sarIdSArea = iv.impIdSArea
            LEFT JOIN tblArea a ON a.arIdArea = sa.sarIdArea
            WHERE i.usrIdClave = @clave and i.usrIdRol = @rol", conn);
            cmdImp.Parameters.AddWithValue("@clave", usrIdClave);
            cmdImp.Parameters.AddWithValue("@rol", rol.UsrIdRol);
            rol.Imputaciones = new List<UsuarioImputacionDto>();
            using var readerImp = await cmdImp.ExecuteReaderAsync();
            while (await readerImp.ReadAsync())
        {
            var imputacion = new UsuarioImputacionDto
            {
                UsrIdImp = readerImp.IsDBNull(0) ? null : readerImp.GetString(0),
                UsrIdRol = readerImp.GetString(1),
                ImpIdSArea = readerImp.IsDBNull(2) ? 0 : readerImp.GetInt32(2),
                SubAreaNombre = readerImp.IsDBNull(3) ? null : readerImp.GetString(3),
                SarIdArea = readerImp.IsDBNull(4) ? 0 : readerImp.GetInt32(4),
                AreaNombre = readerImp.IsDBNull(5) ? null : readerImp.GetString(5),
                impNivel = readerImp.IsDBNull(6) ? null : readerImp.GetString(6),
            };
                rol.Imputaciones.Add(imputacion);
            }
        readerImp.Close();
        }
        return result;
    }



        /// <summary>
        /// Genera el siguiente usrIdClave para la sociedad indicada:
        ///   <socNombre> + número (máximo existente + 1) con ceros a la izquierda.
        /// Usa transacción Serializable y locks para evitar duplicados concurrentes.
        /// </summary>
        /// <param name="socIdSoc">ID de sociedad (tblSociedad.socIdSoc)</param>
        /// <param name="connectionString">Cadena de conexión (DB por defecto debe contener las tablas)</param>
        /// <param name="numDigitos">Cantidad de dígitos del consecutivo (por defecto, 4)</param>
        /// <param name="ct">CancellationToken opcional</param>
        public   async Task<string> ObtenerSiguienteUsrIdClaveAsync(
            int socIdSoc,
//            string connectionString,
            int numDigitos = 4,
            CancellationToken ct = default)
        {
           // await using var conn = new SqlConnection(connectionString);
           // await conn.OpenAsync(ct);


            using var conn = CreateConnection();
            await conn.OpenAsync();



            // Transacción serializable para “serializar” el cálculo del máximo.
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            try
            {
                // 1) Obtener prefijo desde la sociedad (socNombre)
                string prefix;
                await using (var cmd = new SqlCommand(
                    @"SELECT LTRIM(RTRIM([socNombre])) 
                  FROM [dbo].[tblSociedad] 
                  WHERE [socIdSoc] = @socIdSoc;", conn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@socIdSoc", SqlDbType.Int) { Value = socIdSoc });

                    var result = await cmd.ExecuteScalarAsync(ct);
                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException($"Sociedad {socIdSoc} no encontrada.");

                    prefix = Convert.ToString(result)!;
                    if (string.IsNullOrWhiteSpace(prefix))
                        throw new InvalidOperationException($"Sociedad {socIdSoc} sin nombre (socNombre vacío).");
                }

                // 2) Calcular el máximo sufijo numérico existente para ese prefijo y sociedad
                var start = prefix.Length + 1; // SUBSTRING inicia en 1
                int maxN = 0;

                await using (var cmd = new SqlCommand(@"
SELECT MAX(TRY_CONVERT(int, SUBSTRING(u.[usrIdClave], @start, 8000)))
FROM [dbo].[tblUsuario] AS u WITH (UPDLOCK, HOLDLOCK)
WHERE u.[usrIdSoc] = @socIdSoc
  AND u.[usrIdClave] LIKE @likePrefix;", conn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.Int) { Value = start });
                    cmd.Parameters.Add(new SqlParameter("@socIdSoc", SqlDbType.Int) { Value = socIdSoc });
                    cmd.Parameters.Add(new SqlParameter("@likePrefix", SqlDbType.NVarChar, 400) { Value = prefix + "%" });

                    var result = await cmd.ExecuteScalarAsync(ct);
                    if (result != null && result != DBNull.Value)
                        maxN = Convert.ToInt32(result);
                }

                // 3) Siguiente número y formateo con ceros a la izquierda
                var next = maxN + 1;
                var nextId = prefix + next.ToString("D" + numDigitos);

                await tx.CommitAsync(ct);
                return nextId;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
 

    public async Task<bool> CreateAsync(UsuarioEdicionDto dto)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();
        int resultado;

        try
        {
            var u = dto.Usuario;

            // Insertar en tblUsuario
//            using var cmdUsr = new SqlCommand(@"INSERT INTO tblUsuario VALUES (@c,@s,@ap,@am,@cc,@pu,@co,@no,@pw)", conn, tran);
            using var cmdUsr = new SqlCommand(@"INSERT INTO tblUsuario VALUES (@c,@s,@ap,@am,@cc ,@pu,@co,@no,@pw)", conn, tran);

            string nuevoId = await ObtenerSiguienteUsrIdClaveAsync(socIdSoc: Convert.ToInt16(u.UsrIdSoc) );


            cmdUsr.Parameters.AddWithValue("@c", nuevoId);
            cmdUsr.Parameters.AddWithValue("@s", u.UsrIdSoc.ToString());
            cmdUsr.Parameters.AddWithValue("@ap", u.UsrApellidoP);
            cmdUsr.Parameters.AddWithValue("@am", (object)u.UsrApellidoM ?? DBNull.Value);
            cmdUsr.Parameters.AddWithValue("@cc", "*");
            cmdUsr.Parameters.AddWithValue("@pu", u.UsrPuesto);
            cmdUsr.Parameters.AddWithValue("@co", u.UsrCorreo);
            cmdUsr.Parameters.AddWithValue("@no", u.UsrNombre);


            string passwordHashed = PasswordHelper.HashPassword(u.PasswordHash);


            cmdUsr.Parameters.AddWithValue("@pw", (object)passwordHashed ?? DBNull.Value);
            await cmdUsr.ExecuteNonQueryAsync();








            // Insertar roles/permisos
            foreach (var r in dto.Usuario.RolesPermisos)
            {
                using var cmdRol = new SqlCommand(@"INSERT INTO tblUsrRolPer VALUES (@c,@r,@a,@x,@d,@u,@v,@l,@t,@q,@m,@li)", conn, tran);
                cmdRol.Parameters.AddWithValue("@c", nuevoId);
                cmdRol.Parameters.AddWithValue("@r", r.UsrIdRol);
                cmdRol.Parameters.AddWithValue("@a", r.UsrPerAlta);
                cmdRol.Parameters.AddWithValue("@x", r.Configuracion);
                cmdRol.Parameters.AddWithValue("@d", r.UsrPerDel);
                cmdRol.Parameters.AddWithValue("@u", r.UsrPerUpdate);
                cmdRol.Parameters.AddWithValue("@v", r.UsrPerVoBo);
                cmdRol.Parameters.AddWithValue("@l", r.UsrPerLic);
                cmdRol.Parameters.AddWithValue("@t", r.UsrPerAut);
                cmdRol.Parameters.AddWithValue("@q", r.UsrPerReq);
                cmdRol.Parameters.AddWithValue("@m", r.UsrPerCompara);
                cmdRol.Parameters.AddWithValue("@li", r.UsrPerLibera);
                await cmdRol.ExecuteNonQueryAsync();


                // Insertar imputaciones
                foreach (var i in r.Imputaciones)
                {
                    using var cmdImp = new SqlCommand(@"INSERT INTO tblUsrImputacion VALUES (@c,@r,@i,@ni)", conn, tran);
                    cmdImp.Parameters.AddWithValue("@c", nuevoId);
                    cmdImp.Parameters.AddWithValue("@r", i.UsrIdRol);
                    cmdImp.Parameters.AddWithValue("@i", i.UsrIdImp ?? (object)DBNull.Value);
                    cmdImp.Parameters.AddWithValue("@ni", i.impNivel ?? (object)DBNull.Value);
                    await cmdImp.ExecuteNonQueryAsync();
                }
            }



            tran.Commit();
            return true;
        }
        catch
        {
            tran.Rollback();
            return false;
        }
    }
    public async Task<bool> UpdateAsync(UsuarioEdicionDto dto)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            var u = dto.Usuario;
        
            // Actualizar tblUsuario
            using var cmdUsr1 = new SqlCommand(@"UPDATE tblUsuario SET
                usrIdSoc=@s, usrApellidoP=@ap, usrApellidoM=@am, usrCeCo=@cc,
                usrPuesto=@pu, usrCorreo=@co, usrNombre=@no, PasswordHash=@pw
                WHERE usrIdClave=@c", conn, tran);

            using var cmdUsr = new SqlCommand(@"UPDATE tblUsuario SET
                usrIdSoc=@s, usrApellidoP=@ap, usrApellidoM=@am, 
                usrPuesto=@pu, usrCorreo=@co, usrNombre=@no, PasswordHash=@pw
                WHERE usrIdClave=@c", conn, tran);

            cmdUsr.Parameters.AddWithValue("@c", u.UsrIdClave);
            cmdUsr.Parameters.AddWithValue("@s", u.UsrIdSoc);
            cmdUsr.Parameters.AddWithValue("@ap", u.UsrApellidoP);
            cmdUsr.Parameters.AddWithValue("@am", (object)u.UsrApellidoM ?? DBNull.Value);
            cmdUsr.Parameters.AddWithValue("@pu", u.UsrPuesto);
            cmdUsr.Parameters.AddWithValue("@co", u.UsrCorreo);
            cmdUsr.Parameters.AddWithValue("@no", u.UsrNombre);
            cmdUsr.Parameters.AddWithValue("@pw", (object)u.PasswordHash ?? DBNull.Value);
            await cmdUsr.ExecuteNonQueryAsync();

            // Eliminar registros previos en tblUsrRolPer y tblUsrImputacion
            using var cmdDelRol = new SqlCommand("DELETE FROM tblUsrRolPer WHERE usrIdClave = @c", conn, tran);
            cmdDelRol.Parameters.AddWithValue("@c", u.UsrIdClave);
            await cmdDelRol.ExecuteNonQueryAsync();

            using var cmdDelImp = new SqlCommand("DELETE FROM tblUsrImputacion WHERE usrIdClave = @c", conn, tran);
            cmdDelImp.Parameters.AddWithValue("@c", u.UsrIdClave);
            await cmdDelImp.ExecuteNonQueryAsync();

            // Insertar nuevos roles
            foreach (var r in dto.Usuario.RolesPermisos)
            {
                using var cmdRol = new SqlCommand(@"INSERT INTO tblUsrRolPer VALUES (@c,@r,@a,@x,@d,@u,@v,@l,@t,@q,@m,@li)", conn, tran);
                cmdRol.Parameters.AddWithValue("@c", u.UsrIdClave);
                cmdRol.Parameters.AddWithValue("@r", r.UsrIdRol);
                cmdRol.Parameters.AddWithValue("@a", r.UsrPerAlta);
                cmdRol.Parameters.AddWithValue("@x", r.Configuracion);
                cmdRol.Parameters.AddWithValue("@d", r.UsrPerDel);
                cmdRol.Parameters.AddWithValue("@u", r.UsrPerUpdate);
                cmdRol.Parameters.AddWithValue("@v", r.UsrPerVoBo);
                cmdRol.Parameters.AddWithValue("@l", r.UsrPerLic);
                cmdRol.Parameters.AddWithValue("@t", r.UsrPerAut);
                cmdRol.Parameters.AddWithValue("@q", r.UsrPerReq);
                cmdRol.Parameters.AddWithValue("@m", r.UsrPerCompara);
                cmdRol.Parameters.AddWithValue("@li", r.UsrPerLibera);
                await cmdRol.ExecuteNonQueryAsync();

            // Insertar nuevas imputaciones
            foreach (var i in r.Imputaciones)
            {
                using var cmdImp = new SqlCommand(@"INSERT INTO tblUsrImputacion VALUES (@c,@r,@i,@ni)", conn, tran);
                cmdImp.Parameters.AddWithValue("@c", u.UsrIdClave);
                cmdImp.Parameters.AddWithValue("@r", i.UsrIdRol);
              cmdImp.Parameters.AddWithValue("@i", i.UsrIdImp ?? (object)DBNull.Value);
                    cmdImp.Parameters.AddWithValue("@ni", i.impNivel ?? (object)DBNull.Value);
                    await cmdImp.ExecuteNonQueryAsync();
            }

            }



            tran.Commit();
            return true;
        }
        catch
        {
            tran.Rollback();
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string usrIdClave)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            using var cmdDelImp = new SqlCommand("DELETE FROM tblUsrImputacion WHERE usrIdClave = @c", conn, tran);
            cmdDelImp.Parameters.AddWithValue("@c", usrIdClave);
            await cmdDelImp.ExecuteNonQueryAsync();

            using var cmdDelRol = new SqlCommand("DELETE FROM tblUsrRolPer WHERE usrIdClave = @c", conn, tran);
            cmdDelRol.Parameters.AddWithValue("@c", usrIdClave);
            await cmdDelRol.ExecuteNonQueryAsync();

            using var cmdDelUsr = new SqlCommand("DELETE FROM tblUsuario WHERE usrIdClave = @c", conn, tran);
            cmdDelUsr.Parameters.AddWithValue("@c", usrIdClave);
            await cmdDelUsr.ExecuteNonQueryAsync();

            tran.Commit();
            return true;
        }
        catch
        {
            tran.Rollback();
            return false;
        }
    }
}
