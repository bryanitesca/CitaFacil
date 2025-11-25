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
    public class DoctoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Doctores
                .Include(d => d.Especialidad)
                .Include(d => d.Usuario)
                .Where(d => d.Usuario != null && d.Usuario.activo);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Especialidad)
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(m => m.id == id && m.Usuario != null && m.Usuario.activo);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            ViewData["especialidad_id"] = new SelectList(_context.Especialidades, "id", "nombre");
            ViewData["usuario_id"] = new SelectList(_context.Usuarios.Where(u => u.activo), "id", "contraseña");
            return View();
        }

        // POST: Doctors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,usuario_id,apellido,segundo_apellido,licencia,especialidad_id")] Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["especialidad_id"] = new SelectList(_context.Especialidades, "id", "nombre", doctor.especialidad_id);
            ViewData["usuario_id"] = new SelectList(_context.Usuarios.Where(u => u.activo), "id", "contraseña", doctor.usuario_id);
            return View(doctor);
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctores.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["especialidad_id"] = new SelectList(_context.Especialidades, "id", "nombre", doctor.especialidad_id);
            ViewData["usuario_id"] = new SelectList(_context.Usuarios.Where(u => u.activo), "id", "contraseña", doctor.usuario_id);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("id,usuario_id,apellido,segundo_apellido,licencia,especialidad_id")] Doctor doctor)
        {
            if (id != doctor.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.id))
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
            ViewData["especialidad_id"] = new SelectList(_context.Especialidades, "id", "nombre", doctor.especialidad_id);
            ViewData["usuario_id"] = new SelectList(_context.Usuarios.Where(u => u.activo), "id", "contraseña", doctor.usuario_id);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Especialidad)
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(m => m.id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var doctor = await _context.Doctores.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctores.Remove(doctor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(long id)
        {
            return _context.Doctores.Any(e => e.id == id);
        }
    }
}
