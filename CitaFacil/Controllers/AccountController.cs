// En: Controllers/AccountController.cs
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CitaFacil.Data;
using CitaFacil.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using CitaFacil.ViewModels.Account;
using CitaFacil.Constants;
using CitaFacil.Services.Security;
using Microsoft.Extensions.Logging;

namespace CitaFacil.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<AccountController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // --- REGISTRO ---
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel
            {
                fecha_nacimiento = DateTime.Today.AddYears(-18)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.fecha_nacimiento > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.fecha_nacimiento), "La fecha de nacimiento no puede ser futura.");
                return View(model);
            }

            var normalizedUser = model.usuario.Trim().ToUpperInvariant();
            var normalizedEmail = model.correo.Trim().ToUpperInvariant();

            var existingUser = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.usuario.ToUpper() == normalizedUser || u.correo.ToUpper() == normalizedEmail);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "El nombre de usuario o correo ya está en uso.");
                return View(model);
            }

            var segundoApellido = string.IsNullOrWhiteSpace(model.segundo_apellido) ? null : model.segundo_apellido.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var nuevoUsuario = new Usuario
                {
                    nombre = model.nombre.Trim(),
                    apellido = model.apellido.Trim(),
                    segundo_apellido = segundoApellido,
                    usuario = model.usuario.Trim(),
                    correo = model.correo.Trim(),
                    rol = Roles.Paciente,
                    contraseña = _passwordHasher.Hash(model.contraseña),
                    celular = string.IsNullOrWhiteSpace(model.celular) ? null : model.celular.Trim(),
                    foto_url = null
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                var nuevoPaciente = new Paciente
                {
                    usuario_id = nuevoUsuario.id,
                    nombre = nuevoUsuario.nombre,
                    apellido = nuevoUsuario.apellido,
                    segundo_apellido = segundoApellido,
                    fecha_nacimiento = model.fecha_nacimiento,
                    genero = string.IsNullOrWhiteSpace(model.genero) ? null : model.genero.Trim(),
                    direccion = string.IsNullOrWhiteSpace(model.direccion) ? null : model.direccion.Trim()
                };

                _context.Pacientes.Add(nuevoPaciente);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Tu cuenta ha sido creada correctamente. Ahora puedes iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar una nueva cuenta para {Usuario}", model.usuario);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar la cuenta. Inténtalo de nuevo.");
                return View(model);
            }
        }


        // --- INICIO DE SESIÓN (LOGIN) ---
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedIdentificador = model.identificador.Trim().ToUpperInvariant();

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.usuario.ToUpper() == normalizedIdentificador || u.correo.ToUpper() == normalizedIdentificador);

            if (user == null || !_passwordHasher.Verify(model.contraseña, user.contraseña))
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos.");
                return View(model);
            }

            if (!user.activo)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Contacta a un administrador.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(ClaimTypes.Name, user.usuario),
                new Claim(ClaimTypes.GivenName, user.nombre),
                new Claim(ClaimTypes.Surname, user.apellido ?? string.Empty),
                new Claim(ClaimTypes.Email, user.correo),
                new Claim(ClaimTypes.Role, user.rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.recordar
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return user.rol switch
            {
                Roles.Administrador => RedirectToAction("Index", "Admin"),
                Roles.Doctor => RedirectToAction("Index", "Doctor"),
                Roles.Paciente => RedirectToAction("Dashboard", "Pacientes"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // --- CERRAR SESIÓN (LOGOUT) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // --- PROTOTIPO DE LOGIN (Solo vista) ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ProtoLogin()
        {
            return View();
        }
    }
}
