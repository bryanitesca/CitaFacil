using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitaFacil.Constants;
using CitaFacil.Data;
using CitaFacil.Models;
using CitaFacil.Services.Security;
using CitaFacil.Services.Storage;
using CitaFacil.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CitaFacil.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UsuariosController> _logger;

        private readonly IImageStorageService _imageStorageService;

        public UsuariosController(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<UsuariosController> logger, IImageStorageService imageStorageService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _imageStorageService = imageStorageService;
        }

        // GET: Usuarios (LISTAR)
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Usuarios.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.nombre, pattern) ||
                    (u.apellido != null && EF.Functions.Like(u.apellido, pattern)));
            }

            var usuarios = await query
                .OrderByDescending(u => u.creado_el)
                .ToListAsync();

            ViewBag.SearchTerm = search?.Trim();

            return View(usuarios);
        }

        // GET: Usuarios/Details/5 (DETALLES)
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            // Cargar el usuario CON sus datos relacionados
            var usuario = await _context.Usuarios
                .Include(u => u.Doctor)
                    .ThenInclude(d => d!.Especialidad) // Cargar la especialidad del doctor
                .Include(u => u.Paciente)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create (VISTA DE CREAR)
        public async Task<IActionResult> Create()
        {
            ViewBag.RolesList = new SelectList(Roles.Todos);
            var especialidades = await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync();
            ViewBag.EspecialidadesList = new SelectList(especialidades, "id", "nombre");

            return View(new CreateUserViewModel());
        }

        // POST: Usuarios/Create (GUARDAR CREACIÓN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            var usuarioInput = model.usuario.Trim();
            var correoInput = model.correo.Trim();
            var normalizedUser = usuarioInput.ToUpperInvariant();
            var normalizedEmail = correoInput.ToUpperInvariant();
            var segundoApellidoUsuario = string.IsNullOrWhiteSpace(model.segundo_apellido) ? null : model.segundo_apellido.Trim();

            var existingUser = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.usuario.ToUpper() == normalizedUser || u.correo.ToUpper() == normalizedEmail);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "El nombre de usuario o el correo electrónico ya están en uso.");
            }

            if (!Roles.Todos.Contains(model.rol))
            {
                ModelState.AddModelError(nameof(model.rol), "Selecciona un rol válido.");
            }

            if (model.rol == Roles.Doctor)
            {
                if (string.IsNullOrWhiteSpace(model.licencia))
                {
                    ModelState.AddModelError(nameof(model.licencia), "La licencia profesional es obligatoria.");
                }
                else
                {
                    var licenciaSanitizada = model.licencia.Trim();
                    var licenciaDuplicada = await _context.Doctores
                        .AsNoTracking()
                        .AnyAsync(d => d.licencia == licenciaSanitizada);

                    if (licenciaDuplicada)
                    {
                        ModelState.AddModelError(nameof(model.licencia), "La licencia profesional ya está registrada.");
                    }
                }

                if (model.especialidad_id is null)
                {
                    ModelState.AddModelError(nameof(model.especialidad_id), "Selecciona una especialidad.");
                }
                else
                {
                    var especialidadValida = await _context.Especialidades
                        .AsNoTracking()
                        .AnyAsync(e => e.id == model.especialidad_id && e.activa);

                    if (!especialidadValida)
                    {
                        ModelState.AddModelError(nameof(model.especialidad_id), "La especialidad seleccionada ya no está disponible.");
                    }
                }
            }

            if (model.rol == Roles.Paciente)
            {
                if (model.fecha_nacimiento is null)
                {
                    ModelState.AddModelError(nameof(model.fecha_nacimiento), "La fecha de nacimiento es obligatoria.");
                }
                else if (model.fecha_nacimiento > DateTime.Today)
                {
                    ModelState.AddModelError(nameof(model.fecha_nacimiento), "La fecha de nacimiento no puede ser futura.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = new SelectList(Roles.Todos);
                var especialidadesInvalida = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.EspecialidadesList = new SelectList(especialidadesInvalida, "id", "nombre");
                return View(model);
            }

            string? fotoPath = null;

            if (model.rol == Roles.Doctor)
            {
                if (model.foto is null || model.foto.Length == 0)
                {
                    ModelState.AddModelError(nameof(model.foto), "La fotografía es obligatoria para los doctores.");
                }
                else
                {
                    var fotoResultado = await _imageStorageService.SaveUserAvatarAsync(model.foto, null);
                    if (!fotoResultado.Success || string.IsNullOrWhiteSpace(fotoResultado.FilePath))
                    {
                        ModelState.AddModelError(nameof(model.foto), fotoResultado.ErrorMessage ?? "No se pudo guardar la fotografía.");
                    }
                    else
                    {
                        fotoPath = fotoResultado.FilePath;
                    }
                }
            }
            else if (model.foto != null && model.foto.Length > 0)
            {
                ModelState.AddModelError(nameof(model.foto), "Solo los doctores pueden tener fotografía.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = new SelectList(Roles.Todos);
                var especialidadesError = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.EspecialidadesList = new SelectList(especialidadesError, "id", "nombre");
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var nuevoUsuario = new Usuario
                {
                    nombre = model.nombre.Trim(),
                    apellido = model.apellido.Trim(),
                    segundo_apellido = segundoApellidoUsuario,
                    usuario = usuarioInput,
                    correo = correoInput,
                    rol = model.rol,
                    contraseña = _passwordHasher.Hash(model.contraseña),
                    celular = string.IsNullOrWhiteSpace(model.celular) ? null : model.celular.Trim(),
                    foto_url = fotoPath
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                if (model.rol == Roles.Doctor)
                {
                    var nuevoDoctor = new Doctor
                    {
                        usuario_id = nuevoUsuario.id,
                        nombre = nuevoUsuario.nombre,
                        apellido = nuevoUsuario.apellido ?? string.Empty,
                        segundo_apellido = nuevoUsuario.segundo_apellido,
                        licencia = model.licencia!.Trim(),
                        especialidad_id = model.especialidad_id!.Value,
                        consultorio = string.IsNullOrWhiteSpace(model.consultorio) ? null : model.consultorio.Trim(),
                        biografia = string.IsNullOrWhiteSpace(model.biografia) ? null : model.biografia.Trim()
                    };

                    _context.Doctores.Add(nuevoDoctor);
                }
                else if (model.rol == Roles.Paciente)
                {
                    var nuevoPaciente = new Paciente
                    {
                        usuario_id = nuevoUsuario.id,
                        nombre = nuevoUsuario.nombre,
                        apellido = nuevoUsuario.apellido ?? string.Empty,
                        segundo_apellido = nuevoUsuario.segundo_apellido,
                        fecha_nacimiento = model.fecha_nacimiento!.Value,
                        genero = string.IsNullOrWhiteSpace(model.genero) ? null : model.genero.Trim(),
                        direccion = string.IsNullOrWhiteSpace(model.direccion) ? null : model.direccion.Trim(),
                    };

                    _context.Pacientes.Add(nuevoPaciente);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Usuario '{nuevoUsuario.usuario}' ({model.rol}) creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                var friendlyMessage = BuildCreateUserErrorMessage(dbEx, model);
                _logger.LogError(dbEx, "Error de base de datos al crear el usuario {Usuario}: {Mensaje}", model.usuario, friendlyMessage);
                ModelState.AddModelError(string.Empty, friendlyMessage);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al crear el usuario {Usuario}", model.usuario);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al crear el usuario. Intenta nuevamente y, si el problema persiste, contacta al administrador.");
            }

            ViewBag.RolesList = new SelectList(Roles.Todos);
            var especialidades = await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync();
            ViewBag.EspecialidadesList = new SelectList(especialidades, "id", "nombre");

            return View(model);
        }

        // GET: Usuarios/Edit/5 (VISTA DE EDITAR)
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Doctor)
                .Include(u => u.Paciente)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.id == id);

            if (usuario == null) return NotFound();

            // Mapear de Modelo a ViewModel
            var viewModel = new EditUserViewModel
            {
                id = usuario.id,
                nombre = usuario.nombre,
                apellido = usuario.apellido ?? string.Empty,
                segundo_apellido = usuario.segundo_apellido,
                usuario = usuario.usuario,
                correo = usuario.correo,
                rol = usuario.rol,
                celular = usuario.celular,
                foto_actual = usuario.foto_url
            };

            if (usuario.Doctor != null)
            {
                viewModel.licencia = usuario.Doctor.licencia;
                viewModel.especialidad_id = usuario.Doctor.especialidad_id;
                viewModel.consultorio = usuario.Doctor.consultorio;
                viewModel.biografia = usuario.Doctor.biografia;
            }

            if (usuario.Paciente != null)
            {
                viewModel.fecha_nacimiento = usuario.Paciente.fecha_nacimiento;
                viewModel.genero = usuario.Paciente.genero;
                viewModel.direccion = usuario.Paciente.direccion;
            }

            // Cargar dropdowns
            ViewBag.RolesList = new SelectList(Roles.Todos);
            ViewBag.EspecialidadesList = new SelectList(await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync(), "id", "nombre");

            return View(viewModel); // Enviar el ViewModel a la vista
        }

        // POST: Usuarios/Edit/5 (GUARDAR EDICIÓN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditUserViewModel model)
        {
            if (id != model.id) return NotFound();

            var usuarioInput = model.usuario.Trim();
            var correoInput = model.correo.Trim();
            var normalizedUser = usuarioInput.ToUpperInvariant();
            var normalizedEmail = correoInput.ToUpperInvariant();

            var existingUser = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => (u.usuario.ToUpper() == normalizedUser || u.correo.ToUpper() == normalizedEmail) && u.id != model.id);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "El nombre de usuario o el correo electrónico ya están en uso por otra cuenta.");
            }

            if (!Roles.Todos.Contains(model.rol))
            {
                ModelState.AddModelError(nameof(model.rol), "Selecciona un rol válido.");
            }

            if (model.rol == Roles.Doctor)
            {
                if (string.IsNullOrWhiteSpace(model.licencia))
                {
                    ModelState.AddModelError(nameof(model.licencia), "La licencia profesional es obligatoria.");
                }

                if (model.especialidad_id is null)
                {
                    ModelState.AddModelError(nameof(model.especialidad_id), "Selecciona una especialidad.");
                }
            }

            if (model.rol == Roles.Paciente)
            {
                if (model.fecha_nacimiento is null)
                {
                    ModelState.AddModelError(nameof(model.fecha_nacimiento), "La fecha de nacimiento es obligatoria.");
                }
                else if (model.fecha_nacimiento > DateTime.Today)
                {
                    ModelState.AddModelError(nameof(model.fecha_nacimiento), "La fecha de nacimiento no puede ser futura.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.RolesList = new SelectList(Roles.Todos);
                var especialidadesInvalidas = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.EspecialidadesList = new SelectList(especialidadesInvalidas, "id", "nombre");
                model.foto_actual = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.id == model.id)
                    .Select(u => u.foto_url)
                    .FirstOrDefaultAsync();
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.Doctor)
                    .Include(u => u.Paciente)
                    .FirstOrDefaultAsync(u => u.id == model.id);

                if (usuario == null) return NotFound();
                string? updatedPhotoPath = usuario.foto_url;

                if (model.rol == Roles.Doctor)
                {
                    if (model.foto != null && model.foto.Length > 0)
                    {
                        var fotoResultado = await _imageStorageService.SaveUserAvatarAsync(model.foto, usuario.foto_url);
                        if (!fotoResultado.Success || string.IsNullOrWhiteSpace(fotoResultado.FilePath))
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError(nameof(model.foto), fotoResultado.ErrorMessage ?? "No se pudo actualizar la fotografía.");
                            return await ReturnEditViewAsync(model, usuario.foto_url);
                        }

                        updatedPhotoPath = fotoResultado.FilePath;
                    }
                }
                else
                {
                    if (model.foto != null && model.foto.Length > 0)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(model.foto), "Solo los doctores pueden tener fotografía.");
                        return await ReturnEditViewAsync(model, usuario.foto_url);
                    }

                    if (!string.IsNullOrWhiteSpace(usuario.foto_url))
                    {
                        await _imageStorageService.DeleteAsync(usuario.foto_url);
                        updatedPhotoPath = null;
                    }
                }

                var sanitizedNombre = model.nombre.Trim();
                var sanitizedApellido = model.apellido.Trim();
                var sanitizedSegundo = string.IsNullOrWhiteSpace(model.segundo_apellido) ? null : model.segundo_apellido.Trim();

                usuario.nombre = sanitizedNombre;
                usuario.apellido = sanitizedApellido;
                usuario.usuario = usuarioInput;
                usuario.correo = correoInput;
                usuario.rol = model.rol;
                usuario.celular = string.IsNullOrWhiteSpace(model.celular) ? null : model.celular.Trim();
                usuario.foto_url = updatedPhotoPath;
                usuario.segundo_apellido = sanitizedSegundo;

                _context.Usuarios.Update(usuario);

                if (model.rol == Roles.Doctor)
                {
                    var doctor = usuario.Doctor ?? new Doctor { usuario_id = usuario.id };
                    doctor.nombre = sanitizedNombre;
                    doctor.apellido = sanitizedApellido;
                    doctor.segundo_apellido = sanitizedSegundo;
                    doctor.licencia = model.licencia!.Trim();
                    doctor.especialidad_id = model.especialidad_id!.Value;
                    doctor.consultorio = string.IsNullOrWhiteSpace(model.consultorio) ? null : model.consultorio.Trim();
                    doctor.biografia = string.IsNullOrWhiteSpace(model.biografia) ? null : model.biografia.Trim();

                    if (usuario.Doctor == null)
                    {
                        _context.Doctores.Add(doctor);
                    }
                    else
                    {
                        _context.Doctores.Update(doctor);
                    }

                    if (usuario.Paciente != null)
                    {
                        _context.Pacientes.Remove(usuario.Paciente);
                        usuario.Paciente = null;
                    }
                }
                else if (model.rol == Roles.Paciente)
                {
                    var paciente = usuario.Paciente ?? new Paciente { usuario_id = usuario.id };
                    paciente.nombre = sanitizedNombre;
                    paciente.apellido = sanitizedApellido;
                    paciente.segundo_apellido = sanitizedSegundo;
                    paciente.fecha_nacimiento = model.fecha_nacimiento!.Value;
                    paciente.genero = string.IsNullOrWhiteSpace(model.genero) ? null : model.genero.Trim();
                    paciente.direccion = string.IsNullOrWhiteSpace(model.direccion) ? null : model.direccion.Trim();

                    if (usuario.Paciente == null)
                    {
                        _context.Pacientes.Add(paciente);
                    }
                    else
                    {
                        _context.Pacientes.Update(paciente);
                    }

                    if (usuario.Doctor != null)
                    {
                        _context.Doctores.Remove(usuario.Doctor);
                        usuario.Doctor = null;
                    }
                }
                else
                {
                    if (usuario.Doctor != null)
                    {
                        _context.Doctores.Remove(usuario.Doctor);
                        usuario.Doctor = null;
                    }

                    if (usuario.Paciente != null)
                    {
                        _context.Pacientes.Remove(usuario.Paciente);
                        usuario.Paciente = null;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Usuario '{usuario.usuario}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Conflicto de concurrencia al actualizar el usuario {UsuarioId}", model.id);
                if (!UsuarioExists(model.id))
                {
                    return NotFound();
                }

                ModelState.AddModelError(string.Empty, "Otro usuario modificó este registro. Vuelve a cargar la página e inténtalo de nuevo.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al actualizar el usuario {UsuarioId}", model.id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el usuario. Intenta nuevamente.");
            }

            ViewBag.RolesList = new SelectList(Roles.Todos);
            var especialidades = await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync();
            ViewBag.EspecialidadesList = new SelectList(especialidades, "id", "nombre");

            if (string.IsNullOrWhiteSpace(model.foto_actual))
            {
                model.foto_actual = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.id == model.id)
                    .Select(u => u.foto_url)
                    .FirstOrDefaultAsync();
            }

            return View(model);

            async Task<IActionResult> ReturnEditViewAsync(EditUserViewModel viewModel, string? fotoActual)
            {
                ViewBag.RolesList = new SelectList(Roles.Todos);
                var especialidadesLocales = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.EspecialidadesList = new SelectList(especialidadesLocales, "id", "nombre");
                viewModel.foto_actual = fotoActual;
                return View(viewModel);
            }
        }

        // GET: Usuarios/Delete/5 (VISTA DE BORRAR)
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5 (CONFIRMAR BORRADO)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.activo = !usuario.activo;
            await _context.SaveChangesAsync();

            var accion = usuario.activo ? "reactivado" : "desactivado";
            TempData["SuccessMessage"] = $"Usuario '{usuario.usuario}' {accion} correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(long id)
        {
            return _context.Usuarios.Any(e => e.id == id);
        }

        private string BuildCreateUserErrorMessage(DbUpdateException exception, CreateUserViewModel input)
        {
            if (exception.GetBaseException() is SqlException sqlException)
            {
                foreach (SqlError error in sqlException.Errors)
                {
                    var friendlyFromSql = ResolveSqlError(error, input);
                    if (!string.IsNullOrEmpty(friendlyFromSql))
                    {
                        return friendlyFromSql;
                    }
                }
            }

            var baseMessage = exception.GetBaseException().Message;

            if (baseMessage.Contains("IX_Doctores_licencia", StringComparison.OrdinalIgnoreCase))
            {
                return "La licencia profesional ingresada ya se encuentra registrada.";
            }

            if (baseMessage.Contains("FK_Doctores_Especialidades_especialidad_id", StringComparison.OrdinalIgnoreCase))
            {
                return "La especialidad seleccionada no existe o fue desactivada.";
            }

            return "No se pudo crear el usuario porque los datos enviados no cumplen con las reglas del sistema. Revisa el formulario e inténtalo nuevamente.";
        }

        private static string? ResolveSqlError(SqlError error, CreateUserViewModel input)
        {
            if (error.Number == 2601 || error.Number == 2627)
            {
                if (error.Message.Contains("IX_Usuarios_usuario", StringComparison.OrdinalIgnoreCase))
                {
                    return $"El nombre de usuario '{input.usuario}' ya está registrado. Elige uno diferente.";
                }

                if (error.Message.Contains("IX_Usuarios_correo", StringComparison.OrdinalIgnoreCase))
                {
                    return $"El correo '{input.correo}' ya está registrado.";
                }

                if (error.Message.Contains("IX_Doctores_licencia", StringComparison.OrdinalIgnoreCase))
                {
                    return "La licencia profesional ingresada ya se encuentra asociada a otro doctor.";
                }
            }

            if (error.Number == 547 && error.Message.Contains("FK_Doctores_Especialidades_especialidad_id", StringComparison.OrdinalIgnoreCase))
            {
                return "La especialidad seleccionada ya no está disponible. Actualiza la página e inténtalo nuevamente.";
            }

            return null;
        }
    }
}

