using CitaFacil.Data;
using CitaFacil.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitaFacil.Controllers
{
    [Authorize]
    public class EspecialidadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EspecialidadesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Especialidads
        public async Task<IActionResult> Index()
        {
            return View(await _context.Especialidades.ToListAsync());
        }

        // GET: Especialidads/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(m => m.id == id);
            if (especialidad == null)
            {
                return NotFound();
            }

            return View(especialidad);
        }

        // GET: Especialidads/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Especialidads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,nombre,descripcion,icono,activa")] Especialidad especialidad)
        {
            if (ModelState.IsValid)
            {
                _context.Add(especialidad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(especialidad);
        }

        // POST: Especialidads/CreateAjax
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Especialidad especialidad)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                _context.Add(especialidad);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Especialidad creada exitosamente", id = especialidad.id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear especialidad: " + ex.Message });
            }
        }

        // GET: Especialidads/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especialidad = await _context.Especialidades.FindAsync(id);
            if (especialidad == null)
            {
                return NotFound();
            }
            return View(especialidad);
        }

        // POST: Especialidads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("id,nombre,descripcion,icono,activa")] Especialidad especialidad)
        {
            if (id != especialidad.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(especialidad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EspecialidadExists(especialidad.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(especialidad);
        }

        // POST: Especialidads/EditAjax
        [HttpPost]
        public async Task<IActionResult> EditAjax(long id, [FromBody] Especialidad especialidad)
        {
            try
            {
                if (id != especialidad.id)
                {
                    return Json(new { success = false, message = "ID no coincide" });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                _context.Update(especialidad);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Especialidad actualizada exitosamente" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EspecialidadExists(id))
                {
                    return Json(new { success = false, message = "Especialidad no encontrada" });
                }
                else
                {
                    return Json(new { success = false, message = "Error de concurrencia al actualizar" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar especialidad: " + ex.Message });
            }
        }

        // GET: Especialidads/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(m => m.id == id);
            if (especialidad == null)
            {
                return NotFound();
            }

            return View(especialidad);
        }

        // POST: Especialidads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var especialidad = await _context.Especialidades.FindAsync(id);
            if (especialidad != null)
            {
                _context.Especialidades.Remove(especialidad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE: Especialidads/DeleteAjax/5
        [HttpDelete]
        public async Task<IActionResult> DeleteAjax(long id)
        {
            try
            {
                var especialidad = await _context.Especialidades
                    .Include(e => e.Doctores)
                    .FirstOrDefaultAsync(e => e.id == id);

                if (especialidad == null)
                {
                    return Json(new { success = false, message = "Especialidad no encontrada" });
                }

                // Verificar si tiene doctores asociados
                if (especialidad.Doctores != null && especialidad.Doctores.Any())
                {
                    return Json(new { success = false, message = "No se puede eliminar la especialidad porque tiene doctores asociados" });
                }

                _context.Especialidades.Remove(especialidad);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Especialidad eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar especialidad: " + ex.Message });
            }
        }

        // GET: Especialidads/GetDetailsAjax/5
        [HttpGet]
        public async Task<IActionResult> GetDetailsAjax(long id)
        {
            try
            {
                var especialidad = await _context.Especialidades
                    .Include(e => e.Doctores)
                    .FirstOrDefaultAsync(e => e.id == id);

                if (especialidad == null)
                {
                    return Json(new { success = false, message = "Especialidad no encontrada" });
                }

                return Json(new
                {
                    success = true,
                    especialidad = new
                    {
                        id = especialidad.id,
                        nombre = especialidad.nombre,
                        descripcion = especialidad.descripcion,
                        icono = especialidad.icono,
                        activa = especialidad.activa,
                        totalDoctores = especialidad.Doctores?.Count ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener detalles: " + ex.Message });
            }
        }

        private bool EspecialidadExists(long id)
        {
            return _context.Especialidades.Any(e => e.id == id);
        }
    }
}
